using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data;

/// <summary>
/// EF Core database context for the Account Service's own PostgreSQL database.
/// </summary>
public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store money as a fixed-precision numeric, not a floating-point double.
        modelBuilder.Entity<Account>()
            .Property(a => a.Balance)
            .HasColumnType("numeric(18,2)");

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique();
    }
}
