using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using PhotoService.Controllers;
using PhotoService.Data;
using PhotoService.Models;
using System.Security.Claims;
using Moq;

namespace PhotoService.Tests.Controllers;

/// <summary>
/// Unit tests for VoiceMessagesController.
/// Uses InMemoryDatabase and fake ClaimsPrincipal for auth.
/// </summary>
public class VoiceMessagesControllerTests : IDisposable
{
    private readonly PhotoContext _context;
    private readonly Mock<ILogger<VoiceMessagesController>> _mockLogger;
    private readonly VoiceMessagesController _controller;

    public VoiceMessagesControllerTests()
    {
        var options = new DbContextOptionsBuilder<PhotoContext>()
            .UseInMemoryDatabase(databaseName: "TestVoiceMsgDb_" + Guid.NewGuid())
            .Options;
        _context = new PhotoContext(options);
        _mockLogger = new Mock<ILogger<VoiceMessagesController>>();

        _controller = new VoiceMessagesController(_context, _mockLogger.Object);
        SetupUser("user-abc-123");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SetupUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private static IFormFile CreateMockAudioFile(string name, string contentType, int sizeBytes)
    {
        var content = new byte[sizeBytes];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, sizeBytes, "audio", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private void SetupFormWithDuration(double duration)
    {
        var formCollection = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "duration", duration.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            });
        _controller.ControllerContext.HttpContext.Request.ContentType = "multipart/form-data";
        _controller.ControllerContext.HttpContext.Request.Form = formCollection;
    }

    private async Task<VoiceMessage> SeedVoiceMessageAsync(string userId = "user-abc-123")
    {
        var vm = new VoiceMessage
        {
            SenderUserId = userId,
            StoredFileName = $"{userId}_test_{Guid.NewGuid():N}.m4a",
            FileSizeBytes = 50000,
            DurationSeconds = 10,
            MimeType = "audio/mp4",
            ModerationStatus = "AUTO_APPROVED",
            ContentHash = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
        };
        _context.VoiceMessages.Add(vm);
        await _context.SaveChangesAsync();
        return vm;
    }

    // ──────────────── Upload Tests ────────────────

    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest()
    {
        var result = await _controller.Upload(null!);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("No audio file", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Upload_FileTooLarge_ReturnsBadRequest()
    {
        SetupFormWithDuration(5.0);
        var file = CreateMockAudioFile("big.m4a", "audio/mp4", 3 * 1024 * 1024); // 3MB > 2MB limit
        var result = await _controller.Upload(file);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("exceeds", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Upload_InvalidMimeType_ReturnsBadRequest()
    {
        SetupFormWithDuration(5.0);
        var file = CreateMockAudioFile("bad.txt", "text/plain", 1000);
        var result = await _controller.Upload(file);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid audio format", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Upload_TooShortDuration_ReturnsBadRequest()
    {
        SetupFormWithDuration(0.5); // < 1s minimum
        var file = CreateMockAudioFile("short.m4a", "audio/mp4", 1000);
        var result = await _controller.Upload(file);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Duration must be between", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Upload_TooLongDuration_ReturnsBadRequest()
    {
        SetupFormWithDuration(65.0); // > 60s maximum
        var file = CreateMockAudioFile("long.m4a", "audio/mp4", 1000);
        var result = await _controller.Upload(file);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Duration must be between", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task Upload_ValidFile_Returns201AndSavesToDb()
    {
        SetupFormWithDuration(5.5);
        var file = CreateMockAudioFile("voice.m4a", "audio/mp4", 50000);
        var result = await _controller.Upload(file);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Contains("/api/voice-messages/", createdResult.Location);

        // Verify DB entry
        var saved = await _context.VoiceMessages.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("user-abc-123", saved.SenderUserId);
        Assert.Equal(5.5, saved.DurationSeconds);
        Assert.Equal(50000, saved.FileSizeBytes);
        Assert.Equal("AUTO_APPROVED", saved.ModerationStatus);
    }

    [Fact]
    public async Task Upload_MultipleMessages_AllSaved()
    {
        SetupFormWithDuration(3.0);
        var file1 = CreateMockAudioFile("v1.m4a", "audio/mp4", 10000);
        await _controller.Upload(file1);

        SetupFormWithDuration(7.0);
        var file2 = CreateMockAudioFile("v2.m4a", "audio/mp4", 20000);
        await _controller.Upload(file2);

        // Unlike VoicePrompts, both should exist (no soft-delete of previous)
        var count = await _context.VoiceMessages.CountAsync();
        Assert.Equal(2, count);
    }

    // ──────────────── GetAudio Tests ────────────────

    [Fact]
    public async Task GetAudio_NotFound_Returns404()
    {
        var result = await _controller.GetAudio(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAudio_DeletedMessage_Returns404()
    {
        var vm = await SeedVoiceMessageAsync();
        vm.IsDeleted = true;
        await _context.SaveChangesAsync();

        var result = await _controller.GetAudio(vm.Id);
        Assert.IsType<NotFoundResult>(result);
    }
}
