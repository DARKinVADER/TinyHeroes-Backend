namespace TinyHeroes.Application.DTOs.Prize;

public record CreatePrizePresetRequest(string Label, string Emoji);
public record PrizePresetResponse(Guid Id, string Label, string Emoji, bool IsSystem);
