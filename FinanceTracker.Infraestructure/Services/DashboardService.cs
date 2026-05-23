using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(TransactionFilterDto filter)
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

        var totalIncome = await query
            .Where(transaction => transaction.Type == TransactionType.Income)
            .SumAsync(transaction => transaction.Amount);

        var totalExpense = await query
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .SumAsync(transaction => transaction.Amount);

        var transactionCount = await query.CountAsync();

        var latestTransactions = await query
            .OrderByDescending(transaction => transaction.Date)
            .Take(5)
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

        return new DashboardSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            TransactionCount = transactionCount,
            LatestTransactions = latestTransactions
        };
    }
}