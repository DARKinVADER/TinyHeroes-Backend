namespace TinyHeroes.Application.DTOs.JoinRequest;

public record SubmitJoinRequestRequest(string JoinCode);
public record ResolveJoinRequestRequest(bool Approve);
public record JoinRequestResponse(
    Guid Id,
    string RequesterDisplayName,
    string RequesterEmail,
    DateTime RequestedAt,
    string Status,
    string FamilyName);
