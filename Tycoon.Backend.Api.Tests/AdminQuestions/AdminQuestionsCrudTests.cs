using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminQuestions
{
    public sealed class AdminQuestionsCrudTests : IClassFixture<TycoonApiFactory>
    {
        private readonly HttpClient _http;

        public AdminQuestionsCrudTests(TycoonApiFactory factory)
        {
            _http = factory.CreateClient().WithAdminOpsKey();
        }

        [Fact]
        public async Task AdminRoutes_Require_OpsKey()
        {
            using var noKey = new TycoonApiFactory().CreateClient(); // new client with no header

            var r = await noKey.GetAsync("/admin/questions");
            r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await r.HasErrorCodeAsync("UNAUTHORIZED");
        }

        [Fact]
        public async Task Create_Get_Update_List_BulkDelete_Works()
        {
            // Create
            var createReq = new CreateQuestionRequest(
                Text: "What is 2+2?",
                Category: "Math",
                Difficulty: QuestionDifficulty.Easy,
                Options: new[]
                {
                    new QuestionOptionDto("A","3"),
                    new QuestionOptionDto("B","4"),
                    new QuestionOptionDto("C","5"),
                },
                CorrectOptionId: "B",
                Tags: new[] { "addition", "basics" },
                MediaKey: null
            );

            var createdResp = await _http.PostAsJsonAsync("/admin/questions", createReq);
            createdResp.IsSuccessStatusCode.Should().BeTrue();

            var created = await createdResp.Content.ReadFromJsonAsync<QuestionDto>();
            created.Should().NotBeNull();
            created!.Id.Should().NotBeEmpty();
            created.Category.Should().Be("Math");
            created.Tags.Should().Contain("addition");

            var id = created.Id;

            // Get
            var getResp = await _http.GetAsync($"/admin/questions/{id}");
            getResp.IsSuccessStatusCode.Should().BeTrue();

            var got = await getResp.Content.ReadFromJsonAsync<QuestionDto>();
            got!.CorrectOptionId.Should().Be("B");
            got.Options.Count.Should().Be(3);

            // Update
            var updateReq = new UpdateQuestionRequest(
                Text: "What is 3+3?",
                Category: "Math",
                Difficulty: QuestionDifficulty.Easy,
                Options: new[]
                {
                    new QuestionOptionDto("A","5"),
                    new QuestionOptionDto("B","6"),
                    new QuestionOptionDto("C","7"),
                },
                CorrectOptionId: "B",
                Tags: new[] { "addition" },
                MediaKey: "uploads/20250101/test.png"
            );

            var updateResp = await _http.PutAsJsonAsync($"/admin/questions/{id}", updateReq);
            updateResp.IsSuccessStatusCode.Should().BeTrue();

            var updated = await updateResp.Content.ReadFromJsonAsync<QuestionDto>();
            updated!.Text.Should().Contain("3+3");
            updated.HasMedia().Should().BeTrue();

            // List with tag filter
            var listResp = await _http.GetAsync("/admin/questions?tags=addition&tagMode=Any&page=1&pageSize=20");
            listResp.IsSuccessStatusCode.Should().BeTrue();

            var list = await listResp.Content.ReadFromJsonAsync<QuestionListResponseDto>();
            list!.Total.Should().BeGreaterThan(0);
            list.Items.Should().Contain(i => i.Id == id);

            // Facets present (grid-friendly)
            list.TagFacets.Should().NotBeNull();
            list.CategoryFacets.Should().NotBeNull();
            list.DifficultyFacets.Should().NotBeNull();

            // Bulk delete
            var bulkResp = await _http.PostAsJsonAsync("/admin/questions/bulk-delete", new BulkDeleteQuestionsRequest(new[] { id }));
            bulkResp.IsSuccessStatusCode.Should().BeTrue();

            var bulk = await bulkResp.Content.ReadFromJsonAsync<BulkDeleteResultDto>();
            bulk!.Requested.Should().Be(1);
            bulk.Deleted.Should().Be(1);

            // Ensure removed
            var getAfter = await _http.GetAsync($"/admin/questions/{id}");
            getAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);

            await getAfter.HasErrorCodeAsync("NOT_FOUND");
        }
    }

    internal static class QuestionDtoExt
    {
        public static bool HasMedia(this QuestionDto q) => !string.IsNullOrWhiteSpace(q.MediaKey);
    }
}
