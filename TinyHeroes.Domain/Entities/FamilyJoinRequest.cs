using TinyHeroes.Domain.Enums;

namespace TinyHeroes.Domain.Entities;

public class FamilyJoinRequest
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid RequestedById { get; set; }
    public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedById { get; set; }
    public Family Family { get; set; } = null!;
    public User RequestedBy { get; set; } = null!;
}
