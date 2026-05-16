namespace TinyHeroes.Domain.Entities;

public class MonthSummary
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? ChampionChildId { get; set; }
    public string? ChampionName { get; set; }
    public int TotalDeeds { get; set; }
    public string Rankings { get; set; } = "[]";
    public string? PrizeAwarded { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public Child? Champion { get; set; }
}
