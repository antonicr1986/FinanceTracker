using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.Application.DTOs.Categories;

namespace FinanceTracker.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();

    Task<CategoryDto?> GetByIdAsync(int id);

    Task<CategoryDto> CreateAsync(CreateCategoryDto createCategoryDto);

    Task<bool> UpdateAsync(int id, UpdateCategoryDto updateCategoryDto);

    Task<bool> DeleteAsync(int id);
}