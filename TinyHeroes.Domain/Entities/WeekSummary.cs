namespace TinyHeroes.Domain.Entities;

public class WeekSummary
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string Rankings { get; set; } = "[]";
    public string? PrizesAwarded { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
}
