namespace FinanceTracker.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public List<Category> Categories { get; set; } = new();

    public List<Transaction> Transactions { get; set; } = new();

    public List<Budget> Budgets { get; set; } = new();
}