namespace FinanceTracker.Application.Common;

public enum DeleteCategoryResult
{
    Success,
    CategoryNotFound,
    CategoryHasTransactions
}