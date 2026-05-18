using FinanceTracker.Application.DTOs.Transactions;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Common;

namespace FinanceTracker.Api.Controllers;

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
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionDto createTransactionDto)
    {
        var transaction = await _transactionService.CreateAsync(createTransactionDto);

        if (transaction is null)
        {
            return BadRequest("The selected category does not exist.");
        }

        return CreatedAtAction(
            nameof(GetTransaction),
            new { id = transaction.Id },
            transaction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(int id, UpdateTransactionDto updateTransactionDto)
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