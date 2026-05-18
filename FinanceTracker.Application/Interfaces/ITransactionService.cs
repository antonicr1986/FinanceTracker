using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Common;

namespace FinanceTracker.Application.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetAllAsync(TransactionFilterDto filter);

    Task<TransactionDto?> GetByIdAsync(int id);

    Task<TransactionDto?> CreateAsync(CreateTransactionDto createTransactionDto);

    Task<UpdateTransactionResult> UpdateAsync(int id, UpdateTransactionDto updateTransactionDto);

    Task<bool> DeleteAsync(int id);

    Task<TransactionSummaryDto> GetSummaryAsync(TransactionFilterDto filter);
}