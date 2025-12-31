using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminQuestions
{
    public sealed class AdminQuestionsImportExportTests : IClassFixture<TycoonApiFactory>
    {
        private readonly HttpClient _http;

        public AdminQuestionsImportExportTests(TycoonApiFactory factory)
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

            var import = await importResp.Content.ReadFromJsonAsync<ImportQuestionsResultDto>();
            import!.Received.Should().Be(2);
            import.Created.Should().BeGreaterThanOrEqualTo(2);

            // Export filtered by tag
            var exportResp = await _http.GetAsync("/admin/questions/export?tags=capital&tagMode=Any&page=1&pageSize=100");
            exportResp.IsSuccessStatusCode.Should().BeTrue();

            var export = await exportResp.Content.ReadFromJsonAsync<QuestionListResponseDto>();
            export!.Items.Should().NotBeEmpty();
            export.Items.Any(i => i.Category == "Geography").Should().BeTrue();
        }
    }
}
