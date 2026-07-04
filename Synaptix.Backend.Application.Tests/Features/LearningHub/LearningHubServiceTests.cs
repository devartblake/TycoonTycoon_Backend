using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Synaptix.Backend.Application.Features.LearningHub;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Domain.Repositories;
using Synaptix.Shared.Contracts.Dtos.LearningHub;

namespace Synaptix.Backend.Application.Tests.Features.LearningHub;

/// <summary>
/// Unit tests for LearningHubService.
/// Tests question-lesson linking, click tracking, and recommendations.
/// </summary>
public class LearningHubServiceTests
{
    private readonly FakeQuestionLessonMappingRepository _repository;
    private readonly FakeLogger<LearningHubService> _logger;
    private readonly LearningHubService _service;

    public LearningHubServiceTests()
    {
        _repository = new FakeQuestionLessonMappingRepository();
        _logger = new FakeLogger<LearningHubService>();

        _service = new LearningHubService(_repository, _logger);
    }

    #region GetLessonsForQuestion Tests

    [Fact]
    public async Task GetLessonsForQuestion_WithValidQuestion_ReturnsMappedLessons()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var expectedLessonIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _repository.SetupLessons(questionId, expectedLessonIds);

        // Act
        var result = await _service.GetLessonsForQuestionAsync(questionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedLessonIds, result);
    }

    [Fact]
    public async Task GetLessonsForQuestion_WithNoMappings_ReturnsEmpty()
    {
        // Arrange
        var questionId = Guid.NewGuid();

        // Act
        var result = await _service.GetLessonsForQuestionAsync(questionId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region TrackLearnMoreClick Tests

    [Fact]
    public async Task TrackLearnMoreClick_WithValidInput_ReturnsTrue()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var context = "quiz-review";

        // Act
        var result = await _service.TrackLearnMoreClickAsync(playerId, questionId, context);

        // Assert
        Assert.True(result);
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

    #endregion
}

internal class FakeQuestionLessonMappingRepository : IQuestionLessonMappingRepository
{
    private readonly Dictionary<Guid, IEnumerable<Guid>> _mappings = new();

    public void SetupLessons(Guid questionId, IEnumerable<Guid> lessonIds)
    {
        _mappings[questionId] = lessonIds;
    }

    public Task<IEnumerable<Guid>> GetLessonsByQuestionAsync(Guid questionId, CancellationToken ct = default)
    {
        return Task.FromResult(_mappings.TryGetValue(questionId, out var result) ? result : Enumerable.Empty<Guid>());
    }

    public Task<IEnumerable<Guid>> GetQuestionsByLessonAsync(Guid lessonId, CancellationToken ct = default)
    {
        return Task.FromResult(Enumerable.Empty<Guid>());
    }

    public Task<QuestionLessonMapping> CreateMappingAsync(QuestionLessonMapping mapping, CancellationToken ct = default) => Task.FromResult(mapping);
    public Task<bool> DeleteMappingAsync(Guid questionId, Guid lessonId, CancellationToken ct = default) => Task.FromResult(true);
    public Task<bool> MappingExistsAsync(Guid questionId, Guid lessonId, CancellationToken ct = default) => Task.FromResult(false);
    public Task<int> BulkInsertMappingsAsync(IEnumerable<QuestionLessonMapping> mappings, CancellationToken ct = default) => Task.FromResult(mappings.Count());
}

internal class FakeLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
