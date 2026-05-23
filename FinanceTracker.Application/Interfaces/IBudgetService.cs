using FinanceTracker.Application.DTOs.Budgets;

namespace FinanceTracker.Application.Interfaces;

public interface IBudgetService
{
    Task<List<BudgetDto>> GetAllAsync();

    Task<BudgetDto?> GetByIdAsync(int id);

    Task<BudgetDto?> CreateAsync(CreateBudgetDto createBudgetDto);

    Task<bool> UpdateAsync(int id, UpdateBudgetDto updateBudgetDto);

    Task<bool> DeleteAsync(int id);
}