namespace TinyHeroes.Application.Interfaces;

public interface IEmailService
{
    Task SendFeedbackAsync(string category, string message, string? senderEmail, CancellationToken ct = default);
}
