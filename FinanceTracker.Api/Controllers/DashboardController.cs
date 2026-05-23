using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary([FromQuery] TransactionFilterDto filter)
    {
        var summary = await _dashboardService.GetSummaryAsync(filter);

        return Ok(summary);
    }
}