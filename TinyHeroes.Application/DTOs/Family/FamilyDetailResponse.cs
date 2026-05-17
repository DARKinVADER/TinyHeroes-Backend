namespace TinyHeroes.Application.DTOs.Family;

public record FamilyDetailResponse(Guid Id, string Name, DayOfWeek WeekStartDay, List<FamilyMemberResponse> Members);
public record FamilyMemberResponse(Guid UserId, string DisplayName, string Email, string Role);
public record UpdateFamilyRequest(string Name, DayOfWeek WeekStartDay);
