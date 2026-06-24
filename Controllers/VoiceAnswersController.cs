using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoService.Data;
using PhotoService.Models;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PhotoService.Controllers;

/// <summary>
/// Voice Answers API — questions pool + user answer upload/retrieval.
/// Used by the Voice flavor for blind dating onboarding.
///
/// Flow:
///   GET /questions?flavorId=voice → get 10 questions (client picks 3)
///   POST /{questionId} → upload answer audio (AAC, 3-30s)
///   GET /my → get current user's answers
///   GET /user/{userId} → get another user's answers (for discovery cards)
/// </summary>
[ApiController]
[Route("api/voice-answers")]
[Authorize]
public class VoiceAnswersController : ControllerBase
{
    private readonly PhotoContext _context;
    private readonly ILogger<VoiceAnswersController> _logger;

    public VoiceAnswersController(PhotoContext context, ILogger<VoiceAnswersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get available voice questions for a flavor.
    /// GET /api/voice-answers/questions?flavorId=voice
    /// </summary>
    [HttpGet("questions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuestions([FromQuery] string? flavorId = "voice")
    {
        var questions = await _context.VoiceQuestions
            .Where(q => q.IsActive && (q.FlavorId == null || q.FlavorId == flavorId))
            .OrderBy(q => q.QuestionOrder)
            .Select(q => new
            {
                q.Id,
                q.QuestionText,
                q.QuestionTextEn,
                q.QuestionOrder,
            })
            .ToListAsync();

        return Ok(questions);
    }

    /// <summary>
    /// Upload a voice answer for a specific question.
    /// Replaces existing answer for same question (soft-delete old one).
    /// POST /api/voice-answers/{questionId}
    /// </summary>
    [HttpPost("{questionId:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(int questionId, IFormFile audio)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Validate question exists
            var question = await _context.VoiceQuestions
                .Where(q => q.Id == questionId && q.IsActive)
                .FirstOrDefaultAsync();

            if (question == null)
                return NotFound("Question not found or inactive");

            // Validate audio
            if (audio == null || audio.Length == 0)
                return BadRequest("No audio file provided");

            if (audio.Length > VoicePromptConstants.MaxFileSizeBytes)
                return BadRequest($"File exceeds {VoicePromptConstants.MaxFileSizeBytes / 1024}KB limit");

            var mimeType = audio.ContentType?.ToLower() ?? "";
            if (!VoicePromptConstants.AllowedMimeTypes.Contains(mimeType))
                return BadRequest($"Invalid audio format. Allowed: {string.Join(", ", VoicePromptConstants.AllowedMimeTypes)}");

            // Store file
            var storedFileName = $"{userId}_{questionId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid():N}.m4a";
            var directory = Path.Combine("uploads", "voice-answers", userId.ToString());
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

            // Soft-delete existing answer for this question
            var existing = await _context.VoiceAnswers
                .Where(a => a.UserId == userId && a.QuestionId == questionId && !a.IsDeleted)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.IsDeleted = true;
                existing.DeletedAt = DateTime.UtcNow;
                _logger.LogInformation("Soft-deleted previous voice answer {Id} for user {UserId} question {QuestionId}",
                    existing.Id, userId, questionId);
            }

            // Parse duration from form data
            var durationStr = Request.Form["duration"].FirstOrDefault();
            double duration = 15;
            if (double.TryParse(durationStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                duration = parsed;
            }

            if (duration < VoicePromptConstants.MinDurationSeconds || duration > VoicePromptConstants.MaxDurationSeconds)
                return BadRequest($"Duration must be between {VoicePromptConstants.MinDurationSeconds} and {VoicePromptConstants.MaxDurationSeconds} seconds");

            // Create entity
            var answer = new VoiceAnswer
            {
                UserId = userId,
                QuestionId = questionId,
                StoredFileName = storedFileName,
                FileSizeBytes = audio.Length,
                DurationSeconds = duration,
                MimeType = mimeType,
                ModerationStatus = Models.ModerationStatus.AutoApproved,
                ContentHash = contentHash,
                CreatedAt = DateTime.UtcNow,
            };

            _context.VoiceAnswers.Add(answer);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Voice answer {Id} uploaded for user {UserId} question {QuestionId} ({Size}B, {Duration}s)",
                answer.Id, userId, questionId, audio.Length, duration);

            return Created(answer.Url, new
            {
                id = answer.Id,
                questionId = answer.QuestionId,
                url = answer.Url,
                durationSeconds = answer.DurationSeconds,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading voice answer");
            return StatusCode(500, "An error occurred while uploading voice answer");
        }
    }

    /// <summary>
    /// Get the current user's voice answers with question text.
    /// GET /api/voice-answers/my
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAnswers()
    {
        var userId = GetCurrentUserId();

        var answers = await _context.VoiceAnswers
            .Include(a => a.Question)
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderBy(a => a.Question!.QuestionOrder)
            .Select(a => new
            {
                a.Id,
                a.QuestionId,
                questionText = a.Question!.QuestionText,
                questionTextEn = a.Question.QuestionTextEn,
                a.DurationSeconds,
                audioUrl = $"/api/voice-answers/{a.Id}/audio",
                a.CreatedAt,
            })
            .ToListAsync();

        return Ok(new { answers, count = answers.Count });
    }

    /// <summary>
    /// Get another user's voice answers (for discovery cards / blind dating).
    /// Rejected answers are filtered out.
    /// GET /api/voice-answers/user/{userId}
    /// </summary>
    [HttpGet("user/{targetUserId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAnswers(int targetUserId)
    {
        var answers = await _context.VoiceAnswers
            .Include(a => a.Question)
            .Where(a => a.UserId == targetUserId && !a.IsDeleted
                     && a.ModerationStatus != Models.ModerationStatus.Rejected)
            .OrderBy(a => a.Question!.QuestionOrder)
            .Select(a => new
            {
                a.Id,
                a.QuestionId,
                questionText = a.Question!.QuestionText,
                questionTextEn = a.Question.QuestionTextEn,
                a.DurationSeconds,
                audioUrl = $"/api/voice-answers/{a.Id}/audio",
            })
            .ToListAsync();

        return Ok(new { answers, count = answers.Count });
    }

    /// <summary>
    /// Stream audio for a specific voice answer.
    /// GET /api/voice-answers/{answerId}/audio
    /// </summary>
    [HttpGet("{answerId:int}/audio")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAudio(int answerId)
    {
        var answer = await _context.VoiceAnswers
            .Where(a => a.Id == answerId && !a.IsDeleted
                     && a.ModerationStatus != Models.ModerationStatus.Rejected)
            .FirstOrDefaultAsync();

        if (answer == null)
            return NotFound("Voice answer not found");

        var filePath = Path.Combine("uploads", "voice-answers", answer.UserId.ToString(), answer.StoredFileName);
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("Voice answer file missing: {Path}", filePath);
            return NotFound("Audio file not found");
        }

        Response.Headers.CacheControl = "private, max-age=3600";
        return PhysicalFile(Path.GetFullPath(filePath), answer.MimeType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Delete a specific voice answer.
    /// DELETE /api/voice-answers/{answerId}
    /// </summary>
    [HttpDelete("{answerId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int answerId)
    {
        var userId = GetCurrentUserId();

        var answer = await _context.VoiceAnswers
            .Where(a => a.Id == answerId && a.UserId == userId && !a.IsDeleted)
            .FirstOrDefaultAsync();

        if (answer == null)
            return NotFound("Voice answer not found");

        answer.IsDeleted = true;
        answer.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Voice answer {Id} deleted for user {UserId}", answerId, userId);
        return Ok(new { message = "Voice answer deleted" });
    }

    // ────────────── Helpers ──────────────

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("Unable to determine user identity");

        if (int.TryParse(userIdClaim, out var userId))
            return userId;

        return Math.Abs(userIdClaim.GetHashCode());
    }
}
