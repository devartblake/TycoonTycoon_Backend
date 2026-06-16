using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Study;

public sealed class StudyFavoritesAndModesContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public StudyFavoritesAndModesContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FavoritesEndpoints_RequireAuthentication()
    {
        var questionId = await SeedQuestionAsync($"Question {Guid.NewGuid():N}", $"Science-{Guid.NewGuid():N}");
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/study-sets/favorites/{questionId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FavoritedQuestions_AppearInFavoritesStudySet_AndDiscovery()
    {
        var category = $"Science-{Guid.NewGuid():N}";
        var questionId = await SeedQuestionAsync("Favorite me", category);
        var client = await CreateAuthenticatedClientAsync("study-favorites");

        var favoriteResponse = await client.PostAsync($"/api/v1/study-sets/favorites/{questionId}", null);
        favoriteResponse.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<StudySetsResponseDto>("/api/v1/study-sets", TestJson.Default);
        list.Should().NotBeNull();
        list!.Items.Should().Contain(x => x.Kind == StudySetKinds.Favorites && x.Id == "favorites");

        var favorites = await client.GetFromJsonAsync<StudySetDetailDto>("/api/v1/study-sets/favorites?count=10", TestJson.Default);
        favorites.Should().NotBeNull();
        favorites!.Kind.Should().Be(StudySetKinds.Favorites);
        favorites.Questions.Should().ContainSingle(x => x.Id == questionId);
        favorites.Questions.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.CorrectOptionId));
    }

    [Fact]
    public async Task StudySession_PersistsRequestedMode()
    {
        var category = $"History-{Guid.NewGuid():N}";
        await SeedQuestionAsync("Mode question", category);
        var client = await CreateAuthenticatedClientAsync("study-mode");

        var createResponse = await client.PostAsJsonAsync("/api/v1/study-sessions", new CreateStudySessionRequest(
            StudySetId: $"category:{category}",
            Mode: StudySessionModes.Flashcard,
            Count: 10));
        createResponse.EnsureSuccessStatusCode();

        var session = await createResponse.Content.ReadFromJsonAsync<StudySessionDto>();
        session.Should().NotBeNull();
        session!.Mode.Should().Be(StudySessionModes.Flashcard);

        var summary = await client.GetFromJsonAsync<StudySessionDto>($"/api/v1/study-sessions/{session.Id}/summary");
        summary.Should().NotBeNull();
        summary!.Mode.Should().Be(StudySessionModes.Flashcard);
    }

    [Fact]
    public async Task DeleteFavorite_RemovesQuestionFromFavoritesStudySet()
    {
        var category = $"History-{Guid.NewGuid():N}";
        var questionId = await SeedQuestionAsync("Remove me", category);
        var client = await CreateAuthenticatedClientAsync("study-unfavorite");

        var addResponse = await client.PostAsync($"/api/v1/study-sets/favorites/{questionId}", null);
        addResponse.EnsureSuccessStatusCode();

        var removeResponse = await client.DeleteAsync($"/api/v1/study-sets/favorites/{questionId}");
        removeResponse.EnsureSuccessStatusCode();

        var detailResponse = await client.GetAsync("/api/v1/study-sets/favorites?count=10");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedQuestionAsync(string text, string category)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question(
            text: text,
            category: category,
            difficulty: QuestionDifficulty.Easy,
            correctOptionId: "A",
            mediaKey: null);
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", "Correct"),
            new QuestionOption(question.Id, "B", "Incorrect")
        });
        question.SetStatus("Approved");

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question.Id;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string prefix)
    {
        var client = _factory.CreateClient();

        var signupResponse = await client.PostAsJsonAsync("/api/v1/auth/signup", new SignupRequest(
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
