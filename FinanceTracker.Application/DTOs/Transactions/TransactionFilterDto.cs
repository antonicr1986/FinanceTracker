using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.DTOs.Transactions;

public class TransactionFilterDto
{
    public TransactionType? Type { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}