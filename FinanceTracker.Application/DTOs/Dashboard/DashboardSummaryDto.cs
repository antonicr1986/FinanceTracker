using FinanceTracker.Application.DTOs.Transactions;

namespace FinanceTracker.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TotalIncome { get; set; }

    public decimal TotalExpense { get; set; }

    public decimal Balance { get; set; }

    public int TransactionCount { get; set; }

    public List<TransactionDto> LatestTransactions { get; set; } = new();
}