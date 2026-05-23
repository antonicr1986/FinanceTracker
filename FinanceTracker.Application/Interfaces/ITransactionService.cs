using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Transactions;

namespace FinanceTracker.Application.Interfaces;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> GetAllAsync(TransactionFilterDto filter);

    Task<TransactionDto?> GetByIdAsync(int id);

    Task<CreateTransactionServiceResult> CreateAsync(CreateTransactionDto createTransactionDto);

    Task<UpdateTransactionResult> UpdateAsync(int id, UpdateTransactionDto updateTransactionDto);

    Task<bool> DeleteAsync(int id);

    Task<TransactionSummaryDto> GetSummaryAsync(TransactionFilterDto filter);
}