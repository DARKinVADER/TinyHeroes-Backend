// backend/TinyHeroes.Application/DTOs/Prize/PrizeClaimDtos.cs
namespace TinyHeroes.Application.DTOs.Prize;

public record CreatePrizeClaimRequest(
    string Scope,
    Guid? WeekSummaryId,
    Guid? MonthSummaryId,
    int? Rank,
    Guid ChildId,
    string ChildName,
    string PrizeEmoji,
    string PrizeLabel
);

public record UpdateUsedRequest(bool IsUsed);

public record AddCommentRequest(string Text);

public record PrizeCommentDto(Guid Id, string Text, DateTime CreatedAt);

public record PrizeClaimDto(
    Guid Id,
    string Scope,
    Guid? WeekSummaryId,
    Guid? MonthSummaryId,
    int? Rank,
    Guid ChildId,
    string ChildName,
    string PrizeEmoji,
    string PrizeLabel,
    bool IsUsed,
    DateTime? UsedAt,
    DateTime CreatedAt,
    List<PrizeCommentDto> Comments
);
