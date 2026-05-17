using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.DTOs.Categories;

public class CategoryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public TransactionType Type { get; set; }
}