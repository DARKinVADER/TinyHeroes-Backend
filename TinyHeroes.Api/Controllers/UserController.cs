using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TinyHeroes.Application.DTOs.User;
using TinyHeroes.Domain.Entities;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController(UserManager<User> userManager) : ApiControllerBase
{

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> GetMe()
    {
        var user = await userManager.FindByIdAsync(GetUserId().ToString());
        if (user is null) return NotFound();

        return Ok(new UserProfileResponse(
            user.Id.ToString(),
            user.DisplayName,
            user.Email!,
            user.PreferredLanguage,
            user.PushNotificationsEnabled,
            user.WeeklyEmailEnabled));
    }

    [HttpPatch("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe(UpdateUserProfileRequest req)
    {
        var user = await userManager.FindByIdAsync(GetUserId().ToString());
        if (user is null) return NotFound();

        if (req.DisplayName is not null) user.DisplayName = req.DisplayName;
        if (req.PreferredLanguage is not null) user.PreferredLanguage = req.PreferredLanguage;
        if (req.PushNotificationsEnabled.HasValue) user.PushNotificationsEnabled = req.PushNotificationsEnabled.Value;
        if (req.WeeklyEmailEnabled.HasValue) user.WeeklyEmailEnabled = req.WeeklyEmailEnabled.Value;

        await userManager.UpdateAsync(user);

        return Ok(new UserProfileResponse(
            user.Id.ToString(),
            user.DisplayName,
            user.Email!,
            user.PreferredLanguage,
            user.PushNotificationsEnabled,
            user.WeeklyEmailEnabled));
    }
}
