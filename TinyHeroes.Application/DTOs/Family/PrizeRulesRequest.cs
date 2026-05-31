using System.ComponentModel.DataAnnotations;

namespace TinyHeroes.Application.DTOs.Family;

public record SetPrizeRulesRequest(
    [Range(0, int.MaxValue)] int? WeeklyMinDeeds,
    [Range(0, int.MaxValue)] int? MonthlyMinDeeds
);
