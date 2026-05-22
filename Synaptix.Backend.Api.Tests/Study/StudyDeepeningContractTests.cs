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

public sealed class StudyDeepeningContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public StudyDeepeningContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CustomStudySet_CanBeCreated_Updated_AndListed()
    {
        var category = $"Custom-{Guid.NewGuid():N}";
        var firstQuestionId = await SeedQuestionAsync("Custom 1", category);
        var secondQuestionId = await SeedQuestionAsync("Custom 2", category);
        var client = await CreateAuthenticatedClientAsync("custom-study-set");

        var createResponse = await client.PostAsJsonAsync("/study-sets", new CreateStudySetRequest(
            Title: "My Saved Set",
            Description: "A saved custom set",
            QuestionIds: new[] { firstQuestionId, secondQuestionId }));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<StudySetDetailDto>(TestJson.Default);
        created.Should().NotBeNull();
        created!.Kind.Should().Be(StudySetKinds.Custom);
        created.Title.Should().Be("My Saved Set");
        created.Questions.Should().HaveCount(2);

        var updateResponse = await client.PatchAsJsonAsync($"/study-sets/{created.Id}", new UpdateStudySetRequest(
            Title: "My Updated Set",
            Description: "Updated",
            QuestionIds: new[] { secondQuestionId }));
        updateResponse.EnsureSuccessStatusCode();

        var updated = await updateResponse.Content.ReadFromJsonAsync<StudySetDetailDto>(TestJson.Default);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("My Updated Set");
        updated.Questions.Should().ContainSingle(x => x.Id == secondQuestionId);

        var list = await client.GetFromJsonAsync<StudySetsResponseDto>("/study-sets", TestJson.Default);
        list.Should().NotBeNull();
        list!.Items.Should().Contain(x => x.Kind == StudySetKinds.Custom && x.Id == created.Id);
    }

    [Fact]
    public async Task FlashcardProgress_PersistsInteractionState_AndCreatesDueReviewRecommendation()
    {
        var category = $"Flash-{Guid.NewGuid():N}";
        await SeedQuestionAsync("Flash question", category);
        var client = await CreateAuthenticatedClientAsync("flashcard-deep");

        var createResponse = await client.PostAsJsonAsync("/study-sessions", new CreateStudySessionRequest(
            StudySetId: $"category:{category}",
            Mode: StudySessionModes.Flashcard,
            Count: 10));
        createResponse.EnsureSuccessStatusCode();

        var session = await createResponse.Content.ReadFromJsonAsync<StudySessionDto>();
        session.Should().NotBeNull();
        var questionId = session!.QuestionIds[0];

        var progressResponse = await client.PostAsJsonAsync(
            $"/study-sessions/{session.Id}/progress",
            new UpdateStudySessionProgressRequest(
                QuestionId: questionId,
                SelectedOptionId: null,
                CurrentQuestionIndex: 0,
                FlashcardAction: "Again",
                Confidence: 1,
                AnswerRevealed: true,
                IsCompleted: false));
        progressResponse.EnsureSuccessStatusCode();

        var summary = await client.GetFromJsonAsync<StudySessionDto>($"/study-sessions/{session.Id}/summary", TestJson.Default);
        summary.Should().NotBeNull();
        summary!.Interactions.Should().ContainSingle(x =>
            x.QuestionId == questionId
            && x.FlashcardAction == "Again"
            && x.Confidence == 1
            && x.AnswerRevealed);

        var recommended = await client.GetFromJsonAsync<RecommendedStudySetsResponseDto>("/study-sets/recommended", TestJson.Default);
        recommended.Should().NotBeNull();
        recommended!.Items.Should().Contain(x => x.Kind == StudySetKinds.DueReview && x.Id == "due-review");

        var dueReview = await client.GetFromJsonAsync<StudySetDetailDto>("/study-sets/due-review?count=10", TestJson.Default);
        dueReview.Should().NotBeNull();
        dueReview!.Kind.Should().Be(StudySetKinds.DueReview);
        dueReview.Questions.Should().Contain(x => x.Id == questionId);
    }

    [Fact]
    public async Task StudySetMutation_RequiresAuthentication()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/study-sets", new CreateStudySetRequest(
            Title: "Denied",
            Description: null,
            QuestionIds: Array.Empty<Guid>()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
