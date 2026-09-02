using Microsoft.EntityFrameworkCore;
using TransactionService.Models;

namespace TransactionService.Data;

/// <summary>EF Core context for the Transaction Service's own PostgreSQL database.</summary>
public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasColumnType("numeric(18,2)");
    }
}
