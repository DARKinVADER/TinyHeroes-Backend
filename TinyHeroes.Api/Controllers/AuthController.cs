using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using TinyHeroes.Api.Services;
using TinyHeroes.Application.DTOs.Auth;
using TinyHeroes.Application.Interfaces;
using TinyHeroes.Domain.Entities;
using TinyHeroes.Infrastructure.Data;

namespace TinyHeroes.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ITokenService tokenService,
    AppDbContext db,
    IConfiguration config,
    IMemoryCache cache,
    ICaptchaService captchaService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        if (!await captchaService.ValidateAsync(req.CaptchaToken))
            return BadRequest(new { error = "captcha_failed" });

        var user = new User { DisplayName = req.DisplayName, Email = req.Email, UserName = req.Email };
        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
        return Ok(new AuthResponse(tokenService.GenerateAccessToken(user), user.Id.ToString(), user.DisplayName, user.Email!, hasFamily));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        if (!await captchaService.ValidateAsync(req.CaptchaToken))
            return BadRequest(new { error = "captcha_failed" });

        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            await userManager.CheckPasswordAsync(new User(), req.Password);
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized();

        var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
        return Ok(new AuthResponse(tokenService.GenerateAccessToken(user), user.Id.ToString(), user.DisplayName, user.Email!, hasFamily));
    }

    [HttpGet("social/{provider}")]
    [EnableRateLimiting("auth")]
    public IActionResult SocialLogin(string provider, [FromQuery] string returnUrl = "/")
    {
        var redirectUrl = Url.Action(nameof(SocialCallback), new { provider, returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("social/{provider}/callback")]
    public async Task<IActionResult> SocialCallback(string provider, [FromQuery] string returnUrl = "/")
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null) return BadRequest("External login info not found.");

        var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
        if (email is null) return BadRequest("Provider did not supply an email.");

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var displayName = info.Principal.FindFirst(ClaimTypes.Name)?.Value ?? email;
            user = new User { DisplayName = displayName, Email = email, UserName = email };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded) return BadRequest(createResult.Errors);
            await userManager.AddLoginAsync(user, info);
        }

        var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
        var token = tokenService.GenerateAccessToken(user);
        var code = Guid.NewGuid().ToString("N");
        cache.Set(code, (token, hasFamily), TimeSpan.FromSeconds(60));

        var frontendUrl = config["Auth:FrontendUrl"] ?? "http://localhost:4200";
        return Redirect($"{frontendUrl}/auth/callback?code={code}");
    }

    [HttpPost("exchange")]
    public IActionResult Exchange([FromBody] ExchangeCodeRequest req)
    {
        if (!cache.TryGetValue<(string token, bool hasFamily)>(req.Code, out var val))
            return BadRequest("Invalid or expired code.");
        cache.Remove(req.Code);

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(val.token);
        var userId = jwt.Subject;
        var displayName = jwt.Claims.FirstOrDefault(c => c.Type == "displayName")?.Value ?? string.Empty;
        var email = jwt.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

        return Ok(new AuthResponse(val.token, userId, displayName, email, val.hasFamily));
    }
}
