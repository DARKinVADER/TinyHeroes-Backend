using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Domain.Entities;

public class FamilyMember
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public FamilyRole Role { get; set; } = FamilyRole.CoParent;
    public string? Relation { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
    public User User { get; set; } = null!;
}
