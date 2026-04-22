using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Study
{
    public sealed record AddStudyFavoriteQuestion(Guid PlayerId, Guid QuestionId) : IRequest<ManageStudyFavoriteQuestionResult>;
    public sealed record RemoveStudyFavoriteQuestion(Guid PlayerId, Guid QuestionId) : IRequest<ManageStudyFavoriteQuestionResult>;

    public sealed record ManageStudyFavoriteQuestionResult(string Status, int FavoritesCount = 0);

    public sealed class AddStudyFavoriteQuestionHandler
        : IRequestHandler<AddStudyFavoriteQuestion, ManageStudyFavoriteQuestionResult>
    {
        private readonly IAppDb _db;

        public AddStudyFavoriteQuestionHandler(IAppDb db) => _db = db;

        public async Task<ManageStudyFavoriteQuestionResult> Handle(AddStudyFavoriteQuestion request, CancellationToken ct)
        {
            var questionExists = await _db.Questions
                .AsNoTracking()
                .AnyAsync(x => x.Id == request.QuestionId && x.Status == "Approved", ct);

            if (!questionExists)
                return new ManageStudyFavoriteQuestionResult("QuestionNotFound");

            var exists = await _db.QuestionStudyFavorites
                .AnyAsync(x => x.PlayerId == request.PlayerId && x.QuestionId == request.QuestionId, ct);

            if (!exists)
            {
                _db.QuestionStudyFavorites.Add(new QuestionStudyFavorite(request.PlayerId, request.QuestionId));
                await _db.SaveChangesAsync(ct);
            }

            var count = await _db.QuestionStudyFavorites
                .AsNoTracking()
                .CountAsync(x => x.PlayerId == request.PlayerId, ct);

            return new ManageStudyFavoriteQuestionResult("Ok", count);
        }
    }

    public sealed class RemoveStudyFavoriteQuestionHandler
        : IRequestHandler<RemoveStudyFavoriteQuestion, ManageStudyFavoriteQuestionResult>
    {
        private readonly IAppDb _db;

        public RemoveStudyFavoriteQuestionHandler(IAppDb db) => _db = db;

        public async Task<ManageStudyFavoriteQuestionResult> Handle(RemoveStudyFavoriteQuestion request, CancellationToken ct)
        {
            var favorite = await _db.QuestionStudyFavorites
                .FirstOrDefaultAsync(x => x.PlayerId == request.PlayerId && x.QuestionId == request.QuestionId, ct);

            if (favorite is not null)
            {
                _db.QuestionStudyFavorites.Remove(favorite);
                await _db.SaveChangesAsync(ct);
            }

            var count = await _db.QuestionStudyFavorites
                .AsNoTracking()
                .CountAsync(x => x.PlayerId == request.PlayerId, ct);

            return new ManageStudyFavoriteQuestionResult("Ok", count);
        }
    }
}
