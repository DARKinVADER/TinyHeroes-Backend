using System.ComponentModel.DataAnnotations;

namespace TinyHeroes.Application.DTOs.Feedback;

public class FeedbackRequest
{
    [Required]
    [RegularExpression("^(bug|idea|general)$", ErrorMessage = "Category must be bug, idea, or general.")]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }
}
