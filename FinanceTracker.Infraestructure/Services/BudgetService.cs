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

    public BudgetService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BudgetDto>> GetAllAsync()
    {
        return await _context.Budgets
            .Include(budget => budget.Category)
            .OrderByDescending(budget => budget.Year)
            .ThenByDescending(budget => budget.Month)
            .Select(budget => new BudgetDto
            {
                Id = budget.Id,
                Name = budget.Name,
                Amount = budget.Amount,
                Month = budget.Month,
                Year = budget.Year,
                Type = budget.Type,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category != null ? budget.Category.Name : null
            })
            .ToListAsync();
    }

    public async Task<BudgetDto?> GetByIdAsync(int id)
    {
        return await _context.Budgets
            .Include(budget => budget.Category)
            .Where(budget => budget.Id == id)
            .Select(budget => new BudgetDto
            {
                Id = budget.Id,
                Name = budget.Name,
                Amount = budget.Amount,
                Month = budget.Month,
                Year = budget.Year,
                Type = budget.Type,
                CategoryId = budget.CategoryId,
                CategoryName = budget.Category != null ? budget.Category.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CreateBudgetServiceResult> CreateAsync(CreateBudgetDto createBudgetDto)
    {
        if (createBudgetDto.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(category => category.Id == createBudgetDto.CategoryId.Value);

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
            CategoryId = createBudgetDto.CategoryId
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
        var budget = await _context.Budgets.FindAsync(id);

        if (budget is null)
        {
            return BudgetOperationResult.BudgetNotFound;
        }

        if (updateBudgetDto.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(category => category.Id == updateBudgetDto.CategoryId.Value);

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
        var budget = await _context.Budgets.FindAsync(id);

        if (budget is null)
        {
            return false;
        }

        _context.Budgets.Remove(budget);
        await _context.SaveChangesAsync();

        return true;
    }
}