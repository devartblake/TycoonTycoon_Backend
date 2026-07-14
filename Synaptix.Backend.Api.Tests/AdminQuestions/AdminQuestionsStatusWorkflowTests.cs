using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.AdminQuestions;

public sealed class AdminQuestionsStatusWorkflowTests : IClassFixture<SynaptixApiFactory>
{
    private readonly HttpClient _http;

    public AdminQuestionsStatusWorkflowTests(SynaptixApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task ApproveEndpoint_ChangesStatusToApproved()
    {
        var createReq = new CreateQuestionRequest(
            Text: "Status workflow question?",
            Category: "General",
            Difficulty: QuestionDifficulty.Easy,
            Options: new[]
            {
                new QuestionOptionDto("A","Yes"),
                new QuestionOptionDto("B","No"),
            },
            CorrectOptionId: "A",
            Tags: new[] { "workflow" },
            MediaKey: null,
            Status: "Draft"
        );

        var createdResp = await _http.PostAsJsonAsync("/admin/questions", createReq);
        createdResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await createdResp.Content.ReadFromJsonAsync<QuestionDto>(TestJson.Default);
        createdBody.Should().NotBeNull();
        var id = createdBody!.Id;

        var approveResp = await _http.PostAsync($"/admin/questions/{id}/approve", null);
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var approved = await approveResp.Content.ReadFromJsonAsync<QuestionDto>(TestJson.Default);
        approved.Should().NotBeNull();
        approved!.Status.Should().Be("Approved");

        var listResp = await _http.GetFromJsonAsync<Dictionary<string, object>>("/admin/questions?status=Approved&page=1&pageSize=25");
        listResp.Should().NotBeNull();
    }
}
