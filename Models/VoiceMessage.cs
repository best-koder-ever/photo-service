using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhotoService.Models;

/// <summary>
/// Voice message entity — audio sent in chat, stored as AAC.
/// Unlike VoicePrompt (one per user), users can send many voice messages.
/// </summary>
[Table("voice_messages")]
public class VoiceMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Sender user ID (from Keycloak sub)</summary>
    [Required]
    [MaxLength(128)]
    public string SenderUserId { get; set; } = string.Empty;

    /// <summary>Stored filename: {userId}_{timestamp}_{guid}.m4a</summary>
    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>File size in bytes</summary>
    [Required]
    public long FileSizeBytes { get; set; }

    /// <summary>Duration in seconds (client-reported)</summary>
    [Required]
    public double DurationSeconds { get; set; }

    /// <summary>MIME type — always audio/mp4 for AAC</summary>
    [Required]
    [MaxLength(50)]
    public string MimeType { get; set; } = "audio/mp4";

    /// <summary>Content moderation status</summary>
    [Required]
    [MaxLength(20)]
    public string ModerationStatus { get; set; } = "AUTO_APPROVED";

    /// <summary>SHA-256 content hash for integrity</summary>
    [MaxLength(64)]
    public string? ContentHash { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    /// <summary>Calculated file path for storage</summary>
    [NotMapped]
    public string FilePath => $"uploads/voice-messages/{SenderUserId}/{StoredFileName}";
}

/// <summary>Voice message constants</summary>
public static class VoiceMessageConstants
{
    public const int MinDurationSeconds = 1;
    public const int MaxDurationSeconds = 60;
    public const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB
    public static readonly string[] AllowedMimeTypes = { "audio/mp4", "audio/aac", "audio/m4a", "audio/mpeg" };
}
