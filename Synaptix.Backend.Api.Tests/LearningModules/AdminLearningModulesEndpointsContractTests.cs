using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.LearningModules;

public sealed class AdminLearningModulesEndpointsContractTests : IClassFixture<SynaptixApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _http;

    public AdminLearningModulesEndpointsContractTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task AdminRoutes_Require_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var response = await noKey.GetAsync("/admin/modules");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await response.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminRoutes_Reject_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("wrong-key");

        var response = await wrongKey.GetAsync("/admin/modules");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await response.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Create_Update_Publish_Unpublish_And_List_Work_AsDocumented()
    {
        var createResponse = await _http.PostAsJsonAsync("/admin/modules", new CreateLearningModuleRequest(
            Title: "Science Basics",
            Description: "Foundational science questions.",
            Category: "Science",
            Difficulty: QuestionDifficulty.Easy,
            RewardXp: 500,
            RewardCoins: 100));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedIdResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();

        var moduleId = created.Id;

        var updateResponse = await _http.PatchAsJsonAsync($"/admin/modules/{moduleId}", new UpdateLearningModuleRequest(
            Title: "Science Intermediate",
            Description: "Updated science module.",
            Category: "Science",
            Difficulty: QuestionDifficulty.Medium,
            RewardXp: 750,
            RewardCoins: 150));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminLearningModuleListItemDto>(JsonOptions);
        updated.Should().NotBeNull();
        updated!.Id.Should().Be(moduleId);
        updated.Title.Should().Be("Science Intermediate");
        updated.Difficulty.Should().Be(QuestionDifficulty.Medium);
        updated.IsPublished.Should().BeFalse();
        updated.LessonCount.Should().Be(0);
        updated.RewardXp.Should().Be(750);
        updated.RewardCoins.Should().Be(150);

        var publishResponse = await _http.PatchAsync($"/admin/modules/{moduleId}/publish", content: null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await publishResponse.Content.ReadFromJsonAsync<PublishStateResponse>();
        published.Should().NotBeNull();
        published!.Id.Should().Be(moduleId);
        published.IsPublished.Should().BeTrue();

        var listResponse = await _http.GetFromJsonAsync<List<AdminLearningModuleListItemDto>>("/admin/modules?category=Science&isPublished=true", JsonOptions);
        listResponse.Should().NotBeNull();
        listResponse!.Should().ContainSingle(x => x.Id == moduleId);

        var unpublishResponse = await _http.PatchAsync($"/admin/modules/{moduleId}/unpublish", content: null);
        unpublishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unpublished = await unpublishResponse.Content.ReadFromJsonAsync<PublishStateResponse>();
        unpublished.Should().NotBeNull();
        unpublished!.Id.Should().Be(moduleId);
        unpublished.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task AddLesson_And_RemoveLesson_Work_ForExistingModuleAndQuestion()
    {
        var module = await SeedModuleAsync("Science Basics", "Science", QuestionDifficulty.Easy);
        var question = await SeedQuestionAsync("What is H2O?", "Science", QuestionDifficulty.Easy, "B");

        var addResponse = await _http.PostAsJsonAsync($"/admin/modules/{module.Id}/lessons", new AddModuleLessonRequest(
            QuestionId: question.Id,
            Order: 1,
            Explanation: "Water is made from hydrogen and oxygen."));

        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var added = await addResponse.Content.ReadFromJsonAsync<CreatedLessonResponse>();
        added.Should().NotBeNull();
        added!.LessonId.Should().NotBeEmpty();

        var publicLessons = await _factory.CreateClient().GetFromJsonAsync<List<ModuleLessonDto>>($"/api/v1/modules/{module.Id}/lessons");
        publicLessons.Should().NotBeNull();
        publicLessons!.Should().ContainSingle();
        publicLessons![0].QuestionId.Should().Be(question.Id);
        publicLessons![0].Explanation.Should().Be("Water is made from hydrogen and oxygen.");
        publicLessons![0].Order.Should().Be(1);

        var deleteResponse = await _http.DeleteAsync($"/admin/modules/{module.Id}/lessons/{added.LessonId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lessonsAfterDelete = await _factory.CreateClient().GetFromJsonAsync<List<ModuleLessonDto>>($"/api/v1/modules/{module.Id}/lessons");
        lessonsAfterDelete.Should().NotBeNull();
        lessonsAfterDelete!.Should().BeEmpty();
    }

    [Fact]
    public async Task AddLesson_ReturnsValidationError_ForDuplicateOrder()
    {
        var module = await SeedModuleAsync("Duplicate Order Module", "Science", QuestionDifficulty.Easy);
        var firstQuestion = await SeedQuestionAsync("Question One", "Science", QuestionDifficulty.Easy, "A");
        var secondQuestion = await SeedQuestionAsync("Question Two", "Science", QuestionDifficulty.Easy, "B");

        var firstResponse = await _http.PostAsJsonAsync($"/admin/modules/{module.Id}/lessons", new AddModuleLessonRequest(
            QuestionId: firstQuestion.Id,
            Order: 1,
            Explanation: null));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateResponse = await _http.PostAsJsonAsync($"/admin/modules/{module.Id}/lessons", new AddModuleLessonRequest(
            QuestionId: secondQuestion.Id,
            Order: 1,
            Explanation: "Duplicate order"));

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await duplicateResponse.HasErrorCodeAsync("VALIDATION_ERROR");
        await duplicateResponse.HasErrorMessageContainingAsync("order 1");
    }

    [Fact]
    public async Task AddLesson_ReturnsValidationError_ForMissingQuestion()
    {
        var module = await SeedModuleAsync("Missing Question Module", "Science", QuestionDifficulty.Easy);

        var response = await _http.PostAsJsonAsync($"/admin/modules/{module.Id}/lessons", new AddModuleLessonRequest(
            QuestionId: Guid.NewGuid(),
            Order: 1,
            Explanation: null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await response.HasErrorCodeAsync("VALIDATION_ERROR");
        await response.HasErrorMessageContainingAsync("Question not found");
    }

    [Fact]
    public async Task Update_Publish_And_RemoveLesson_ReturnNotFound_ForMissingModuleOrLesson()
    {
        var updateResponse = await _http.PatchAsJsonAsync($"/admin/modules/{Guid.NewGuid()}", new UpdateLearningModuleRequest(
            Title: "Missing",
            Description: "Missing",
            Category: "Science",
            Difficulty: QuestionDifficulty.Easy,
            RewardXp: 100,
            RewardCoins: 50));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await updateResponse.HasErrorCodeAsync("NOT_FOUND");

        var publishResponse = await _http.PatchAsync($"/admin/modules/{Guid.NewGuid()}/publish", content: null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await publishResponse.HasErrorCodeAsync("NOT_FOUND");

        var deleteResponse = await _http.DeleteAsync($"/admin/modules/{Guid.NewGuid()}/lessons/{Guid.NewGuid()}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await deleteResponse.HasErrorCodeAsync("NOT_FOUND");
    }

    private async Task<LearningModule> SeedModuleAsync(string title, string category, QuestionDifficulty difficulty)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var module = new LearningModule(title, $"{title} description", category, difficulty, rewardXp: 500, rewardCoins: 100);
        module.Publish();
        db.LearningModules.Add(module);
        await db.SaveChangesAsync();
        return module;
    }

    private async Task<Question> SeedQuestionAsync(string text, string category, QuestionDifficulty difficulty, string correctOptionId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question(text, category, difficulty, correctOptionId, mediaKey: null);
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", $"{text} option A"),
            new QuestionOption(question.Id, "B", $"{text} option B")
        });
        question.SetStatus("Approved");

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question;
    }

    private sealed record CreatedIdResponse(Guid Id);

    private sealed record CreatedLessonResponse(Guid LessonId);

    private sealed record PublishStateResponse(Guid Id, bool IsPublished);
}
