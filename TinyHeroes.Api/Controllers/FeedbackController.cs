using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TinyHeroes.Application.DTOs.Feedback;
using TinyHeroes.Application.Interfaces;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController(IEmailService emailService, ILogger<FeedbackController> logger) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("feedback")]
    public async Task<IActionResult> Submit(FeedbackRequest req, CancellationToken ct)
    {
        try
        {
            await emailService.SendFeedbackAsync(req.Category, req.Message, req.Email, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Feedback email send failed silently. Category={Category}", req.Category);
        }

        return NoContent();
    }
}
