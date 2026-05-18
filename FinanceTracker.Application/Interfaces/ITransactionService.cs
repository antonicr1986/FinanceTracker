using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.Application.DTOs.Transactions;

namespace FinanceTracker.Application.Interfaces;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetAllAsync();

    Task<TransactionDto?> GetByIdAsync(int id);

    Task<TransactionDto> CreateAsync(CreateTransactionDto createTransactionDto);

    Task<bool> UpdateAsync(int id, UpdateTransactionDto updateTransactionDto);

    Task<bool> DeleteAsync(int id);
}