using Microsoft.EntityFrameworkCore;
using PhotoService.Data;
using PhotoService.DTOs;
using PhotoService.Models;

namespace PhotoService.Services;

/// <summary>
/// Photo Service implementation - Core business logic for photo management
/// Handles photo CRUD operations, validation, and integration with storage/processing
/// </summary>
public class PhotoService : IPhotoService
{
    private readonly PhotoContext _context;
    private readonly IImageProcessingService _imageProcessing;
    private readonly IStorageService _storage;
    private readonly ILogger<PhotoService> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// Standard service layer pattern with EF Core and custom services
    /// </summary>
    public PhotoService(
        PhotoContext context,
        IImageProcessingService imageProcessing,
        IStorageService storage,
        ILogger<PhotoService> logger)
    {
        _context = context;
        _imageProcessing = imageProcessing;
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Upload and process a new photo for a user
    /// Comprehensive photo upload with validation, processing, and storage
    /// </summary>
    public async Task<PhotoUploadResultDto> UploadPhotoAsync(int userId, PhotoUploadDto uploadDto)
    {
        var result = new PhotoUploadResultDto();

        try
        {
            _logger.LogInformation("Starting photo upload for user {UserId}", userId);

            // ================================
            // VALIDATION PHASE
            // Comprehensive validation before processing
            // ================================

            // Check if user can upload more photos
            if (!await CanUserUploadMorePhotosAsync(userId))
            {
                result.ErrorMessage = $"Maximum photo limit reached ({PhotoConstants.MaxPhotosPerUser} photos per user)";
                return result;
            }

            // Validate file format and content
            using var validationStream = uploadDto.Photo.OpenReadStream();
            var validation = await _imageProcessing.ValidateImageAsync(validationStream, uploadDto.Photo.FileName);
            
            if (!validation.IsValid)
            {
                result.ErrorMessage = validation.ErrorMessage ?? "Invalid image file";
                return result;
            }

            // Add validation warnings to result
            result.Warnings.AddRange(validation.Warnings);

            // Check file size
            if (uploadDto.Photo.Length > PhotoConstants.MaxFileSizeBytes)
            {
                result.ErrorMessage = $"File size exceeds maximum limit of {PhotoConstants.MaxFileSizeBytes / (1024 * 1024)} MB";
                return result;
            }

            // ================================
            // PROCESSING PHASE
            // Image processing and storage preparation
            // ================================

            PhotoProcessingInfoDto processingInfo;
            ImageProcessingResult processedImage;

            // Process the image
            using (var processingStream = uploadDto.Photo.OpenReadStream())
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                processedImage = await _imageProcessing.ProcessImageAsync(processingStream, uploadDto.Photo.FileName);
                stopwatch.Stop();

                processingInfo = new PhotoProcessingInfoDto
                {
                    WasResized = processedImage.WasResized,
                    OriginalWidth = processedImage.OriginalWidth,
                    OriginalHeight = processedImage.OriginalHeight,
                    FinalWidth = processedImage.Width,
                    FinalHeight = processedImage.Height,
                    FormatConverted = processedImage.Format != validation.Format,
                    OriginalFormat = validation.Format,
                    FinalFormat = processedImage.Format,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // ================================
            // STORAGE PHASE
            // Store processed images in multiple sizes
            // ================================

            StorageResult fullSizeStorage;
            StorageResult thumbnailStorage;
            StorageResult mediumStorage;

            // Store full-size image
            using (var fullStream = new MemoryStream(processedImage.ImageData))
            {
                fullSizeStorage = await _storage.StoreImageAsync(fullStream, userId, uploadDto.Photo.FileName);
                if (!fullSizeStorage.Success)
                {
                    result.ErrorMessage = $"Failed to store image: {fullSizeStorage.ErrorMessage}";
                    return result;
                }
            }

            // Store thumbnail
            using (var thumbStream = new MemoryStream(processedImage.ThumbnailData))
            {
                thumbnailStorage = await _storage.StoreImageAsync(thumbStream, userId, uploadDto.Photo.FileName, "_thumb");
                if (!thumbnailStorage.Success)
                {
                    // Cleanup full-size image on thumbnail failure
                    await _storage.DeleteImageAsync(fullSizeStorage.FilePath);
                    result.ErrorMessage = $"Failed to store thumbnail: {thumbnailStorage.ErrorMessage}";
                    return result;
                }
            }

            // Store medium-size image
            using (var mediumStream = new MemoryStream(processedImage.MediumData))
            {
                mediumStorage = await _storage.StoreImageAsync(mediumStream, userId, uploadDto.Photo.FileName, "_medium");
                if (!mediumStorage.Success)
                {
                    // Cleanup previously stored images on medium failure
                    await _storage.DeleteImageAsync(fullSizeStorage.FilePath);
                    await _storage.DeleteImageAsync(thumbnailStorage.FilePath);
                    result.ErrorMessage = $"Failed to store medium image: {mediumStorage.ErrorMessage}";
                    return result;
                }
            }

            // ================================
            // DATABASE PHASE
            // Create photo record with metadata
            // ================================

            // Determine display order
            int displayOrder = uploadDto.DisplayOrder ?? await GetNextDisplayOrderAsync(userId);

            // Handle primary photo logic
            if (uploadDto.IsPrimary)
            {
                await UnsetAllPrimaryPhotosAsync(userId);
            }

            // Create photo entity
            var photo = new Photo
            {
                UserId = userId,
                OriginalFileName = uploadDto.Photo.FileName,
                StoredFileName = Path.GetFileName(fullSizeStorage.FilePath),
                FileExtension = processedImage.Extension,
                FileSizeBytes = fullSizeStorage.FileSize,
                Width = processedImage.Width,
                Height = processedImage.Height,
                DisplayOrder = displayOrder,
                IsPrimary = uploadDto.IsPrimary,
                QualityScore = processedImage.QualityScore,
                ModerationStatus = DetermineModerationStatus(processedImage.QualityScore),
                CreatedAt = DateTime.UtcNow
            };

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            // ================================
            // SUCCESS RESPONSE
            // Build complete response with photo details
            // ================================

            result.Success = true;
            result.ProcessingInfo = processingInfo;
            result.Photo = new PhotoResponseDto
            {
                Id = photo.Id,
                UserId = photo.UserId,
                OriginalFileName = photo.OriginalFileName,
                DisplayOrder = photo.DisplayOrder,
                IsPrimary = photo.IsPrimary,
                CreatedAt = photo.CreatedAt,
                Width = photo.Width,
                Height = photo.Height,
                FileSizeBytes = photo.FileSizeBytes,
                ModerationStatus = photo.ModerationStatus,
                QualityScore = photo.QualityScore,
                Urls = new PhotoUrlsDto
                {
                    Full = $"/api/photos/{photo.Id}/image",
                    Medium = $"/api/photos/{photo.Id}/medium",
                    Thumbnail = $"/api/photos/{photo.Id}/thumbnail"
                }
            };

            _logger.LogInformation("Photo upload completed successfully for user {UserId}, photo ID {PhotoId}", 
                userId, photo.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for user {UserId}", userId);
            result.ErrorMessage = "An error occurred while uploading the photo. Please try again.";
            return result;
        }
    }

    /// <summary>
    /// Get all photos for a specific user
    /// Returns complete photo collection with metadata
    /// </summary>
    public async Task<UserPhotoSummaryDto> GetUserPhotosAsync(int userId)
    {
        var photos = await _context.Photos
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        var photoResponses = photos.Select(p => new PhotoResponseDto
        {
            Id = p.Id,
            UserId = p.UserId,
            OriginalFileName = p.OriginalFileName,
            DisplayOrder = p.DisplayOrder,
            IsPrimary = p.IsPrimary,
            CreatedAt = p.CreatedAt,
            Width = p.Width,
            Height = p.Height,
            FileSizeBytes = p.FileSizeBytes,
            ModerationStatus = p.ModerationStatus,
            QualityScore = p.QualityScore,
            Urls = new PhotoUrlsDto
            {
                Full = $"/api/photos/{p.Id}/image",
                Medium = $"/api/photos/{p.Id}/medium",
                Thumbnail = $"/api/photos/{p.Id}/thumbnail"
            }
        }).ToList();

        var primaryPhoto = photoResponses.FirstOrDefault(p => p.IsPrimary);

        return new UserPhotoSummaryDto
        {
            UserId = userId,
            TotalPhotos = photos.Count,
            HasPrimaryPhoto = primaryPhoto != null,
            PrimaryPhoto = primaryPhoto,
            Photos = photoResponses,
            TotalStorageBytes = photos.Sum(p => p.FileSizeBytes)
        };
    }

    /// <summary>
    /// Get a specific photo by ID with ownership validation
    /// </summary>
    public async Task<PhotoResponseDto?> GetPhotoAsync(int photoId, int userId)
    {
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId && !p.IsDeleted);

        if (photo == null)
            return null;

        return new PhotoResponseDto
        {
            Id = photo.Id,
            UserId = photo.UserId,
            OriginalFileName = photo.OriginalFileName,
            DisplayOrder = photo.DisplayOrder,
            IsPrimary = photo.IsPrimary,
            CreatedAt = photo.CreatedAt,
            Width = photo.Width,
            Height = photo.Height,
            FileSizeBytes = photo.FileSizeBytes,
            ModerationStatus = photo.ModerationStatus,
            QualityScore = photo.QualityScore,
            Urls = new PhotoUrlsDto
            {
                Full = $"/api/photos/{photo.Id}/image",
                Medium = $"/api/photos/{photo.Id}/medium",
                Thumbnail = $"/api/photos/{photo.Id}/thumbnail"
            }
        };
    }

    /// <summary>
    /// Get user's primary profile photo
    /// </summary>
    public async Task<PhotoResponseDto?> GetPrimaryPhotoAsync(int userId)
    {
        var photo = await _context.Photos
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.DisplayOrder)
            .FirstOrDefaultAsync();

        if (photo == null)
            return null;

        return new PhotoResponseDto
        {
            Id = photo.Id,
            UserId = photo.UserId,
            OriginalFileName = photo.OriginalFileName,
            DisplayOrder = photo.DisplayOrder,
            IsPrimary = photo.IsPrimary,
            CreatedAt = photo.CreatedAt,
            Width = photo.Width,
            Height = photo.Height,
            FileSizeBytes = photo.FileSizeBytes,
            ModerationStatus = photo.ModerationStatus,
            QualityScore = photo.QualityScore,
            Urls = new PhotoUrlsDto
            {
                Full = $"/api/photos/{photo.Id}/image",
                Medium = $"/api/photos/{photo.Id}/medium",
                Thumbnail = $"/api/photos/{photo.Id}/thumbnail"
            }
        };
    }

    /// <summary>
    /// Update photo metadata
    /// </summary>
    public async Task<PhotoResponseDto?> UpdatePhotoAsync(int photoId, int userId, PhotoUpdateDto updateDto)
    {
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId && !p.IsDeleted);

        if (photo == null)
            return null;

        // Handle primary photo update
        if (updateDto.IsPrimary.HasValue && updateDto.IsPrimary.Value && !photo.IsPrimary)
        {
            await UnsetAllPrimaryPhotosAsync(userId);
            photo.IsPrimary = true;
        }
        else if (updateDto.IsPrimary.HasValue && !updateDto.IsPrimary.Value && photo.IsPrimary)
        {
            photo.IsPrimary = false;
        }

        // Handle display order update
        if (updateDto.DisplayOrder.HasValue)
        {
            photo.DisplayOrder = updateDto.DisplayOrder.Value;
        }

        photo.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetPhotoAsync(photoId, userId);
    }

