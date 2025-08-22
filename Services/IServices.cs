using PhotoService.DTOs;
using PhotoService.Models;

namespace PhotoService.Services;

/// <summary>
/// Interface for Photo Service business logic operations
/// Defines standard CRUD operations and photo-specific functionality
/// </summary>
public interface IPhotoService
{
    /// <summary>
    /// Upload and process a new photo for a user
    /// Handles file validation, image processing, and storage
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="uploadDto">Photo upload request data</param>
    /// <returns>Upload result with photo information or error details</returns>
    Task<PhotoUploadResultDto> UploadPhotoAsync(int userId, PhotoUploadDto uploadDto);

    /// <summary>
    /// Get all photos for a specific user
    /// Returns photos ordered by display order, excluding deleted photos
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>User's photo collection summary</returns>
    Task<UserPhotoSummaryDto> GetUserPhotosAsync(int userId);

    /// <summary>
    /// Get a specific photo by ID with ownership validation
    /// Ensures user can only access their own photos
    /// </summary>
    /// <param name="photoId">Photo identifier</param>
    /// <param name="userId">Requesting user identifier</param>
    /// <returns>Photo details or null if not found/unauthorized</returns>
    Task<PhotoResponseDto?> GetPhotoAsync(int photoId, int userId);

    /// <summary>
    /// Get user's primary profile photo
    /// Returns the photo marked as primary, or first photo if none marked
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Primary photo or null if user has no photos</returns>
    Task<PhotoResponseDto?> GetPrimaryPhotoAsync(int userId);

    /// <summary>
    /// Update photo metadata (order, primary status)
    /// Does not modify the actual image file
    /// </summary>
    /// <param name="photoId">Photo identifier</param>
    /// <param name="userId">User identifier for authorization</param>
    /// <param name="updateDto">Update request data</param>
    /// <returns>Updated photo details or null if not found/unauthorized</returns>
    Task<PhotoResponseDto?> UpdatePhotoAsync(int photoId, int userId, PhotoUpdateDto updateDto);

    /// <summary>
    /// Reorder multiple photos in a single operation
    /// Atomic operation to prevent display order conflicts
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="reorderDto">Reorder request with new photo positions</param>
    /// <returns>Updated photo collection</returns>
    Task<UserPhotoSummaryDto> ReorderPhotosAsync(int userId, PhotoReorderDto reorderDto);

    /// <summary>
    /// Set a specific photo as the user's primary profile photo
    /// Automatically unsets other photos as primary
    /// </summary>
    /// <param name="photoId">Photo identifier to set as primary</param>
    /// <param name="userId">User identifier for authorization</param>
    /// <returns>Success status</returns>
    Task<bool> SetPrimaryPhotoAsync(int photoId, int userId);

    /// <summary>
    /// Soft delete a photo (mark as deleted without removing file)
    /// Maintains referential integrity and allows for recovery
    /// </summary>
    /// <param name="photoId">Photo identifier</param>
    /// <param name="userId">User identifier for authorization</param>
    /// <returns>Success status</returns>
    Task<bool> DeletePhotoAsync(int photoId, int userId);

    /// <summary>
    /// Get photo file stream for serving images
    /// Returns different sizes based on request type
    /// </summary>
    /// <param name="photoId">Photo identifier</param>
    /// <param name="size">Requested image size (full, medium, thumbnail)</param>
    /// <returns>Image stream and content type</returns>
    Task<(Stream? stream, string contentType, string fileName)> GetPhotoStreamAsync(int photoId, string size = "full");

    /// <summary>
    /// Validate if user can upload more photos
    /// Checks against maximum photo limit per user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>True if user can upload more photos</returns>
    Task<bool> CanUserUploadMorePhotosAsync(int userId);

    /// <summary>
    /// Get photos pending moderation review
    /// Admin function for content moderation workflow
    /// </summary>
    /// <param name="status">Moderation status filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of photos for review</returns>
    Task<(List<PhotoResponseDto> photos, int totalCount)> GetPhotosForModerationAsync(
        string status = Models.ModerationStatus.PendingReview, 
        int pageNumber = 1, 
        int pageSize = 50);

    /// <summary>
    /// Update photo moderation status
    /// Admin function for approving/rejecting photos
    /// </summary>
    /// <param name="photoId">Photo identifier</param>
    /// <param name="status">New moderation status</param>
    /// <param name="notes">Moderation notes</param>
    /// <returns>Success status</returns>
    Task<bool> UpdateModerationStatusAsync(int photoId, string status, string? notes = null);
}

