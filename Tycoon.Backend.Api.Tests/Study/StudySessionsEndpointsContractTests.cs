using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Study;

public sealed class StudySessionsEndpointsContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public StudySessionsEndpointsContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateStudySession_RequiresAuthentication()
    {
        var category = $"Science-{Guid.NewGuid():N}";
        await SeedQuestionAsync("Science 1", category, QuestionDifficulty.Easy, "Approved");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/study-sessions", new CreateStudySessionRequest(
            StudySetId: $"category:{category}",
            Count: 10));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateStudySession_ReturnsDurableSnapshotForAuthenticatedUser()
    {
        var category = $"Science-{Guid.NewGuid():N}";
        await SeedQuestionAsync("Science 1", category, QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("Science 2", category, QuestionDifficulty.Medium, "Approved");
        var client = await CreateAuthenticatedClientAsync("study-create");

        var response = await client.PostAsJsonAsync("/study-sessions", new CreateStudySessionRequest(
            StudySetId: $"category:{category}",
            Count: 10));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<StudySessionDto>();
        payload.Should().NotBeNull();
        payload!.StudySetId.Should().Be($"category:{category}");
        payload.Category.Should().Be(category);
        payload.QuestionCount.Should().Be(2);
        payload.AnsweredCount.Should().Be(0);
        payload.CorrectCount.Should().Be(0);
        payload.QuestionIds.Should().HaveCount(2);
        payload.AnsweredQuestionIds.Should().BeEmpty();
        payload.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task ProgressAndSummary_UpdateCounts_AndRespectSessionOwnership()
    {
        var category = $"History-{Guid.NewGuid():N}";
        await SeedQuestionAsync("History 1", category, QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("History 2", category, QuestionDifficulty.Medium, "Approved");

        var ownerClient = await CreateAuthenticatedClientAsync("study-owner");
        var otherClient = await CreateAuthenticatedClientAsync("study-other");

        var createResponse = await ownerClient.PostAsJsonAsync("/study-sessions", new CreateStudySessionRequest(
            StudySetId: $"category:{category}",
            Count: 10));
        createResponse.EnsureSuccessStatusCode();
        var session = (await createResponse.Content.ReadFromJsonAsync<StudySessionDto>())!;

        var firstQuestionId = session.QuestionIds[0];

        var progressResponse = await ownerClient.PostAsJsonAsync(
            $"/study-sessions/{session.Id}/progress",
            new UpdateStudySessionProgressRequest(
                QuestionId: firstQuestionId,
                SelectedOptionId: "A",
                CurrentQuestionIndex: 1,
                IsCompleted: false));
        progressResponse.EnsureSuccessStatusCode();

        var progress = await progressResponse.Content.ReadFromJsonAsync<StudySessionDto>();
        progress.Should().NotBeNull();
        progress!.AnsweredCount.Should().Be(1);
        progress.CorrectCount.Should().Be(1);
        progress.CurrentQuestionIndex.Should().Be(1);
        progress.AnsweredQuestionIds.Should().Contain(firstQuestionId);

        var completeResponse = await ownerClient.PostAsJsonAsync(
            $"/study-sessions/{session.Id}/progress",
            new UpdateStudySessionProgressRequest(
                QuestionId: session.QuestionIds[1],
                SelectedOptionId: "B",
                CurrentQuestionIndex: 1,
                IsCompleted: true));
        completeResponse.EnsureSuccessStatusCode();

        var summaryResponse = await ownerClient.GetAsync($"/study-sessions/{session.Id}/summary");
        summaryResponse.EnsureSuccessStatusCode();

        var summary = await summaryResponse.Content.ReadFromJsonAsync<StudySessionDto>();
        summary.Should().NotBeNull();
        summary!.AnsweredCount.Should().Be(2);
        summary.CorrectCount.Should().Be(1);
        summary.IsCompleted.Should().BeTrue();
        summary.CompletedAtUtc.Should().NotBeNull();

        var forbiddenSummary = await otherClient.GetAsync($"/study-sessions/{session.Id}/summary");
        forbiddenSummary.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Progress_ReturnsValidationError_WhenQuestionDoesNotBelongToSession()
    {
        var scienceCategory = $"Science-{Guid.NewGuid():N}";
        var historyCategory = $"History-{Guid.NewGuid():N}";
        await SeedQuestionAsync("Science 1", scienceCategory, QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("History 1", historyCategory, QuestionDifficulty.Easy, "Approved");
        var client = await CreateAuthenticatedClientAsync("study-validation");

        var createResponse = await client.PostAsJsonAsync("/study-sessions", new CreateStudySessionRequest(
            StudySetId: $"category:{scienceCategory}",
            Count: 10));
        createResponse.EnsureSuccessStatusCode();
        var session = (await createResponse.Content.ReadFromJsonAsync<StudySessionDto>())!;

        var historyList = await client.GetFromJsonAsync<StudySetDetailDto>(
            $"/study-sets/category:{historyCategory}?count=10",
            TestJson.Default);
        historyList.Should().NotBeNull();
        var foreignQuestionId = historyList!.Questions[0].Id;

        var response = await client.PostAsJsonAsync(
            $"/study-sessions/{session.Id}/progress",
            new UpdateStudySessionProgressRequest(
                QuestionId: foreignQuestionId,
                SelectedOptionId: "A",
                CurrentQuestionIndex: 0,
                IsCompleted: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await response.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    private async Task SeedQuestionAsync(string text, string category, QuestionDifficulty difficulty, string status)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question(
            text: text,
            category: category,
            difficulty: difficulty,
            correctOptionId: "A",
            mediaKey: null);
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", "Correct"),
            new QuestionOption(question.Id, "B", "Incorrect")
        });
        question.SetStatus(status);

        db.Questions.Add(question);
        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string prefix)
    {
        var client = _factory.CreateClient();

        var signupResponse = await client.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: $"{prefix}-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"{prefix}_{Guid.NewGuid():N}"));
        signupResponse.EnsureSuccessStatusCode();

        var signup = await signupResponse.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup!.AccessToken);
        return client;
    }
}
