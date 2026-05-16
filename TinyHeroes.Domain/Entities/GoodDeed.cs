namespace TinyHeroes.Domain.Entities;

public class GoodDeed
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public Guid AddedByUserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ImageType { get; set; } = "library";
    public string ImageValue { get; set; } = "⭐";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Child Child { get; set; } = null!;
    public User AddedBy { get; set; } = null!;
}
