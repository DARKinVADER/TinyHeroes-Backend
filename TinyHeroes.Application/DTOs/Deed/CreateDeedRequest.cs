namespace TinyHeroes.Application.DTOs.Deed;

public record CreateDeedRequest(Guid ChildId, string Description, string ImageValue);
public record DeedResponse(Guid Id, Guid ChildId, string Description, string ImageType, string ImageValue, string AddedByName, DateTime CreatedAt);
public record ChildStatsResponse(Guid ChildId, int WeeklyCount, int TotalCount);
