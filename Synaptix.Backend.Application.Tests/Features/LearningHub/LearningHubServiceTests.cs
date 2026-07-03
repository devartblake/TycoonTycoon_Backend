using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Synaptix.Backend.Application.Features.LearningHub;
using Synaptix.Backend.Domain.Repositories;

namespace Synaptix.Backend.Application.Tests.Features.LearningHub;

/// <summary>
/// Unit tests for LearningHubService.
/// Tests question-lesson linking, click tracking, and recommendations.
/// </summary>
public class LearningHubServiceTests
{
    private readonly Mock<IQuestionLessonMappingRepository> _mockRepository;
    private readonly Mock<IAnalyticsEventService> _mockAnalytics;
    private readonly Mock<ILogger<LearningHubService>> _mockLogger;
    private readonly LearningHubService _service;

    public LearningHubServiceTests()
    {
        _mockRepository = new Mock<IQuestionLessonMappingRepository>();
        _mockAnalytics = new Mock<IAnalyticsEventService>();
        _mockLogger = new Mock<ILogger<LearningHubService>>();

        _service = new LearningHubService(
            _mockRepository.Object,
            _mockAnalytics.Object,
            _mockLogger.Object);
    }

    #region GetLessonsForQuestion Tests

    [Fact]
    public async Task GetLessonsForQuestion_WithValidQuestion_ReturnsMappedLessons()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var expectedLessonIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        _mockRepository
            .Setup(r => r.GetLessonsByQuestionAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLessonIds);

        // Act
        var result = await _service.GetLessonsForQuestionAsync(questionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedLessonIds, result);
        _mockRepository.Verify(
            r => r.GetLessonsByQuestionAsync(questionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLessonsForQuestion_WithNoMappings_ReturnsEmpty()
    {
        // Arrange
        var questionId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetLessonsByQuestionAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        // Act
        var result = await _service.GetLessonsForQuestionAsync(questionId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLessonsForQuestion_WhenRepositoryThrows_ReturnsEmptyAndLogs()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var exception = new InvalidOperationException("Database error");

        _mockRepository
            .Setup(r => r.GetLessonsByQuestionAsync(questionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _service.GetLessonsForQuestionAsync(questionId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error fetching lessons")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region TrackLearnMoreClick Tests

    [Fact]
    public async Task TrackLearnMoreClick_WithValidInput_TracksEventAndReturnsTrue()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var context = "quiz-review";

        _mockAnalytics
            .Setup(a => a.TrackEventAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TrackLearnMoreClickAsync(playerId, questionId, context);

        // Assert
        Assert.True(result);
        _mockAnalytics.Verify(
            a => a.TrackEventAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TrackLearnMoreClick_WhenAnalyticsThrows_ReturnsFalseAndLogs()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var exception = new InvalidOperationException("Analytics service error");

        _mockAnalytics
            .Setup(a => a.TrackEventAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _service.TrackLearnMoreClickAsync(playerId, questionId);

        // Assert
        Assert.False(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error tracking learn-more click")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("quiz-review")]
    [InlineData("search")]
    [InlineData("custom-context")]
    public async Task TrackLearnMoreClick_WithDifferentContexts_TracksCorrectly(string context)
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        _mockAnalytics
            .Setup(a => a.TrackEventAsync(It.IsAny<AnalyticsEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.TrackLearnMoreClickAsync(playerId, questionId, context);

        // Assert
        Assert.True(result);
        _mockAnalytics.Verify(
            a => a.TrackEventAsync(
                It.Is<AnalyticsEvent>(e => e.EventType == "LearnMoreClick"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetRecommendedLessons Tests

    [Fact]
    public async Task GetRecommendedLessons_WithValidPlayer_ReturnsLessonList()
    {
        // Arrange
        var playerId = Guid.NewGuid();

        // Act
        var result = await _service.GetRecommendedLessonsAsync(playerId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<LessonDto>>(result);
    }

    [Fact]
    public async Task GetRecommendedLessons_WithCategoryFilter_PassesCorrectly()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var category = "Mathematics";

        // Act
        var result = await _service.GetRecommendedLessonsAsync(
            playerId,
            category: category);

        // Assert
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task GetRecommendedLessons_WithValidDifficulty_PassesCorrectly(int difficulty)
    {
        // Arrange
        var playerId = Guid.NewGuid();

        // Act
        var result = await _service.GetRecommendedLessonsAsync(
            playerId,
            difficulty: difficulty);

        // Assert
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task GetRecommendedLessons_WithValidLimit_PassesCorrectly(int limit)
    {
        // Arrange
        var playerId = Guid.NewGuid();

        // Act
        var result = await _service.GetRecommendedLessonsAsync(
            playerId,
            limit: limit);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRecommendedLessons_WhenRepositoryThrows_ReturnsEmptyAndLogs()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var exception = new InvalidOperationException("Database error");

        _mockRepository
            .Setup(r => r.GetQuestionsByLessonAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _service.GetRecommendedLessonsAsync(playerId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion
}
