using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.AdminQuestions;

// Covers the operator content-dashboard routes added for #420:
// GET /admin/questions/stats, GET /admin/questions/categories, POST /admin/questions/bulk-review.
public sealed class AdminQuestionsReviewDashboardTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminQuestionsReviewDashboardTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    private async Task<Guid> CreateDraftAsync(string category = "General")
    {
        var req = new CreateQuestionRequest(
            Text: $"Review dashboard question {Guid.NewGuid():N}?",
            Category: category,
            Difficulty: QuestionDifficulty.Easy,
            Options: new[] { new QuestionOptionDto("A", "Yes"), new QuestionOptionDto("B", "No") },
            CorrectOptionId: "A",
            Tags: Array.Empty<string>(),
            MediaKey: null,
            Status: "Draft");

        var resp = await _http.PostAsJsonAsync("/admin/questions", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resp.Content.ReadFromJsonAsync<QuestionDto>(TestJson.Default);
        return body!.Id;
    }

    [Fact]
    public async Task Stats_ReflectApproveAndRejectDeltas()
    {
        var before = await _http.GetFromJsonAsync<AdminQuestionStatsDto>("/admin/questions/stats", TestJson.Default);
        before.Should().NotBeNull();

        // 2 new drafts, then approve one and reject the other.
        var approveId = await CreateDraftAsync();
        var rejectId = await CreateDraftAsync();
        (await _http.PostAsync($"/admin/questions/{approveId}/approve", null)).EnsureSuccessStatusCode();
        (await _http.PostAsync($"/admin/questions/{rejectId}/reject", null)).EnsureSuccessStatusCode();

        var after = await _http.GetFromJsonAsync<AdminQuestionStatsDto>("/admin/questions/stats", TestJson.Default);
        after.Should().NotBeNull();

        after!.TotalApproved.Should().Be(before!.TotalApproved + 1);
        after.TotalRejected.Should().Be(before.TotalRejected + 1);
        after.ApprovalRate.Should().BeInRange(0d, 1d);
        after.AvgReviewTime.Should().BeGreaterThanOrEqualTo(0d);
    }

    [Fact]
    public async Task Categories_ReturnsDistinctSortedIncludingNew()
    {
        var unique = $"Cat-{Guid.NewGuid():N}";
        await CreateDraftAsync(unique);
        await CreateDraftAsync(unique); // duplicate category => must appear once

        var categories = await _http.GetFromJsonAsync<List<string>>("/admin/questions/categories", TestJson.Default);

        categories.Should().NotBeNull();
        categories!.Should().Contain(unique);
        categories.Count(c => c == unique).Should().Be(1);
        categories.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkReview_Approve_SetsEveryQuestionApproved()
    {
        var ids = new[] { await CreateDraftAsync(), await CreateDraftAsync(), await CreateDraftAsync() };

        var resp = await _http.PostAsJsonAsync("/admin/questions/bulk-review",
            new BulkReviewQuestionsRequest(ids, "approve"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await resp.Content.ReadFromJsonAsync<BulkReviewResultDto>(TestJson.Default);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Reviewed.Should().Be(ids.Length);

        foreach (var id in ids)
        {
            var q = await _http.GetFromJsonAsync<QuestionDto>($"/admin/questions/{id}", TestJson.Default);
            q!.Status.Should().Be("Approved");
        }
    }

    [Fact]
    public async Task BulkReview_InvalidVerdict_ReturnsBadRequest()
    {
        var resp = await _http.PostAsJsonAsync("/admin/questions/bulk-review",
            new BulkReviewQuestionsRequest(new[] { Guid.NewGuid() }, "maybe"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
