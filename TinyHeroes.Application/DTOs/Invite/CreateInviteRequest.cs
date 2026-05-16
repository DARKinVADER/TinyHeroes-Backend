namespace TinyHeroes.Application.DTOs.Invite;

public record CreateInviteRequest(string? Email);
public record InviteResponse(Guid Id, string Token, string? Email, DateTime ExpiresAt);
