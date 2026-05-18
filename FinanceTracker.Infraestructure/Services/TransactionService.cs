using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransactionDto>> GetAllAsync()
    {
        return await _context.Transactions
            .Include(transaction => transaction.Category)
            .Select(transaction => new TransactionDto
            {
                Id = transaction.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Date = transaction.Date,
                Type = transaction.Type,
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category.Name
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
                CategoryName = transaction.Category.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<TransactionDto?> CreateAsync(CreateTransactionDto createTransactionDto)
    {
        var categoryExists = await _context.Categories
            .AnyAsync(category => category.Id == createTransactionDto.CategoryId);

        if (!categoryExists)
        {
            return null;
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

        return await GetByIdAsync(transaction.Id);
    }

    public async Task<bool> UpdateAsync(int id, UpdateTransactionDto updateTransactionDto)
    {
        var transaction = await _context.Transactions.FindAsync(id);

        if (transaction is null)
        {
            return false;
        }

        transaction.Description = updateTransactionDto.Description;
        transaction.Amount = updateTransactionDto.Amount;
        transaction.Date = updateTransactionDto.Date;
        transaction.Type = updateTransactionDto.Type;
        transaction.CategoryId = updateTransactionDto.CategoryId;

        await _context.SaveChangesAsync();

        return true;
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
}