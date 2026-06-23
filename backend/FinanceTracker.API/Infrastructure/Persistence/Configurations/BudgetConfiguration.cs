using FinanceTracker.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceTracker.API.Infrastructure.Persistence.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.LimitAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.HasOne(b => b.User).WithMany(u => u.Budgets).HasForeignKey(b => b.UserId);
        builder.HasOne(b => b.Category).WithMany(c => c.Budgets).HasForeignKey(b => b.CategoryId);
        builder.HasIndex(b => new { b.UserId, b.CategoryId, b.Month, b.Year }).IsUnique();
    }
}
