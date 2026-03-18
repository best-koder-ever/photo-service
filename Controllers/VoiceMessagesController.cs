using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoService.Data;
using PhotoService.Models;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PhotoService.Controllers;

/// <summary>
/// Voice Messages API — upload and retrieve audio voice notes sent in chat.
/// Multiple messages per user (unlike VoicePrompts which is one-per-user).
/// Stored as AAC (.m4a), validated server-side for duration and size.
/// </summary>
[ApiController]
[Route("api/voice-messages")]
[Authorize]
public class VoiceMessagesController : ControllerBase
{
    private readonly PhotoContext _context;
    private readonly ILogger<VoiceMessagesController> _logger;

    public VoiceMessagesController(PhotoContext context, ILogger<VoiceMessagesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Upload a voice message audio file.
    /// POST /api/voice-messages
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile audio)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (audio == null || audio.Length == 0)
                return BadRequest("No audio file provided");

            if (audio.Length > VoiceMessageConstants.MaxFileSizeBytes)
                return BadRequest($"File exceeds {VoiceMessageConstants.MaxFileSizeBytes / 1024}KB limit");

            var mimeType = audio.ContentType?.ToLower() ?? "";
            if (!VoiceMessageConstants.AllowedMimeTypes.Contains(mimeType))
                return BadRequest($"Invalid audio format. Allowed: {string.Join(", ", VoiceMessageConstants.AllowedMimeTypes)}");

            // Parse duration from form data
            var durationStr = Request.Form["duration"].FirstOrDefault();
            double duration = 0;
            if (double.TryParse(durationStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                duration = parsed;
            }

            if (duration < VoiceMessageConstants.MinDurationSeconds || duration > VoiceMessageConstants.MaxDurationSeconds)
                return BadRequest($"Duration must be between {VoiceMessageConstants.MinDurationSeconds} and {VoiceMessageConstants.MaxDurationSeconds} seconds");

            // Store file
            var storedFileName = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid():N}.m4a";
            var directory = Path.Combine("uploads", "voice-messages", userId);
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, storedFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audio.CopyToAsync(stream);
            }

            // Content hash
            string contentHash;
            await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var hashBytes = await SHA256.HashDataAsync(stream);
                contentHash = Convert.ToHexString(hashBytes).ToLower();
            }

            var voiceMessage = new VoiceMessage
            {
                SenderUserId = userId,
                StoredFileName = storedFileName,
                FileSizeBytes = audio.Length,
                DurationSeconds = duration,
                MimeType = mimeType,
                ModerationStatus = "AUTO_APPROVED",
                ContentHash = contentHash,
                CreatedAt = DateTime.UtcNow,
            };

            _context.VoiceMessages.Add(voiceMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Voice message {Id} uploaded by {UserId} ({Size}B, {Duration}s)",
                voiceMessage.Id, userId, audio.Length, duration);

            var url = $"/api/voice-messages/{voiceMessage.Id}";
            return Created(url, new
            {
                id = voiceMessage.Id,
                url,
                durationSeconds = voiceMessage.DurationSeconds,
                fileSizeBytes = voiceMessage.FileSizeBytes,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading voice message");
            return StatusCode(500, "An error occurred while uploading voice message");
        }
    }

    /// <summary>
    /// Get voice message audio by ID.
    /// GET /api/voice-messages/{id}
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAudio(int id)
    {
        var vm = await _context.VoiceMessages
            .Where(v => v.Id == id && !v.IsDeleted)
            .FirstOrDefaultAsync();

        if (vm == null) return NotFound();

        var filePath = vm.FilePath;
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("Voice message file not found: {Path}", filePath);
            return NotFound();
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return File(stream, vm.MimeType, enableRangeProcessing: true);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("No user ID in token");
    }
}
