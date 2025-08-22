using System.ComponentModel.DataAnnotations;

namespace PhotoService.DTOs;

/// <summary>
/// Data Transfer Object for photo upload requests
/// Validates file upload parameters and metadata
/// </summary>
public class PhotoUploadDto
{
    /// <summary>
    /// Uploaded photo file
    /// Required for photo upload operations
    /// </summary>
    [Required(ErrorMessage = "Photo file is required")]
    public IFormFile Photo { get; set; } = null!;

    /// <summary>
    /// Display order for the photo in user's gallery
    /// If not provided, will be set to next available order
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Display order must be a positive number")]
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Whether this should be set as the primary profile photo
    /// Only one photo per user can be primary
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Optional description/caption for the photo
    /// Currently not stored but available for future use
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}

/// <summary>
/// Data Transfer Object for photo response
/// Returns photo metadata and URLs to frontend
/// </summary>
public class PhotoResponseDto
{
    /// <summary>
    /// Unique photo identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Owner user identifier
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Original filename as uploaded
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Display order in user's photo gallery
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this is the user's primary profile photo
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Photo upload timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Image dimensions
    /// </summary>
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Content moderation status
    /// </summary>
    public string ModerationStatus { get; set; } = string.Empty;

    /// <summary>
    /// Image quality score (1-100)
    /// </summary>
    public int QualityScore { get; set; }

    /// <summary>
    /// URLs for different image sizes
    /// Optimized for responsive frontend display
    /// </summary>
    public PhotoUrlsDto Urls { get; set; } = new();

    /// <summary>
    /// Helper property: Human-readable file size
    /// </summary>
    public string FileSizeFormatted => FormatFileSize(FileSizeBytes);

    /// <summary>
    /// Format file size in human-readable format
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }
}

/// <summary>
/// Data Transfer Object for photo URLs
/// Provides different image sizes for responsive display
/// </summary>
public class PhotoUrlsDto
{
    /// <summary>
    /// Full-size image URL
    /// Original uploaded image (processed)
    /// </summary>
    public string Full { get; set; } = string.Empty;

    /// <summary>
    /// Medium-size image URL (400x400)
    /// Optimized for profile views and cards
    /// </summary>
    public string Medium { get; set; } = string.Empty;

    /// <summary>
    /// Thumbnail image URL (150x150)
    /// Optimized for list views and previews
    /// </summary>
    public string Thumbnail { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for photo update requests
/// Allows updating photo metadata without re-uploading
/// </summary>
public class PhotoUpdateDto
{
    /// <summary>
    /// New display order for the photo
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Display order must be a positive number")]
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Whether this should be set as the primary profile photo
    /// Setting this to true will unset other photos as primary
    /// </summary>
    public bool? IsPrimary { get; set; }
}

/// <summary>
/// Data Transfer Object for bulk photo operations
/// Allows reordering multiple photos in single request
/// </summary>
public class PhotoReorderDto
{
    /// <summary>
    /// List of photo IDs with their new display orders
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one photo must be specified")]
    public List<PhotoOrderItemDto> Photos { get; set; } = new();
}

/// <summary>
/// Individual photo order item for bulk reordering
/// </summary>
public class PhotoOrderItemDto
{
    /// <summary>
    /// Photo identifier
    /// </summary>
    [Required]
    public int PhotoId { get; set; }

    /// <summary>
    /// New display order for this photo
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Display order must be a positive number")]
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Data Transfer Object for photo upload result
/// Returns success/failure information with photo details
/// </summary>
public class PhotoUploadResultDto
{
    /// <summary>
    /// Whether the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Upload warnings (non-fatal issues)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Photo information if upload successful
    /// </summary>
    public PhotoResponseDto? Photo { get; set; }

    /// <summary>
    /// Processing information
    /// </summary>
    public PhotoProcessingInfoDto? ProcessingInfo { get; set; }
}

/// <summary>
/// Data Transfer Object for photo processing information
/// Provides details about image processing operations
/// </summary>
public class PhotoProcessingInfoDto
{
    /// <summary>
    /// Whether image was resized during processing
    /// </summary>
    public bool WasResized { get; set; }

    /// <summary>
    /// Original image dimensions before processing
    /// </summary>
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }

    /// <summary>
    /// Final image dimensions after processing
    /// </summary>
    public int FinalWidth { get; set; }
    public int FinalHeight { get; set; }

    /// <summary>
    /// Whether image format was converted
    /// </summary>
    public bool FormatConverted { get; set; }

    /// <summary>
    /// Original file format
    /// </summary>
    public string OriginalFormat { get; set; } = string.Empty;

    /// <summary>
    /// Final file format after processing
    /// </summary>
    public string FinalFormat { get; set; } = string.Empty;

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Data Transfer Object for user photo summary
/// Provides overview of user's photo collection
/// </summary>
public class UserPhotoSummaryDto
{
    /// <summary>
    /// User identifier
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Total number of active photos
    /// </summary>
    public int TotalPhotos { get; set; }

    /// <summary>
    /// Whether user has a primary photo set
    /// </summary>
    public bool HasPrimaryPhoto { get; set; }

    /// <summary>
    /// Primary photo information if available
    /// </summary>
    public PhotoResponseDto? PrimaryPhoto { get; set; }

    /// <summary>
    /// List of all user photos
    /// </summary>
    public List<PhotoResponseDto> Photos { get; set; } = new();

    /// <summary>
    /// Total storage used by user's photos (in bytes)
    /// </summary>
    public long TotalStorageBytes { get; set; }

    /// <summary>
    /// Number of additional photos user can upload
    /// </summary>
    public int RemainingPhotoSlots => Math.Max(0, PhotoService.Models.PhotoConstants.MaxPhotosPerUser - TotalPhotos);

    /// <summary>
    /// Whether user has reached photo limit
    /// </summary>
    public bool HasReachedPhotoLimit => TotalPhotos >= PhotoService.Models.PhotoConstants.MaxPhotosPerUser;
}
