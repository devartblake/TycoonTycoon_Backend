using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminQuestions
{
    public sealed class AdminQuestionsImportExportTests : IClassFixture<SynaptixApiFactory>
    {
        private readonly HttpClient _http;

        public AdminQuestionsImportExportTests(SynaptixApiFactory factory)
        {
            _http = factory.CreateClient().WithAdminOpsKey();
        }

        [Fact]
        public async Task Import_Then_Export_Returns_Items()
        {
            var importReq = new ImportQuestionsRequest(new[]
            {
                new CreateQuestionRequest(
                    Text: "Capital of France?",
                    Category: "Geography",
                    Difficulty: QuestionDifficulty.Easy,
                    Options: new[]
                    {
                        new QuestionOptionDto("A","Paris"),
                        new QuestionOptionDto("B","Berlin"),
                    },
                    CorrectOptionId: "A",
                    Tags: new[] { "capital", "europe" },
                    MediaKey: null
                ),
                new CreateQuestionRequest(
                    Text: "Largest planet?",
                    Category: "Science",
                    Difficulty: QuestionDifficulty.Medium,
                    Options: new[]
                    {
                        new QuestionOptionDto("A","Earth"),
                        new QuestionOptionDto("B","Jupiter"),
                    },
                    CorrectOptionId: "B",
                    Tags: new[] { "space" },
                    MediaKey: null
                )
            });

            var importResp = await _http.PostAsJsonAsync("/admin/questions/import", importReq);
            importResp.IsSuccessStatusCode.Should().BeTrue();

            var import = await importResp.Content.ReadFromJsonAsync<ImportQuestionsResultDto>(TestJson.Default);
            import!.Received.Should().Be(2);
            import.Created.Should().BeGreaterThanOrEqualTo(2);

            // Export filtered by tag
            var exportResp = await _http.GetAsync("/admin/questions/export?tags=capital&tagMode=Any&page=1&pageSize=100");
            exportResp.IsSuccessStatusCode.Should().BeTrue();

            var export = await exportResp.Content.ReadFromJsonAsync<QuestionListResponseDto>(TestJson.Default);
            export!.Items.Should().NotBeEmpty();
            export.Items.Any(i => i.Category == "Geography").Should().BeTrue();
        }

        [Fact]
        public async Task ImportTaxonomy_Upserts_BySourceDatasetAndSourceQuestionId()
        {
            var request = new TaxonomyImportQuestionsRequest(new[]
            {
                new TaxonomyQuestionImportItemDto(
                    Id: "flutter-science-001",
                    Text: "What force keeps planets in orbit?",
                    Question: null,
                    Category: "physics",
                    Difficulty: "Medium",
                    Options: new[]
                    {
                        new TaxonomyQuestionOptionImportDto("A", null, "Gravity", true),
                        new TaxonomyQuestionOptionImportDto("B", null, "Evaporation", false)
                    },
                    Answers: null,
                    CorrectOptionId: null,
                    CorrectAnswer: "Gravity",
                    Tags: new[] { "space" },
                    MediaKey: null,
                    ImageUrl: null,
                    VideoUrl: null,
                    AudioUrl: null,
                    Type: "multiple_choice",
                    Taxonomy: new QuestionTaxonomyInputDto(
                        CanonicalCategory: "science",
                        DisplayCategory: "Science",
                        Subject: "stem",
                        Topic: "physics",
                        Subtopic: "gravity",
                        GradeBand: "middle_school",
                        AgeGroup: "teen",
                        Audience: "teen",
                        SourceDataset: "flutter/science/physics",
                        TaxonomyTags: new[] { "flutter_fallback", "physics" }))
            }, Strict: true);

            var first = await _http.PostAsJsonAsync("/admin/questions/import-taxonomy", request);
            first.EnsureSuccessStatusCode();
            var firstPayload = await first.Content.ReadFromJsonAsync<ImportQuestionsResultDto>(TestJson.Default);
            firstPayload!.Received.Should().Be(1);
            firstPayload.Created.Should().Be(1);
            firstPayload.Failed.Should().Be(0);

            var second = await _http.PostAsJsonAsync("/admin/questions/import-taxonomy", request);
            var secondBody = await second.Content.ReadAsStringAsync();
            second.IsSuccessStatusCode.Should().BeTrue(secondBody);
            var secondPayload = await second.Content.ReadFromJsonAsync<ImportQuestionsResultDto>(TestJson.Default);
            secondPayload!.Received.Should().Be(1);
            secondPayload.Created.Should().Be(0);
            secondPayload.Failed.Should().Be(0);

            var exportResp = await _http.GetAsync("/admin/questions/export?category=science&page=1&pageSize=100");
            exportResp.EnsureSuccessStatusCode();
            var export = await exportResp.Content.ReadFromJsonAsync<QuestionListResponseDto>(TestJson.Default);
            export!.Items.Should().ContainSingle(i => i.Taxonomy!.SourceDataset == "flutter/science/physics" &&
                                                      i.Taxonomy.SourceQuestionId == "flutter-science-001");
        }
    }
}
