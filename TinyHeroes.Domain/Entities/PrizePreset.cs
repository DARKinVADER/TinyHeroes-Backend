namespace TinyHeroes.Domain.Entities;

public class PrizePreset
{
    public Guid Id { get; set; }
    public Guid? FamilyId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🎁";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
