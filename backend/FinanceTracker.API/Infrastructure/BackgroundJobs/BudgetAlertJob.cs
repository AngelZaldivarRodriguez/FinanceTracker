using FinanceTracker.API.Domain.Enums;
using FinanceTracker.API.Infrastructure.Email;
using FinanceTracker.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Infrastructure.BackgroundJobs;

public class BudgetAlertJob(AppDbContext db, EmailService email, ILogger<BudgetAlertJob> logger)
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
                    "Budget alert: User {UserId} — {Percentage:F1}% of {Category} ({Spent}/{Limit})",
                    budget.UserId, percentage, budget.Category.Name, spent, budget.LimitAmount);

                var subject = $"⚠️ Presupuesto de {budget.Category.Name} al {percentage:F0}%";
                var body = $"""
                    <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px">
                      <h2 style="color:#1d4ed8">Finance Tracker</h2>
                      <p>Tu presupuesto de <strong>{budget.Category.Name}</strong> está al <strong style="color:#dc2626">{percentage:F0}%</strong>.</p>
                      <table style="width:100%;border-collapse:collapse;margin:16px 0">
                        <tr style="background:#f1f5f9">
                          <td style="padding:10px 12px;font-weight:600">Gastado</td>
                          <td style="padding:10px 12px;text-align:right;color:#dc2626;font-weight:700">{spent:C}</td>
                        </tr>
                        <tr>
                          <td style="padding:10px 12px;font-weight:600">Límite</td>
                          <td style="padding:10px 12px;text-align:right">{budget.LimitAmount:C}</td>
                        </tr>
                        <tr style="background:#f1f5f9">
                          <td style="padding:10px 12px;font-weight:600">Disponible</td>
                          <td style="padding:10px 12px;text-align:right">{budget.LimitAmount - spent:C}</td>
                        </tr>
                      </table>
                      <p style="color:#6b7280;font-size:13px">Este recordatorio se envía automáticamente desde Finance Tracker.</p>
                    </div>
                    """;

                await email.SendAsync(budget.User.Email, subject, body);
            }
        }

        await db.SaveChangesAsync();
    }
}
