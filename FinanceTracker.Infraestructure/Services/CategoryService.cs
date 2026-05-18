using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.Application.DTOs.Categories;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.Application.Common;

namespace FinanceTracker.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        return await _context.Categories
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Type = category.Type
            })
            .ToListAsync();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        return await _context.Categories
            .Where(category => category.Id == id)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Type = category.Type
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto createCategoryDto)
    {
        var category = new Category
        {
            Name = createCategoryDto.Name,
            Type = createCategoryDto.Type
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type
        };
    }

    public async Task<bool> UpdateAsync(int id, UpdateCategoryDto updateCategoryDto)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category is null)
        {
            return false;
        }

        category.Name = updateCategoryDto.Name;
        category.Type = updateCategoryDto.Type;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<DeleteCategoryResult> DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category is null)
        {
            return DeleteCategoryResult.CategoryNotFound;
        }

        var hasTransactions = await _context.Transactions
            .AnyAsync(transaction => transaction.CategoryId == id);

        if (hasTransactions)
        {
            return DeleteCategoryResult.CategoryHasTransactions;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return DeleteCategoryResult.Success;
    }
}