namespace TinyHeroes.Domain.Entities;

public class Family
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DayOfWeek WeekStartDay { get; set; } = DayOfWeek.Monday;
    public int? WeeklyMinDeeds { get; set; }
    public int? MonthlyMinDeeds { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string JoinCode { get; set; } = string.Empty;

    public ICollection<FamilyMember> Members { get; set; } = [];
    public ICollection<Child> Children { get; set; } = [];
    public ICollection<FamilyInvite> Invites { get; set; } = [];
    public ICollection<FamilyJoinRequest> JoinRequests { get; set; } = [];
}
