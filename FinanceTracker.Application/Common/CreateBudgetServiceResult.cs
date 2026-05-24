using FinanceTracker.Application.DTOs.Budgets;

namespace FinanceTracker.Application.Common;

public class CreateBudgetServiceResult
{
    public BudgetOperationResult Result { get; set; }

    public BudgetDto? Budget { get; set; }
}