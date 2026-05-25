using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Domain.Entities;

public class Budget
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public TransactionType Type { get; set; }

    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

    public int? UserId { get; set; }

    public User? User { get; set; }
}