using FinanceTracker.Application.DTOs.Transactions;

namespace FinanceTracker.Application.Common;

public class CreateTransactionServiceResult
{
    public CreateTransactionResult Result { get; set; }

    public TransactionDto? Transaction { get; set; }
}