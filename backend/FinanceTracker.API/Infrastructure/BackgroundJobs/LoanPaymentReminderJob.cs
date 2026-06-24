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

        // Busca el próximo pago no pagado de cada crédito que venza en <=3 días
        var payments = await db.Set<FinanceTracker.API.Domain.Entities.LoanPayment>()
            .Include(p => p.Loan)
            .Where(p => !p.IsPaid && p.DueDate.Date >= today && p.DueDate.Date <= in3Days)
            .ToListAsync();

        var userIds = payments.Select(p => p.Loan.UserId).Distinct().ToList();
        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        foreach (var payment in payments)
        {
            if (!users.TryGetValue(payment.Loan.UserId, out var userEmail)) continue;

            var daysLeft = (int)(payment.DueDate.Date - today).TotalDays;
            var daysText = daysLeft == 0 ? "hoy" : daysLeft == 1 ? "mañana" : $"en {daysLeft} días";

            var subject = $"🚗 Pago del crédito {payment.Loan.Name} vence {daysText}";
            var body = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px">
                  <h2 style="color:#1d4ed8">Finance Tracker</h2>
                  <p>Tu pago mensual del crédito <strong>{payment.Loan.Name}</strong> vence <strong>{daysText}</strong>.</p>
                  <table style="width:100%;border-collapse:collapse;margin:16px 0">
                    <tr style="background:#f1f5f9">
                      <td style="padding:10px 12px;font-weight:600">Pago mensual</td>
                      <td style="padding:10px 12px;text-align:right;color:#dc2626;font-weight:700">{payment.Loan.MonthlyPayment:C}</td>
                    </tr>
                    <tr>
                      <td style="padding:10px 12px;font-weight:600">Número de pago</td>
                      <td style="padding:10px 12px;text-align:right">{payment.PaymentNumber} de {payment.Loan.TotalPayments}</td>
                    </tr>
                    <tr style="background:#f1f5f9">
                      <td style="padding:10px 12px;font-weight:600">Fecha límite</td>
                      <td style="padding:10px 12px;text-align:right">{payment.DueDate:dd/MMM/yyyy}</td>
                    </tr>
                  </table>
                  <p style="color:#6b7280;font-size:13px">Este recordatorio se envía automáticamente desde Finance Tracker.</p>
                </div>
                """;

            await email.SendAsync(userEmail, subject, body);
            logger.LogInformation("Loan reminder sent — {Loan} payment #{Number} due {Date}",
                payment.Loan.Name, payment.PaymentNumber, payment.DueDate.Date);
        }
    }
}
