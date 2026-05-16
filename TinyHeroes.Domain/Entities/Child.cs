using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Domain.Entities;

public class Child
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public string AvatarEmoji { get; set; } = "🦸";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
}
