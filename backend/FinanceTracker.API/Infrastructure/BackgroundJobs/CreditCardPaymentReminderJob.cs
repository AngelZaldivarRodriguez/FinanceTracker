using FinanceTracker.API.Infrastructure.Email;
using FinanceTracker.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.API.Infrastructure.BackgroundJobs;

public class CreditCardPaymentReminderJob(AppDbContext db, EmailService email, ILogger<CreditCardPaymentReminderJob> logger)
{
    public async Task CheckPaymentsDue()
    {
        var today = DateTime.UtcNow.Date;
        var in3Days = today.AddDays(3);

        var cards = await db.CreditCards
            .Where(c => c.NextPaymentDueDate.Date >= today && c.NextPaymentDueDate.Date <= in3Days)
            .ToListAsync();

        var userIds = cards.Select(c => c.UserId).Distinct().ToList();
        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        foreach (var card in cards)
        {
            if (!users.TryGetValue(card.UserId, out var userEmail)) continue;

            var daysLeft = (int)(card.NextPaymentDueDate.Date - today).TotalDays;
            var daysText = daysLeft == 0 ? "hoy" : daysLeft == 1 ? "mañana" : $"en {daysLeft} días";

            var subject = $"⚠️ Pago de tarjeta {card.Name} vence {daysText}";
            var body = $"""
                <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px">
                  <h2 style="color:#1d4ed8">Finance Tracker</h2>
                  <p>Tu tarjeta <strong>{card.Name}</strong> tiene pago pendiente <strong>{daysText}</strong>.</p>
                  <table style="width:100%;border-collapse:collapse;margin:16px 0">
                    <tr style="background:#f1f5f9">
                      <td style="padding:10px 12px;font-weight:600">Pago sin intereses</td>
                      <td style="padding:10px 12px;text-align:right;color:#dc2626;font-weight:700">{card.PaymentToAvoidInterest:C}</td>
                    </tr>
                    <tr>
                      <td style="padding:10px 12px;font-weight:600">Pago mínimo</td>
                      <td style="padding:10px 12px;text-align:right">{card.MinimumPayment:C}</td>
                    </tr>
                    <tr style="background:#f1f5f9">
                      <td style="padding:10px 12px;font-weight:600">Fecha límite</td>
                      <td style="padding:10px 12px;text-align:right">{card.NextPaymentDueDate:dd/MMM/yyyy}</td>
                    </tr>
                  </table>
                  <p style="color:#6b7280;font-size:13px">Este recordatorio se envía automáticamente desde Finance Tracker.</p>
                </div>
                """;

            await email.SendAsync(userEmail, subject, body);
            logger.LogInformation("Payment reminder sent for card {Card} — due {Date}", card.Name, card.NextPaymentDueDate.Date);
        }
    }
}
