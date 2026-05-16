namespace TinyHeroes.Application.DTOs.Summary;

public record WeekSummaryResponse(Guid Id, DateTime WeekStart, DateTime WeekEnd, List<RankingEntry> Rankings);
public record MonthSummaryResponse(Guid Id, int Year, int Month, string? ChampionName, Guid? ChampionChildId, int TotalDeeds, List<RankingEntry> Rankings);
public record RankingEntry(Guid ChildId, string ChildName, int DeedCount, int Rank);
public record CurrentMonthResponse(int Year, int Month, List<RankingEntry> Rankings);
