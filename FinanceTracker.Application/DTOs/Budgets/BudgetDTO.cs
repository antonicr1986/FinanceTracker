using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.DTOs.Budgets;

public class BudgetDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public TransactionType Type { get; set; }

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }
}