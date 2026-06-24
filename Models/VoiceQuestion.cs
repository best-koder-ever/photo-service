using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoService.Models;

/// <summary>
/// Voice question entity — a pool of questions users answer during onboarding.
/// Questions are flavor-scoped (nullable FlavorId means available to all flavors).
/// </summary>
[Table("voice_questions")]
public class VoiceQuestion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Question text shown to the user (Swedish primary, English fallback)</summary>
    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>English translation for i18n</summary>
    [MaxLength(500)]
    public string? QuestionTextEn { get; set; }

    /// <summary>Display order when presenting questions</summary>
    [Required]
    public int QuestionOrder { get; set; }

    /// <summary>Flavor scope — null means available to all flavors</summary>
    [MaxLength(50)]
    public string? FlavorId { get; set; }

    /// <summary>Whether this question is active (can be shown to users)</summary>
    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; }
}
