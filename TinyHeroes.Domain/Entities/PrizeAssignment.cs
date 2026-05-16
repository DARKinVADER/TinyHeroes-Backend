namespace TinyHeroes.Domain.Entities;

public class PrizeAssignment
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Scope { get; set; } = "weekly"; // "weekly" or "monthly"
    public int? Rank { get; set; } // 1, 2, 3 for weekly; null for monthly
    public string Emoji { get; set; } = "🎁";
    public string Label { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
}
