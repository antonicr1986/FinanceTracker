using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace FinanceTracker.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions([FromQuery] TransactionFilterDto filter)
    {
        var transactions = await _transactionService.GetAllAsync(filter);

        return Ok(transactions);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<TransactionSummaryDto>> GetSummary([FromQuery] TransactionFilterDto filter)
    {
        var summary = await _transactionService.GetSummaryAsync(filter);

        return Ok(summary);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id)
    {
        var transaction = await _transactionService.GetByIdAsync(id);

        if (transaction is null)
        {
            return NotFound();
        }

        return Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionDto createTransactionDto)
    {
        var result = await _transactionService.CreateAsync(createTransactionDto);

        if (result.Result == CreateTransactionResult.CategoryNotFound)
        {
            return BadRequest("The selected category does not exist.");
        }

        if (result.Result == CreateTransactionResult.CategoryTypeMismatch)
        {
            return BadRequest("The selected category type does not match the transaction type.");
        }

        return CreatedAtAction(
            nameof(GetTransaction),
            new { id = result.Transaction!.Id },
            result.Transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] UpdateTransactionDto updateTransactionDto)
    {
        var result = await _transactionService.UpdateAsync(id, updateTransactionDto);

        if (result == UpdateTransactionResult.TransactionNotFound)
        {
            return NotFound();
        }

        if (result == UpdateTransactionResult.CategoryNotFound)
        {
            return BadRequest("The selected category does not exist.");
        }

        if (result == UpdateTransactionResult.CategoryTypeMismatch)
        {
            return BadRequest("The selected category type does not match the transaction type.");
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var deleted = await _transactionService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}