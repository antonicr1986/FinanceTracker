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
    private readonly ICurrentUserService _currentUserService;

    public CategoryService(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var userId = GetCurrentUserId();

        return await _context.Categories
        .Where(category => category.UserId == userId)
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
        var userId = GetCurrentUserId();

        return await _context.Categories
            .Where(category =>
                category.Id == id &&
                category.UserId == userId)
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
        var userId = GetCurrentUserId();

        var category = new Category
        {
            Name = createCategoryDto.Name,
            Type = createCategoryDto.Type,
            UserId = userId
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
        var userId = GetCurrentUserId();

        var category = await _context.Categories
            .FirstOrDefaultAsync(category =>
                category.Id == id &&
                category.UserId == userId);

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
        var userId = GetCurrentUserId();

        var category = await _context.Categories
            .FirstOrDefaultAsync(category =>
                category.Id == id &&
                category.UserId == userId);

        if (category is null)
        {
            return DeleteCategoryResult.CategoryNotFound;
        }

        var hasTransactions = await _context.Transactions
            .AnyAsync(transaction =>
                transaction.CategoryId == id &&
                transaction.UserId == userId);

        if (hasTransactions)
        {
            return DeleteCategoryResult.CategoryHasTransactions;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return DeleteCategoryResult.Success;
    }

    private int GetCurrentUserId()
    {
        return _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user not found.");
    }
}