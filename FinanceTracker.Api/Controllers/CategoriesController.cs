using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace FinanceTracker.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _categoryService.GetAllAsync();

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto createCategoryDto)
    {
        var category = await _categoryService.CreateAsync(createCategoryDto);

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto updateCategoryDto)
    {
        var updated = await _categoryService.UpdateAsync(id, updateCategoryDto);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _categoryService.DeleteAsync(id);

        if (result == DeleteCategoryResult.CategoryNotFound)
        {
            return NotFound();
        }

        if (result == DeleteCategoryResult.CategoryHasTransactions)
        {
            return BadRequest("Cannot delete category because it has associated transactions.");
        }

        return NoContent();
    }
}