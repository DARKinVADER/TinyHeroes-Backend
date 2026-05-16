namespace TinyHeroes.Domain.Entities;

public class FamilyInvite
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string? Email { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Accepted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family Family { get; set; } = null!;
}
