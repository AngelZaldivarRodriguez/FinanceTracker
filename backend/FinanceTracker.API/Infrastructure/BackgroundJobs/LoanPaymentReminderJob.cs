using FinanceTracker.API.Features.Loans;
using FinanceTracker.API.Infrastructure.Email;
using FinanceTracker.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Infrastructure.BackgroundJobs;

public class LoanPaymentReminderJob(AppDbContext db, EmailService email, ILogger<LoanPaymentReminderJob> logger)
{
    public async Task CheckPaymentsDue()
    {
        var today = DateTime.UtcNow.Date;
        var in3Days = today.AddDays(3);

        var loans = await db.Loans
            .Include(l => l.Payments)
            .ToListAsync();

        var userIds = loans.Select(l => l.UserId).Distinct().ToList();
        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.Email });

        foreach (var loan in loans)
        {
            // Usa el mismo cálculo que la UI — los pagos futuros no están en DB
            var schedule = AmortizationCalculator.BuildSchedule(loan.StartDate, loan.Payments);
            var nextUnpaid = schedule.FirstOrDefault(r => !r.IsPaid);

            if (nextUnpaid is null) continue;
            if (nextUnpaid.DueDate.Date < today || nextUnpaid.DueDate.Date > in3Days) continue;

            if (!users.TryGetValue(loan.UserId, out var user)) continue;

            var daysLeft = (int)(nextUnpaid.DueDate.Date - today).TotalDays;
            var daysText = daysLeft == 0 ? "hoy" : daysLeft == 1 ? "mañana" : $"en {daysLeft} días";

            var subject = $"🚗 Pago del crédito {loan.Name} vence {daysText}";
            var body = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px">
                  <h2 style="color:#1d4ed8">Finance Tracker</h2>
                  <p>Tu pago mensual del crédito <strong>{loan.Name}</strong> vence <strong>{daysText}</strong>.</p>
                  <table style="width:100%;border-collapse:collapse;margin:16px 0">
                    <tr style="background:#f1f5f9">
                      <td style="padding:10px 12px;font-weight:600">Pago mensual</td>
                      <td style="padding:10px 12px;text-align:right;color:#dc2626;font-weight:700">{nextUnpaid.Total:C}</td>
                    </tr>
                    <tr>
                      <td style="padding:10px 12px;font-weight:600">Número de pago</td>
                      <td style="padding:10px 12px;text-align:right">{nextUnpaid.Number} de {loan.TotalPayments}</td>
                    </tr>
                    <tr style="background:#f1f5f9">
                      <td style="padding:10px 12px;font-weight:600">Fecha límite</td>
                      <td style="padding:10px 12px;text-align:right">{nextUnpaid.DueDate:dd/MMM/yyyy}</td>
                    </tr>
                  </table>
                  <p style="color:#6b7280;font-size:13px">Este recordatorio se envía automáticamente desde Finance Tracker.</p>
                </div>
                """;

            await email.SendAsync(user.Email, subject, body);
            logger.LogInformation("Loan reminder sent — {Loan} payment #{Number} due {Date}",
                loan.Name, nextUnpaid.Number, nextUnpaid.DueDate.Date);
        }
    }
}
