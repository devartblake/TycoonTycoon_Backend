using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Questions
{
    public static class QuestionsUploadEndpoints
    {
        public static void Map(WebApplication app)
        {
            app.MapPost("/upload-question", UploadQuestion)
               .WithTags("Questions")
               .WithOpenApi();
        }

        private static IResult UploadQuestion(
            [FromBody] UploadQuestionRequest req,
            ILoggerFactory loggerFactory)
        {
            if (string.IsNullOrWhiteSpace(req.QuestionTitle))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "questionTitle is required.");

            if (string.IsNullOrWhiteSpace(req.QuestionDetails))
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "questionDetails is required.");

            var logger = loggerFactory.CreateLogger("Questions.Upload");
            logger.LogInformation("Received question upload: {QuestionTitle}", req.QuestionTitle.Replace("\r", "").Replace("\n", ""));

            return Results.Ok(new UploadQuestionResponseDto(
                Message: "Question received successfully.",
                QuestionTitle: req.QuestionTitle,
                QuestionDetails: req.QuestionDetails
            ));
        }
    }
}
