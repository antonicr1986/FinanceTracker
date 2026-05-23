using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.DTOs.Transactions;

namespace FinanceTracker.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(TransactionFilterDto filter);
}