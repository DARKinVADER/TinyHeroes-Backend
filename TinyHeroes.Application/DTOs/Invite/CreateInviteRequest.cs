using System.ComponentModel.DataAnnotations;

namespace TinyHeroes.Application.DTOs.Invite;

public record CreateInviteRequest([EmailAddress] string? Email);
public record InviteResponse(Guid Id, string Token, string? Email, DateTime ExpiresAt);
