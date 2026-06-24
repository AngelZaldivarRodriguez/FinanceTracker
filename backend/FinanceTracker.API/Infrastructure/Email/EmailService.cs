using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FinanceTracker.API.Infrastructure.Email;

public class EmailSettings
{
    public string From { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Password { get; set; } = "";
}

public class EmailService(EmailSettings settings, ILogger<EmailService> logger)
{
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(settings.Password))
        {
            logger.LogWarning("Email not sent — no SMTP password configured. Subject: {Subject}", subject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings.From));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(settings.From, settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email sent to {To} — {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} — {Subject}", to, subject);
        }
    }
}
