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

    public async Task<BudgetDto?> CreateAsync(CreateBudgetDto createBudgetDto)
    {
        if (createBudgetDto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(category => category.Id == createBudgetDto.CategoryId.Value);

            if (!categoryExists)
            {
                return null;
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

        return await GetByIdAsync(budget.Id);
    }

    public async Task<bool> UpdateAsync(int id, UpdateBudgetDto updateBudgetDto)
    {
        var budget = await _context.Budgets.FindAsync(id);

        if (budget is null)
        {
            return false;
        }

        if (updateBudgetDto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(category => category.Id == updateBudgetDto.CategoryId.Value);

            if (!categoryExists)
            {
                return false;
            }
        }

        budget.Name = updateBudgetDto.Name;
        budget.Amount = updateBudgetDto.Amount;
        budget.Month = updateBudgetDto.Month;
        budget.Year = updateBudgetDto.Year;
        budget.Type = updateBudgetDto.Type;
        budget.CategoryId = updateBudgetDto.CategoryId;

        await _context.SaveChangesAsync();

        return true;
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