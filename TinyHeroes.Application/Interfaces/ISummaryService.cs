namespace TinyHeroes.Application.Interfaces;

public interface ISummaryService
{
    Task GenerateMissingWeekSummaries(Guid familyId);
    Task GenerateMissingMonthSummaries(Guid familyId);
}
