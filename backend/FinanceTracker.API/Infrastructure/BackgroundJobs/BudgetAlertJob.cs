using FinanceTracker.API.Domain.Enums;
using FinanceTracker.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Infrastructure.BackgroundJobs;

public class BudgetAlertJob(AppDbContext db, ILogger<BudgetAlertJob> logger)
{
    public async Task CheckBudgetAlerts()
    {
        var now = DateTime.UtcNow;

        var budgets = await db.Budgets
            .Include(b => b.Category)
            .Include(b => b.User)
            .Where(b => b.Month == now.Month && b.Year == now.Year && !b.AlertSent)
            .ToListAsync();

        foreach (var budget in budgets)
        {
            var spent = await db.Transactions
                .Where(t =>
                    t.UserId == budget.UserId &&
                    t.CategoryId == budget.CategoryId &&
                    t.Type == TransactionType.Expense &&
                    t.Date.Month == now.Month &&
                    t.Date.Year == now.Year)
                .SumAsync(t => t.Amount);

            var percentage = budget.LimitAmount > 0 ? spent / budget.LimitAmount * 100 : 0;

            if (percentage >= 80)
            {
                budget.AlertSent = true;

                logger.LogWarning(
                    "Budget alert: User {UserId} has spent {Percentage:F1}% of their {Category} budget ({Spent}/{Limit})",
                    budget.UserId,
                    percentage,
                    budget.Category.Name,
                    spent,
                    budget.LimitAmount
                );

                // Aqui se puede agregar: envio de email, push notification, etc.
            }
        }

        await db.SaveChangesAsync();
    }
}
