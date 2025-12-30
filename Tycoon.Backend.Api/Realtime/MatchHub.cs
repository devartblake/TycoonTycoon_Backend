using Microsoft.AspNetCore.SignalR;

namespace Tycoon.Backend.Api.Realtime
{
    public class MatchHub : Hub
    {
        public async Task JoinMatch(Guid matchId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"match:{matchId}");

        public async Task SubmitAnswer(Guid matchId, string answerId)
            => await Clients.Group($"match:{matchId}")
                   .SendAsync("answer_submitted", new { matchId, user = Context.UserIdentifier, answerId });
    }
}
