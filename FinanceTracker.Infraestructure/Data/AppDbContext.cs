using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinanceTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Transaction> Transactions { get; set; }

    public DbSet<Budget> Budgets { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<MfaRecoveryCode> MfaRecoveryCodes { get; set; }

    public DbSet<TrustedDevice> TrustedDevices { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Base64 de nonce + tag + secreto cifrado: unos 80 caracteres.
        modelBuilder.Entity<User>()
            .Property(user => user.MfaSecret)
            .HasMaxLength(200);

        // SHA-256 en hexadecimal.
        modelBuilder.Entity<MfaRecoveryCode>()
            .Property(code => code.CodeHash)
            .HasMaxLength(64);

        // SHA-256 en hexadecimal; el indice es para la busqueda de cada login.
        modelBuilder.Entity<TrustedDevice>()
            .Property(device => device.TokenHash)
            .HasMaxLength(64);

        modelBuilder.Entity<TrustedDevice>()
            .HasIndex(device => new { device.UserId, device.TokenHash });

        modelBuilder.Entity<Budget>()
            .Property(budget => budget.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .Property(transaction => transaction.Amount)
            .HasPrecision(18, 2);
    }

}
