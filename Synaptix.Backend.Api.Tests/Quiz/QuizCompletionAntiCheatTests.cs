using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Quiz;

public sealed class QuizCompletionAntiCheatTests : IClassFixture<TycoonApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TycoonApiFactory _factory;

    public QuizCompletionAntiCheatTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Complete_rejects_legacy_client_reward_fields_without_answer_evidence()
    {
        using var client = _factory.CreateClient();
        var playerId = Guid.NewGuid();
        client.AuthenticateAsPlayer(_factory, playerId);

        var response = await client.PostAsJsonAsync("/api/v1/quiz/complete", new
        {
            playerId,
            eventId = Guid.NewGuid(),
            xpEarned = 999_999,
            coinsEarned = 999_999
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Complete_ignores_forged_rewards_and_grants_server_computed_rewards()
    {
        using var client = _factory.CreateClient();
        var playerId = Guid.NewGuid();
        client.AuthenticateAsPlayer(_factory, playerId);

        var questionId = await SeedQuestionAsync(correctOptionId: "B", QuestionDifficulty.Easy);

        var response = await client.PostAsJsonAsync("/api/v1/quiz/complete", new
        {
            playerId,
            eventId = Guid.NewGuid(),
            xpEarned = 999_999,
            coinsEarned = 999_999,
            answers = new[]
            {
                new { questionId, selectedOptionId = "B", answerTimeMs = 1_250 }
            }
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        body.GetProperty("awardedXp").GetInt32().Should().Be(8);
        body.GetProperty("awardedCoins").GetInt32().Should().Be(3);
        body.GetProperty("correctAnswers").GetInt32().Should().Be(1);
        body.GetProperty("totalQuestions").GetInt32().Should().Be(1);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var wallet = await db.PlayerWallets.AsNoTracking().SingleAsync(x => x.PlayerId == playerId);
        wallet.Xp.Should().Be(8);
        wallet.Coins.Should().Be(3);
    }

    private async Task<Guid> SeedQuestionAsync(string correctOptionId, QuestionDifficulty difficulty)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question($"Anti-cheat question {Guid.NewGuid():N}", "General", difficulty, correctOptionId, mediaKey: null);
        question.SetStatus("Approved");
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", "A"),
            new QuestionOption(question.Id, "B", "B")
        });

        db.Questions.Add(question);
        await db.SaveChangesAsync();

        return question.Id;
    }
}
