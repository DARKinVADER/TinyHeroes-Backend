namespace TinyHeroes.Application.DTOs.Prize;

public record SetPrizeRequest(string Scope, int? Rank, string Emoji, string Label);
public record PrizeAssignmentResponse(Guid Id, string Scope, int? Rank, string Emoji, string Label);
