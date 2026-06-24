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
/// Unit tests for VoiceAnswersController.
/// Uses InMemoryDatabase and fake ClaimsPrincipal for auth.
/// </summary>
public class VoiceAnswersControllerTests : IDisposable
{
    private readonly PhotoContext _context;
    private readonly Mock<ILogger<VoiceAnswersController>> _mockLogger;
    private readonly VoiceAnswersController _controller;

    public VoiceAnswersControllerTests()
    {
        var options = new DbContextOptionsBuilder<PhotoContext>()
            .UseInMemoryDatabase(databaseName: "TestVoiceAnswerDb_" + Guid.NewGuid())
            .Options;
        _context = new PhotoContext(options);
        _mockLogger = new Mock<ILogger<VoiceAnswersController>>();
        _controller = new VoiceAnswersController(_context, _mockLogger.Object);
        SetupUser("1");
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

    private async Task SeedQuestionsAsync()
    {
        _context.VoiceQuestions.AddRange(
            new VoiceQuestion { Id = 100, QuestionText = "Fråga 1", QuestionTextEn = "Question 1", QuestionOrder = 1, FlavorId = "voice", IsActive = true, CreatedAt = DateTime.UtcNow },
            new VoiceQuestion { Id = 101, QuestionText = "Fråga 2", QuestionTextEn = "Question 2", QuestionOrder = 2, FlavorId = "voice", IsActive = true, CreatedAt = DateTime.UtcNow },
            new VoiceQuestion { Id = 102, QuestionText = "Fråga 3", QuestionTextEn = "Question 3", QuestionOrder = 3, FlavorId = "voice", IsActive = true, CreatedAt = DateTime.UtcNow },
            new VoiceQuestion { Id = 103, QuestionText = "Inaktiv", QuestionTextEn = "Inactive", QuestionOrder = 4, FlavorId = "voice", IsActive = false, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();
    }

    private async Task<VoiceAnswer> SeedAnswerAsync(int userId, int questionId, string status = "AUTO_APPROVED")
    {
        var answer = new VoiceAnswer
        {
            UserId = userId,
            QuestionId = questionId,
            StoredFileName = $"{userId}_{questionId}_test.m4a",
            FileSizeBytes = 50000,
            DurationSeconds = 10,
            MimeType = "audio/mp4",
            ModerationStatus = status,
            CreatedAt = DateTime.UtcNow,
        };
        _context.VoiceAnswers.Add(answer);
        await _context.SaveChangesAsync();
        return answer;
    }

    [Fact]
    public async Task GetQuestions_ReturnsActiveQuestions()
    {
        await SeedQuestionsAsync();

        var result = await _controller.GetQuestions("voice");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        // Should return 3 active questions (not the inactive one)
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("Question 1", json);
        Assert.Contains("Question 2", json);
        Assert.Contains("Question 3", json);
        Assert.DoesNotContain("Inactive", json);
    }

    [Fact]
    public async Task GetMyAnswers_ReturnsEmptyWhenNoAnswers()
    {
        var result = await _controller.GetMyAnswers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"count\":0", json);
    }

    [Fact]
    public async Task GetMyAnswers_ReturnsUserAnswers()
    {
        await SeedQuestionsAsync();
        await SeedAnswerAsync(1, 100);
        await SeedAnswerAsync(1, 101);

        var result = await _controller.GetMyAnswers();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"count\":2", json);
    }

    [Fact]
    public async Task GetUserAnswers_FiltersRejected()
    {
        await SeedQuestionsAsync();
        await SeedAnswerAsync(2, 100, "AUTO_APPROVED");
        await SeedAnswerAsync(2, 101, "REJECTED");

        var result = await _controller.GetUserAnswers(2);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("\"count\":1", json);
    }

    [Fact]
    public async Task Delete_SoftDeletesAnswer()
    {
        await SeedQuestionsAsync();
        var answer = await SeedAnswerAsync(1, 100);

        var result = await _controller.Delete(answer.Id);

        Assert.IsType<OkObjectResult>(result);

        var deleted = await _context.VoiceAnswers.FindAsync(answer.Id);
        Assert.True(deleted!.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
    }

    [Fact]
    public async Task Delete_Returns404ForOtherUsersAnswer()
    {
        await SeedQuestionsAsync();
        var answer = await SeedAnswerAsync(99, 100); // different user

        var result = await _controller.Delete(answer.Id);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
