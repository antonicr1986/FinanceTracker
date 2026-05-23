using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceTracker.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransactionDto>> GetAllAsync(TransactionFilterDto filter)
    {
        var query = _context.Transactions
            .Include(transaction => transaction.Category)
            .AsQueryable();

        if (filter.Type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == filter.Type.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == filter.CategoryId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(transaction => transaction.Date >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(transaction => transaction.Date <= filter.ToDate.Value);
        }

        return await query
        .OrderByDescending(transaction => transaction.Date)
        .Skip((filter.PageNumber - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .Select(transaction => new TransactionDto
        {
            Id = transaction.Id,
            Description = transaction.Description,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Type = transaction.Type,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category!.Name
        })
        .ToListAsync();
    }

    public async Task<TransactionDto?> GetByIdAsync(int id)
    {
        return await _context.Transactions
            .Include(transaction => transaction.Category)
            .Where(transaction => transaction.Id == id)
            .Select(transaction => new TransactionDto
            {
                Id = transaction.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Date = transaction.Date,
                Type = transaction.Type,
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category!.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CreateTransactionServiceResult> CreateAsync(CreateTransactionDto createTransactionDto)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(category => category.Id == createTransactionDto.CategoryId);

        if (category is null)
        {
            return new CreateTransactionServiceResult
            {
                Result = CreateTransactionResult.CategoryNotFound
            };
        }

        if (category.Type != createTransactionDto.Type)
        {
            return new CreateTransactionServiceResult
            {
                Result = CreateTransactionResult.CategoryTypeMismatch
            };
        }

        var transaction = new Transaction
        {
            Description = createTransactionDto.Description,
            Amount = createTransactionDto.Amount,
            Date = createTransactionDto.Date,
            Type = createTransactionDto.Type,
            CategoryId = createTransactionDto.CategoryId
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new CreateTransactionServiceResult
        {
            Result = CreateTransactionResult.Success,
            Transaction = await GetByIdAsync(transaction.Id)
        };
    }

    public async Task<UpdateTransactionResult> UpdateAsync(int id, UpdateTransactionDto updateTransactionDto)
    {
        var transaction = await _context.Transactions.FindAsync(id);

        if (transaction is null)
        {
            return UpdateTransactionResult.TransactionNotFound;
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(category => category.Id == updateTransactionDto.CategoryId);

        if (category is null)
        {
            return UpdateTransactionResult.CategoryNotFound;
        }

        if (category.Type != updateTransactionDto.Type)
        {
            return UpdateTransactionResult.CategoryTypeMismatch;
        }

        transaction.Description = updateTransactionDto.Description;
        transaction.Amount = updateTransactionDto.Amount;
        transaction.Date = updateTransactionDto.Date;
        transaction.Type = updateTransactionDto.Type;
        transaction.CategoryId = updateTransactionDto.CategoryId;

        await _context.SaveChangesAsync();

        return UpdateTransactionResult.Success;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);

        if (transaction is null)
        {
            return false;
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<TransactionSummaryDto> GetSummaryAsync(TransactionFilterDto filter)
    {
        var query = _context.Transactions.AsQueryable();

        if (filter.Type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == filter.Type.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == filter.CategoryId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(transaction => transaction.Date >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(transaction => transaction.Date <= filter.ToDate.Value);
        }

        var totalIncome = await query
            .Where(transaction => transaction.Type == TransactionType.Income)
            .SumAsync(transaction => transaction.Amount);

        var totalExpense = await query
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .SumAsync(transaction => transaction.Amount);

        return new TransactionSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense
        };
    }
}