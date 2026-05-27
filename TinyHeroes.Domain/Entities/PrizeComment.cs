// backend/TinyHeroes.Domain/Entities/PrizeComment.cs
namespace TinyHeroes.Domain.Entities;

public class PrizeComment
{
    public Guid Id { get; set; }
    public Guid PrizeClaimId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PrizeClaim PrizeClaim { get; set; } = null!;
}
