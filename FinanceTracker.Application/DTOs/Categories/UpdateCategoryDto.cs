using FinanceTracker.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Application.DTOs.Categories;

public class UpdateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
}