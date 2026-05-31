namespace TinyHeroes.Application.DTOs.Family;

public record CreateFamilyRequest(string Name, DayOfWeek WeekStartDay);
public record FamilyResponse(Guid Id, string Name, DayOfWeek WeekStartDay, int? WeeklyMinDeeds = null, int? MonthlyMinDeeds = null);
