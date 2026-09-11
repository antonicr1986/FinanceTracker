using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Services;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public BudgetService(AppDbContext context,ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<BudgetDto>> GetAllAsync()
    {
        var userId = GetCurrentUserId();

        var budgets = await _context.Budgets
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId)
            .OrderByDescending(budget => budget.Year)
            .ThenByDescending(budget => budget.Month)
            .ToListAsync();

        var budgetDtos = new List<BudgetDto>();

        foreach (var budget in budgets)
        {
            var spentAmount = await _context.Transactions
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.Type == budget.Type &&
                    transaction.Date.Month == budget.Month &&
                    transaction.Date.Year == budget.Year &&
                    (!budget.CategoryId.HasValue || transaction.CategoryId == budget.CategoryId.Value))
                .SumAsync(transaction => transaction.Amount);

            budgetDtos.Add(new BudgetDto
            {
                Id = budget.Id,
                Name = budget.Name,
                Amount = budget.Amount,
                SpentAmount = spentAmount,
                RemainingAmount = budget.Amount - spentAmount,
                UsagePercentage = budget.Amount > 0
                    ? Math.Round((spentAmount / budget.Amount) * 100, 2)
                    : 0,
                Month = budget.Month,
                Year = budget.Year,
                Type = budget.Type,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category != null ? budget.Category.Name : null
            });
        }

        return budgetDtos;
    }

    public async Task<BudgetDto?> GetByIdAsync(int id)
    {
        var userId = GetCurrentUserId();

        var budget = await _context.Budgets
            .Include(budget => budget.Category)
            .FirstOrDefaultAsync(budget =>
                budget.Id == id &&
                budget.UserId == userId);

        if (budget is null)
        {
            return null;
        }

        var spentAmount = await _context.Transactions
            .Where(transaction =>
                transaction.UserId == userId &&
                transaction.Type == budget.Type &&
                transaction.Date.Month == budget.Month &&
                transaction.Date.Year == budget.Year &&
                (!budget.CategoryId.HasValue || transaction.CategoryId == budget.CategoryId.Value))
            .SumAsync(transaction => transaction.Amount);

        return new BudgetDto
        {
            Id = budget.Id,
            Name = budget.Name,
            Amount = budget.Amount,
            SpentAmount = spentAmount,
            RemainingAmount = budget.Amount - spentAmount,
            UsagePercentage = budget.Amount > 0
                ? Math.Round((spentAmount / budget.Amount) * 100, 2)
                : 0,
            Month = budget.Month,
            Year = budget.Year,
            Type = budget.Type,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category != null ? budget.Category.Name : null
        };
    }

    public async Task<CreateBudgetServiceResult> CreateAsync(CreateBudgetDto createBudgetDto)
    {
        var userId = GetCurrentUserId();

        if (createBudgetDto.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(category =>
                    category.Id == createBudgetDto.CategoryId.Value &&
                    category.UserId == userId);
            if (category is null)
            {
                return new CreateBudgetServiceResult
                {
                    Result = BudgetOperationResult.CategoryNotFound
                };
            }

            if (category.Type != createBudgetDto.Type)
            {
                return new CreateBudgetServiceResult
                {
                    Result = BudgetOperationResult.CategoryTypeMismatch
                };
            }
        }

        var budget = new Budget
        {
            Name = createBudgetDto.Name,
            Amount = createBudgetDto.Amount,
            Month = createBudgetDto.Month,
            Year = createBudgetDto.Year,
            Type = createBudgetDto.Type,
            CategoryId = createBudgetDto.CategoryId,
            UserId = userId
        };

        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        return new CreateBudgetServiceResult
        {
            Result = BudgetOperationResult.Success,
            Budget = await GetByIdAsync(budget.Id)
        };
    }

    public async Task<BudgetOperationResult> UpdateAsync(int id, UpdateBudgetDto updateBudgetDto)
    {
        var userId = GetCurrentUserId();

        var budget = await _context.Budgets
             .FirstOrDefaultAsync(budget =>
                 budget.Id == id &&
                 budget.UserId == userId);

        if (budget is null)
        {
            return BudgetOperationResult.BudgetNotFound;
        }

        if (updateBudgetDto.CategoryId.HasValue)
        {
            var category = await _context.Categories
             .FirstOrDefaultAsync(category =>
                 category.Id == updateBudgetDto.CategoryId.Value &&
                 category.UserId == userId);

            if (category is null)
            {
                return BudgetOperationResult.CategoryNotFound;
            }

            if (category.Type != updateBudgetDto.Type)
            {
                return BudgetOperationResult.CategoryTypeMismatch;
            }
        }

        budget.Name = updateBudgetDto.Name;
        budget.Amount = updateBudgetDto.Amount;
        budget.Month = updateBudgetDto.Month;
        budget.Year = updateBudgetDto.Year;
        budget.Type = updateBudgetDto.Type;
        budget.CategoryId = updateBudgetDto.CategoryId;

        await _context.SaveChangesAsync();

        return BudgetOperationResult.Success;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var userId = GetCurrentUserId();

        var budget = await _context.Budgets
            .FirstOrDefaultAsync(budget =>
                budget.Id == id &&
                budget.UserId == userId);

        if (budget is null)
        {
            return false;
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        return true;
    }
    private int GetCurrentUserId()
    {
        return _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user not found.");
    }
}