using Microsoft.AspNetCore.Identity;

namespace TinyHeroes.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "en";
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool WeeklyEmailEnabled { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FamilyMember> FamilyMemberships { get; set; } = [];
}
