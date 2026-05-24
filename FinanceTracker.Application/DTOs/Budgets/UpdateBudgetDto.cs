using FinanceTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Budgets;

public class UpdateBudgetDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
    public int Month { get; set; }

    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int Year { get; set; }

    public TransactionType Type { get; set; }

    public int? CategoryId { get; set; }
}