    /// <summary>
    /// Reorder multiple photos in a single operation
    /// </summary>
    public async Task<UserPhotoSummaryDto> ReorderPhotosAsync(int userId, PhotoReorderDto reorderDto)
    {
        var photos = await _context.Photos
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .ToListAsync();

        foreach (var orderItem in reorderDto.Photos)
        {
            var photo = photos.FirstOrDefault(p => p.Id == orderItem.PhotoId);
            if (photo != null)
            {
                photo.DisplayOrder = orderItem.DisplayOrder;
                photo.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return await GetUserPhotosAsync(userId);
    }

    /// <summary>
    /// Set a specific photo as the user's primary profile photo
    /// </summary>
    public async Task<bool> SetPrimaryPhotoAsync(int photoId, int userId)
    {
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId && !p.IsDeleted);

        if (photo == null)
            return false;

        await UnsetAllPrimaryPhotosAsync(userId);
        
        photo.IsPrimary = true;
        photo.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Soft delete a photo
    /// </summary>
    public async Task<bool> DeletePhotoAsync(int photoId, int userId)
    {
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId && !p.IsDeleted);

        if (photo == null)
            return false;

        // If deleting primary photo, set next photo as primary
        if (photo.IsPrimary)
        {
            var nextPhoto = await _context.Photos
                .Where(p => p.UserId == userId && p.Id != photoId && !p.IsDeleted)
                .OrderBy(p => p.DisplayOrder)
                .FirstOrDefaultAsync();

            if (nextPhoto != null)
            {
                nextPhoto.IsPrimary = true;
                nextPhoto.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Soft delete
        photo.IsDeleted = true;
        photo.DeletedAt = DateTime.UtcNow;
        photo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // TODO: Schedule physical file deletion (can be done async)
        // For now, files remain for potential recovery

        return true;
    }

    /// <summary>
    /// Get photo file stream for serving images
    /// </summary>
    public async Task<(Stream? stream, string contentType, string fileName)> GetPhotoStreamAsync(int photoId, string size = "full")
    {
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && !p.IsDeleted);

        if (photo == null)
            return (null, string.Empty, string.Empty);

        string filePath = size.ToLower() switch
        {
            "thumbnail" => photo.FilePath.Replace(photo.StoredFileName, photo.StoredFileName.Replace(".", "_thumb.")),
            "medium" => photo.FilePath.Replace(photo.StoredFileName, photo.StoredFileName.Replace(".", "_medium.")),
            _ => photo.FilePath
        };

        var stream = await _storage.GetImageStreamAsync(filePath);
        var contentType = GetContentType(photo.FileExtension);
        var fileName = size == "full" ? photo.OriginalFileName : $"{Path.GetFileNameWithoutExtension(photo.OriginalFileName)}_{size}{photo.FileExtension}";

        return (stream, contentType, fileName);
    }

    /// <summary>
    /// Validate if user can upload more photos
    /// </summary>
    public async Task<bool> CanUserUploadMorePhotosAsync(int userId)
    {
        var currentCount = await _context.Photos
            .CountAsync(p => p.UserId == userId && !p.IsDeleted);

        return currentCount < PhotoConstants.MaxPhotosPerUser;
    }

    /// <summary>
    /// Get photos pending moderation review
    /// </summary>
    public async Task<(List<PhotoResponseDto> photos, int totalCount)> GetPhotosForModerationAsync(
        string status = Models.ModerationStatus.PendingReview, 
        int pageNumber = 1, 
        int pageSize = 50)
    {
        var query = _context.Photos
            .Where(p => p.ModerationStatus == status && !p.IsDeleted);

        var totalCount = await query.CountAsync();

        var photos = await query
            .OrderBy(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PhotoResponseDto
            {
                Id = p.Id,
                UserId = p.UserId,
                OriginalFileName = p.OriginalFileName,
                DisplayOrder = p.DisplayOrder,
                IsPrimary = p.IsPrimary,
                CreatedAt = p.CreatedAt,
                Width = p.Width,
                Height = p.Height,
                FileSizeBytes = p.FileSizeBytes,
                ModerationStatus = p.ModerationStatus,
                QualityScore = p.QualityScore,
                Urls = new PhotoUrlsDto
                {
                    Full = $"/api/photos/{p.Id}/image",
                    Medium = $"/api/photos/{p.Id}/medium",
                    Thumbnail = $"/api/photos/{p.Id}/thumbnail"
                }
            })
            .ToListAsync();

        return (photos, totalCount);
    }

    /// <summary>
    /// Update photo moderation status
    /// </summary>
    public async Task<bool> UpdateModerationStatusAsync(int photoId, string status, string? notes = null)
    {
        var photo = await _context.Photos
            .FirstOrDefaultAsync(p => p.Id == photoId && !p.IsDeleted);

        if (photo == null)
            return false;

        photo.ModerationStatus = status;
        photo.ModerationNotes = notes;
        photo.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // ================================
    // PRIVATE HELPER METHODS
    // Internal business logic utilities
    // ================================

    /// <summary>
    /// Get next available display order for user's photos
    /// </summary>
    private async Task<int> GetNextDisplayOrderAsync(int userId)
    {
        var maxOrder = await _context.Photos
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .MaxAsync(p => (int?)p.DisplayOrder) ?? 0;

        return maxOrder + 1;
    }

    /// <summary>
    /// Unset primary flag for all user's photos
    /// </summary>
    private async Task UnsetAllPrimaryPhotosAsync(int userId)
    {
        var primaryPhotos = await _context.Photos
            .Where(p => p.UserId == userId && p.IsPrimary && !p.IsDeleted)
            .ToListAsync();

        foreach (var photo in primaryPhotos)
        {
            photo.IsPrimary = false;
            photo.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Determine moderation status based on quality score
    /// </summary>
    private static string DetermineModerationStatus(int qualityScore)
    {
        return qualityScore >= 70 ? Models.ModerationStatus.AutoApproved : Models.ModerationStatus.PendingReview;
    }

    /// <summary>
    /// Get MIME content type from file extension
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
