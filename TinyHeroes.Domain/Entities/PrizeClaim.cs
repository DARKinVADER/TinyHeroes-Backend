// backend/TinyHeroes.Domain/Entities/PrizeClaim.cs
namespace TinyHeroes.Domain.Entities;

public class PrizeClaim
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Scope { get; set; } = "weekly";
    public Guid? WeekSummaryId { get; set; }
    public Guid? MonthSummaryId { get; set; }
    public int? Rank { get; set; }
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string PrizeEmoji { get; set; } = string.Empty;
    public string PrizeLabel { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public WeekSummary? WeekSummary { get; set; }
    public MonthSummary? MonthSummary { get; set; }
    public ICollection<PrizeComment> Comments { get; set; } = [];
}
