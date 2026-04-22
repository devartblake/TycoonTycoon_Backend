using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.LearningModules;

public sealed class LearningModulesEndpointsContractTests : IClassFixture<TycoonApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public LearningModulesEndpointsContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task ListModules_ReturnsPublishedModulesOrderedAndDefaultsIsCompletedFalse()
    {
        var first = await SeedPublishedModuleAsync("Alpha Science", "Science", QuestionDifficulty.Easy);
        var second = await SeedPublishedModuleAsync("Beta History", "History", QuestionDifficulty.Hard);
        await SeedUnpublishedModuleAsync("Hidden Module", "Science", QuestionDifficulty.Medium);

        var response = await _http.GetFromJsonAsync<List<LearningModuleListItemDto>>("/modules", JsonOptions);
        var relevant = response!.Where(x => x.Id == first.Id || x.Id == second.Id).ToList();

        relevant.Should().HaveCount(2);
        relevant[0].Id.Should().Be(first.Id);
        relevant.All(x => x.IsCompleted == false).Should().BeTrue();
    }

    [Fact]
    public async Task ListModules_WithPlayerId_MarksCompletedModules()
    {
        var playerId = Guid.NewGuid();
        var completed = await SeedPublishedModuleAsync("Completed Module", "Science", QuestionDifficulty.Easy);
        var incomplete = await SeedPublishedModuleAsync("Incomplete Module", "Science", QuestionDifficulty.Medium);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.ModuleCompletions.Add(new ModuleCompletion(playerId, completed.Id));
            await db.SaveChangesAsync();
        }

        var response = await _http.GetFromJsonAsync<List<LearningModuleListItemDto>>($"/modules?playerId={playerId}", JsonOptions);

        response.Should().NotBeNull();
        var relevant = response!.Where(x => x.Id == completed.Id || x.Id == incomplete.Id).ToList();
        relevant.Should().HaveCount(2);
        relevant.Single(x => x.Id == completed.Id).IsCompleted.Should().BeTrue();
        relevant.Single(x => x.Id == incomplete.Id).IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetModule_ReturnsOverview_ForPublishedModule()
    {
        var module = await SeedPublishedModuleAsync("Science Basics", "Science", QuestionDifficulty.Easy);

        var response = await _http.GetFromJsonAsync<LearningModuleDetailDto>($"/modules/{module.Id}", JsonOptions);

        response.Should().NotBeNull();
        response!.Id.Should().Be(module.Id);
        response.Title.Should().Be("Science Basics");
        response.RewardXp.Should().Be(500);
        response.RewardCoins.Should().Be(100);
    }

    [Fact]
    public async Task GetRecommendedModules_ExcludesCompletedModules_WhenPlayerIdProvided()
    {
        var playerId = Guid.NewGuid();
        var completed = await SeedPublishedModuleAsync("Completed Recommendation", "Science", QuestionDifficulty.Easy);
        var next = await SeedPublishedModuleAsync("Next Recommendation", "Science", QuestionDifficulty.Medium);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.ModuleCompletions.Add(new ModuleCompletion(playerId, completed.Id));
            await db.SaveChangesAsync();
        }

        var response = await _http.GetFromJsonAsync<RecommendedLearningModulesResponseDto>(
            $"/modules/recommended?playerId={playerId}&count=10", JsonOptions);

        response.Should().NotBeNull();
        response!.Items.Should().Contain(x => x.Id == next.Id);
        response.Items.Should().NotContain(x => x.Id == completed.Id);
    }

    [Fact]
    public async Task GetModuleProgress_ReturnsPublishedCatalogSummary_ForPlayer()
    {
        var playerId = Guid.NewGuid();
        var completed = await SeedPublishedModuleAsync("Completed Progress", "Science", QuestionDifficulty.Easy);
        var incomplete = await SeedPublishedModuleAsync("Incomplete Progress", "History", QuestionDifficulty.Hard);
        await SeedUnpublishedModuleAsync("Hidden Progress", "Science", QuestionDifficulty.Medium);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.ModuleCompletions.Add(new ModuleCompletion(playerId, completed.Id));
            await db.SaveChangesAsync();
        }

        var response = await _http.GetFromJsonAsync<LearningModuleProgressDto>(
            $"/modules/progress/{playerId}", JsonOptions);

        response.Should().NotBeNull();
        response!.PlayerId.Should().Be(playerId);
        response.CompletedModules.Should().BeGreaterThanOrEqualTo(1);
        response.CompletedModuleIds.Should().Contain(completed.Id);
        response.CompletedModuleIds.Should().NotContain(incomplete.Id);
        response.TotalPublishedModules.Should().BeGreaterThanOrEqualTo(2);
        response.RemainingModules.Should().Be(response.TotalPublishedModules - response.CompletedModules);
        response.CompletionRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetModule_ReturnsNotFound_ForUnpublishedModule()
    {
        var module = await SeedUnpublishedModuleAsync("Hidden", "Science", QuestionDifficulty.Easy);

        var response = await _http.GetAsync($"/modules/{module.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLessons_ReturnsOrderedLessonsWithCorrectOptionId()
    {
        var module = await SeedPublishedModuleAsync("Lesson Module", "Science", QuestionDifficulty.Easy);
        var questionA = await SeedQuestionAsync("Question A", "Biology", QuestionDifficulty.Easy, "B", "Nucleus", "Mitochondria");
        var questionB = await SeedQuestionAsync("Question B", "Biology", QuestionDifficulty.Easy, "A", "Cell", "Atom");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.ModuleLessons.Add(new ModuleLesson(module.Id, questionA.Id, 2, "Explanation B"));
            db.ModuleLessons.Add(new ModuleLesson(module.Id, questionB.Id, 1, "Explanation A"));
            await db.SaveChangesAsync();
        }

        var response = await _http.GetFromJsonAsync<List<ModuleLessonDto>>($"/modules/{module.Id}/lessons");

        response.Should().NotBeNull();
        response!.Should().HaveCount(2);
        response[0].Order.Should().Be(1);
        response[0].CorrectOptionId.Should().Be("A");
        response[1].Order.Should().Be(2);
        response[1].CorrectOptionId.Should().Be("B");
    }

    [Fact]
    public async Task CompleteModule_IsIdempotent_AndReturnsBalances()
    {
        var playerId = Guid.NewGuid();
        var module = await SeedPublishedModuleAsync("Reward Module", "Science", QuestionDifficulty.Easy);

        var first = await _http.PostAsync($"/modules/{module.Id}/complete?playerId={playerId}", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<CompleteModuleResultDto>();

        firstBody.Should().NotBeNull();
        firstBody!.Status.Should().Be("Completed");
        firstBody.RewardXp.Should().Be(500);
        firstBody.RewardCoins.Should().Be(100);
        firstBody.BalanceXp.Should().Be(500);
        firstBody.BalanceCoins.Should().Be(100);

        var second = await _http.PostAsync($"/modules/{module.Id}/complete?playerId={playerId}", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<CompleteModuleResultDto>();

        secondBody.Should().NotBeNull();
        secondBody!.Status.Should().Be("AlreadyCompleted");
        secondBody.BalanceXp.Should().Be(500);
        secondBody.BalanceCoins.Should().Be(100);
    }

    [Fact]
    public async Task CompleteModule_ReturnsNotFound_ForMissingModule()
    {
        var response = await _http.PostAsync($"/modules/{Guid.NewGuid()}/complete?playerId={Guid.NewGuid()}", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<LearningModule> SeedPublishedModuleAsync(string title, string category, QuestionDifficulty difficulty)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var module = new LearningModule(title, $"{title} description", category, difficulty, rewardXp: 500, rewardCoins: 100);
        module.Publish();
        db.LearningModules.Add(module);
        await db.SaveChangesAsync();
        return module;
    }

    private async Task<LearningModule> SeedUnpublishedModuleAsync(string title, string category, QuestionDifficulty difficulty)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var module = new LearningModule(title, $"{title} description", category, difficulty, rewardXp: 500, rewardCoins: 100);
        db.LearningModules.Add(module);
        await db.SaveChangesAsync();
        return module;
    }

    private async Task<Question> SeedQuestionAsync(string text, string category, QuestionDifficulty difficulty, string correctOptionId, string optionA, string optionB)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question(text, category, difficulty, correctOptionId, mediaKey: null);
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", optionA),
            new QuestionOption(question.Id, "B", optionB)
        });
        question.SetStatus("Approved");

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question;
    }
}
