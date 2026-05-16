using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    AppDbContext db) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var user = new User { DisplayName = req.DisplayName, Email = req.Email, UserName = req.Email };
        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
        return Ok(new AuthResponse(tokenService.GenerateAccessToken(user), user.Id.ToString(), user.DisplayName, user.Email!, hasFamily));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized();

        var result = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!result.Succeeded) return Unauthorized();

        var hasFamily = db.FamilyMembers.Any(m => m.UserId == user.Id);
        return Ok(new AuthResponse(tokenService.GenerateAccessToken(user), user.Id.ToString(), user.DisplayName, user.Email!, hasFamily));
    }
}
