using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Application.Settings;

namespace TinyHeroes.Infrastructure.Services;

public class EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger) : IEmailService
{
    private static readonly Dictionary<string, string> CategoryEmojis = new()
    {
        ["bug"] = "🐛",
        ["idea"] = "💡",
        ["general"] = "💬"
    };

    private static readonly Dictionary<string, string> CategoryLabels = new()
    {
        ["bug"] = "Bug report",
        ["idea"] = "Idea",
        ["general"] = "General feedback"
    };

    public async Task SendFeedbackAsync(string category, string message, string? senderEmail, CancellationToken ct = default)
    {
        var settings = options.Value;
        var emoji = CategoryEmojis.GetValueOrDefault(category, "💬");
        var label = CategoryLabels.GetValueOrDefault(category, "Feedback");

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("TinyHeroes", settings.FromAddress));
        email.To.Add(MailboxAddress.Parse(settings.AdminAddress));
        email.Subject = $"[TinyHeroes Feedback] {emoji} {label}";

        if (!string.IsNullOrWhiteSpace(senderEmail))
            email.ReplyTo.Add(MailboxAddress.Parse(senderEmail));

        var fromLine = string.IsNullOrWhiteSpace(senderEmail) ? "anonymous" : senderEmail;
        email.Body = new TextPart("plain")
        {
            Text = $"Category: {label}\nFrom: {fromLine}\n\nMessage:\n{message}"
        };

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls, ct);

            if (!string.IsNullOrWhiteSpace(settings.Username))
                await smtp.AuthenticateAsync(settings.Username, settings.Password, ct);

            await smtp.SendAsync(email, ct);
            await smtp.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send feedback email. Category={Category} From={From}", category, fromLine);
            throw;
        }
    }
}
