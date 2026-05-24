using FinanceTracker.Application.Common;
using FinanceTracker.Application.DTOs.Budgets;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BudgetDto>>> GetBudgets()
    {
        var budgets = await _budgetService.GetAllAsync();

        return Ok(budgets);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetDto>> GetBudget(int id)
    {
        var budget = await _budgetService.GetByIdAsync(id);

        if (budget is null)
        {
            return NotFound();
        }

        return Ok(budget);
    }

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> CreateBudget([FromBody] CreateBudgetDto createBudgetDto)
    {
        var result = await _budgetService.CreateAsync(createBudgetDto);

        if (result.Result == BudgetOperationResult.CategoryNotFound)
        {
            return BadRequest("The selected category does not exist.");
        }

        if (result.Result == BudgetOperationResult.CategoryTypeMismatch)
        {
            return BadRequest("The selected category type does not match the budget type.");
        }

        return CreatedAtAction(
            nameof(GetBudget),
            new { id = result.Budget!.Id },
            result.Budget);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(int id, [FromBody] UpdateBudgetDto updateBudgetDto)
    {
        var result = await _budgetService.UpdateAsync(id, updateBudgetDto);

        if (result == BudgetOperationResult.BudgetNotFound)
        {
            return NotFound();
        }

        if (result == BudgetOperationResult.CategoryNotFound)
        {
            return BadRequest("The selected category does not exist.");
        }

        if (result == BudgetOperationResult.CategoryTypeMismatch)
        {
            return BadRequest("The selected category type does not match the budget type.");
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(int id)
    {
        var deleted = await _budgetService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}