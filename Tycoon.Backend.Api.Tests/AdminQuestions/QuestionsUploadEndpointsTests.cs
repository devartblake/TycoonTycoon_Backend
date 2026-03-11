using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminQuestions
{
    public sealed class QuestionsUploadEndpointsTests : IClassFixture<TycoonApiFactory>
    {
        private readonly HttpClient _http;

        public QuestionsUploadEndpointsTests(TycoonApiFactory factory)
        {
            _http = factory.CreateClient();
        }

        [Fact]
        public async Task UploadQuestion_ValidPayload_ReturnsOk()
        {
            var req = new UploadQuestionRequest(
                QuestionTitle: "How to implement JSON upload feature?",
                QuestionDetails: "I want to enable JSON-based uploads. What steps should I follow?"
            );

            var resp = await _http.PostAsJsonAsync("/upload-question", req);

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await resp.Content.ReadFromJsonAsync<UploadQuestionResponseDto>();
            result.Should().NotBeNull();
            result!.Message.Should().Be("Question received successfully.");
            result.QuestionTitle.Should().Be(req.QuestionTitle);
            result.QuestionDetails.Should().Be(req.QuestionDetails);
        }

        [Fact]
        public async Task UploadQuestion_MissingTitle_ReturnsBadRequest()
        {
            var req = new UploadQuestionRequest(
                QuestionTitle: "",
                QuestionDetails: "Some details"
            );

            var resp = await _http.PostAsJsonAsync("/upload-question", req);

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        }

        [Fact]
        public async Task UploadQuestion_MissingDetails_ReturnsBadRequest()
        {
            var req = new UploadQuestionRequest(
                QuestionTitle: "Some title",
                QuestionDetails: ""
            );

            var resp = await _http.PostAsJsonAsync("/upload-question", req);

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        }

        [Fact]
        public async Task UploadQuestion_WhitespaceTitle_ReturnsBadRequest()
        {
            var req = new UploadQuestionRequest(
                QuestionTitle: "   ",
                QuestionDetails: "Some details"
            );

            var resp = await _http.PostAsJsonAsync("/upload-question", req);

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        }
    }
}
