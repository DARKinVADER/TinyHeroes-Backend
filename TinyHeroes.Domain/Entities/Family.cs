namespace TinyHeroes.Domain.Entities;

public class Family
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DayOfWeek WeekStartDay { get; set; } = DayOfWeek.Monday;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FamilyMember> Members { get; set; } = [];
    public ICollection<Child> Children { get; set; } = [];
}
