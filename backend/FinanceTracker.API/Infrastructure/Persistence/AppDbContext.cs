using FinanceTracker.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<MsiPromotion> MsiPromotions => Set<MsiPromotion>();
    public DbSet<CreditCardTransaction> CreditCardTransactions => Set<CreditCardTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
