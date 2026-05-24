using FinanceTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Transactions;

public class UpdateTransactionDto
{
    [Required]
    [MaxLength(150)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public TransactionType Type { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be greater than 0.")]
    public int CategoryId { get; set; }
}