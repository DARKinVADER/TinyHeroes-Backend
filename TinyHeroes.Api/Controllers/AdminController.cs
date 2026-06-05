using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;
using Serilog.Events;
using TinyHeroes.Domain.Enums;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[Authorize]
[Route("api/admin")]
public class AdminController(LoggingLevelSwitch levelSwitch, AppDbContext db) : ApiControllerBase
{
    [HttpGet("log-level")]
    public IActionResult GetLogLevel()
        => Ok(new { level = levelSwitch.MinimumLevel.ToString() });

    [HttpPost("log-level")]
    public async Task<IActionResult> SetLogLevel([FromBody] SetLogLevelRequest request)
    {
        var userId = GetUserId();
        var isAdmin = await db.FamilyMembers
            .AnyAsync(m => m.UserId == userId && m.Role == FamilyRole.Admin);

        if (!isAdmin)
            return Forbid();

        if (!Enum.TryParse<LogEventLevel>(request.Level, ignoreCase: true, out var level))
            return BadRequest(new { error = $"Invalid log level '{request.Level}'. Valid values: Verbose, Debug, Information, Warning, Error, Fatal." });

        levelSwitch.MinimumLevel = level;
        return Ok(new { level = levelSwitch.MinimumLevel.ToString() });
    }
}

public record SetLogLevelRequest(string? Level);
