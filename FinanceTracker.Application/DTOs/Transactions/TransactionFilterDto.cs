using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Transactions;

public class TransactionFilterDto
{
    public TransactionType? Type { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 10;
}