/// <summary>
/// Interface for image processing operations
/// Handles image manipulation, resizing, and format conversion
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Process uploaded image: resize, optimize, and convert format if needed
    /// Creates multiple sizes for responsive display
    /// </summary>
    /// <param name="inputStream">Original image stream</param>
    /// <param name="originalFileName">Original file name for format detection</param>
    /// <returns>Processing result with image data and metadata</returns>
    Task<ImageProcessingResult> ProcessImageAsync(Stream inputStream, string originalFileName);

    /// <summary>
    /// Generate thumbnail version of an image
    /// Standard size for list views and previews
    /// </summary>
    /// <param name="inputStream">Source image stream</param>
    /// <param name="width">Thumbnail width (default: 150px)</param>
    /// <param name="height">Thumbnail height (default: 150px)</param>
    /// <returns>Thumbnail image data</returns>
    Task<byte[]> GenerateThumbnailAsync(Stream inputStream, int width = 150, int height = 150);

    /// <summary>
    /// Generate medium-sized version of an image
    /// Balanced quality/size for profile views
    /// </summary>
    /// <param name="inputStream">Source image stream</param>
    /// <param name="width">Medium width (default: 400px)</param>
    /// <param name="height">Medium height (default: 400px)</param>
    /// <returns>Medium-sized image data</returns>
    Task<byte[]> GenerateMediumAsync(Stream inputStream, int width = 400, int height = 400);

    /// <summary>
    /// Validate image file format and content
    /// Ensures file is actually an image and format is supported
    /// </summary>
    /// <param name="stream">File stream to validate</param>
    /// <param name="fileName">Original file name</param>
    /// <returns>Validation result with format information</returns>
    Task<ImageValidationResult> ValidateImageAsync(Stream stream, string fileName);

    /// <summary>
    /// Calculate image quality score based on resolution, clarity, etc.
    /// Used for automatic quality filtering
    /// </summary>
    /// <param name="stream">Image stream to analyze</param>
    /// <returns>Quality score from 1-100</returns>
    Task<int> CalculateQualityScoreAsync(Stream stream);
}

/// <summary>
/// Interface for file storage operations
/// Abstracts storage implementation (local, cloud, etc.)
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Store image file and generate unique filename
    /// Creates directory structure and handles naming conflicts
    /// </summary>
    /// <param name="stream">File content stream</param>
    /// <param name="userId">User identifier for directory organization</param>
    /// <param name="originalFileName">Original file name</param>
    /// <param name="suffix">Optional suffix for different sizes (thumbnail, medium)</param>
    /// <returns>Storage result with file path and metadata</returns>
    Task<StorageResult> StoreImageAsync(Stream stream, int userId, string originalFileName, string suffix = "");

    /// <summary>
    /// Retrieve image file stream
    /// Returns file content for serving to clients
    /// </summary>
    /// <param name="filePath">Stored file path</param>
    /// <returns>File stream or null if not found</returns>
    Task<Stream?> GetImageStreamAsync(string filePath);

    /// <summary>
    /// Delete image file from storage
    /// Physical file deletion (used with soft delete in database)
    /// </summary>
    /// <param name="filePath">File path to delete</param>
    /// <returns>Success status</returns>
    Task<bool> DeleteImageAsync(string filePath);

    /// <summary>
    /// Check if image file exists in storage
    /// Used for validation and cleanup operations
    /// </summary>
    /// <param name="filePath">File path to check</param>
    /// <returns>True if file exists</returns>
    Task<bool> ImageExistsAsync(string filePath);

    /// <summary>
    /// Get file size in bytes
    /// Used for storage quota calculations
    /// </summary>
    /// <param name="filePath">File path to check</param>
    /// <returns>File size in bytes or 0 if not found</returns>
    Task<long> GetFileSizeAsync(string filePath);

    /// <summary>
    /// Clean up orphaned files
    /// Removes files that no longer have database references
    /// </summary>
    /// <param name="validFilePaths">List of currently valid file paths</param>
    /// <returns>Number of files cleaned up</returns>
    Task<int> CleanupOrphanedFilesAsync(List<string> validFilePaths);
}

/// <summary>
/// Result object for image processing operations
/// Contains processed image data and processing metadata
/// </summary>
public class ImageProcessingResult
{
    /// <summary>
    /// Processed image data (full size)
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Thumbnail version of the image
    /// </summary>
    public byte[] ThumbnailData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Medium-sized version of the image
    /// </summary>
    public byte[] MediumData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Final image dimensions after processing
    /// </summary>
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// Final file format after processing
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// File extension for the processed image
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Whether image was resized during processing
    /// </summary>
    public bool WasResized { get; set; }

    /// <summary>
    /// Original dimensions before processing
    /// </summary>
    public int OriginalWidth { get; set; }
    public int OriginalHeight { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Image quality score
    /// </summary>
    public int QualityScore { get; set; }
}

/// <summary>
/// Result object for image validation operations
/// </summary>
public class ImageValidationResult
{
    /// <summary>
    /// Whether the file is a valid image
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Detected image format
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Image dimensions
    /// </summary>
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Validation error message if invalid
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result object for file storage operations
/// </summary>
public class StorageResult
{
    /// <summary>
    /// Whether storage operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Generated file path for stored file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Generated filename for stored file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Error message if storage failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
