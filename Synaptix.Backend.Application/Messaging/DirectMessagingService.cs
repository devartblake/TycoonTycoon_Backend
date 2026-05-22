using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Realtime;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Messaging
{
    public sealed class DirectMessagingService(IAppDb db, IDirectMessageNotifier notifier)
    {
        public async Task<DirectConversationListResponseDto> GetConversationsAsync(Guid playerId, int page, int pageSize, CancellationToken ct)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : Math.Clamp(pageSize, 1, 100);

            var participantRows = db.DirectMessageConversationParticipants
                .AsNoTracking()
                .Where(x => x.PlayerId == playerId);

            var total = await participantRows.CountAsync(ct);
            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            var rows = await participantRows
                .OrderByDescending(x => x.LastReadAtUtc ?? DateTimeOffset.MinValue)
                .Join(
                    db.DirectMessageConversations.AsNoTracking(),
                    participant => participant.ConversationId,
                    conversation => conversation.Id,
                    (participant, conversation) => new
                    {
                        Participant = participant,
                        Conversation = conversation
                    })
                .OrderByDescending(x => x.Conversation.UpdatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Conversation.Id,
                    x.Conversation.Type,
                    x.Conversation.CreatedAtUtc,
                    x.Conversation.UpdatedAtUtc,
                    x.Participant.LastReadAtUtc
                })
                .ToListAsync(ct);

            var conversationIds = rows.Select(x => x.Id).ToList();
            var participantMap = await db.DirectMessageConversationParticipants
                .AsNoTracking()
                .Where(x => conversationIds.Contains(x.ConversationId))
                .ToListAsync(ct);

            var otherPlayerIds = participantMap
                .Where(x => x.PlayerId != playerId)
                .Select(x => x.PlayerId)
                .Distinct()
                .ToList();

            var users = await db.Users
                .AsNoTracking()
                .Where(x => otherPlayerIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Handle, x.AvatarUrl })
                .ToListAsync(ct);

            var userMap = users.ToDictionary(x => x.Id);

            var latestMessages = await db.DirectMessages
                .AsNoTracking()
                .Where(x => conversationIds.Contains(x.ConversationId))
                .GroupBy(x => x.ConversationId)
                .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
                .ToListAsync(ct);

            var latestMessageMap = latestMessages.ToDictionary(x => x.ConversationId);

            var unreadCounts = await GetUnreadCountsAsync(playerId, conversationIds, ct);

            var items = rows.Select(row =>
            {
                var otherParticipant = participantMap
                    .FirstOrDefault(x => x.ConversationId == row.Id && x.PlayerId != playerId);

                userMap.TryGetValue(otherParticipant?.PlayerId ?? Guid.Empty, out var otherUser);
                latestMessageMap.TryGetValue(row.Id, out var latestMessage);
                unreadCounts.TryGetValue(row.Id, out var unreadCount);

                var participantIds = participantMap
                    .Where(x => x.ConversationId == row.Id)
                    .Select(x => x.PlayerId)
                    .ToList();

                return new DirectConversationSummaryDto(
                    row.Id,
                    row.Type,
                    participantIds,
                    otherUser?.Handle ?? "Direct Message",
                    otherUser?.AvatarUrl,
                    latestMessage?.Content,
                    latestMessage?.CreatedAtUtc,
                    unreadCount,
                    row.CreatedAtUtc,
                    row.UpdatedAtUtc);
            }).ToList();

            return new DirectConversationListResponseDto(items, page, pageSize, total, totalPages);
        }

        public async Task<(string? Error, DirectConversationSummaryDto? Conversation)> GetOrCreateDirectConversationAsync(
            Guid playerId,
            Guid targetPlayerId,
            CancellationToken ct)
        {
            if (playerId == Guid.Empty || targetPlayerId == Guid.Empty)
                return ("validation_error", null);

            if (playerId == targetPlayerId)
                return ("self_dm_not_allowed", null);

            var targetUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == targetPlayerId && x.IsActive, ct);

            if (targetUser is null)
                return ("not_found", null);

            var existingConversationId = await db.DirectMessageConversationParticipants
                .Where(x => x.PlayerId == playerId)
                .Join(
                    db.DirectMessageConversationParticipants.Where(x => x.PlayerId == targetPlayerId),
                    left => left.ConversationId,
                    right => right.ConversationId,
                    (left, right) => left.ConversationId)
                .FirstOrDefaultAsync(ct);

            if (existingConversationId != Guid.Empty)
            {
                var existing = await GetConversationSummaryAsync(playerId, existingConversationId, ct);
                return existing is null ? ("not_found", null) : (null, existing);
            }

            var conversation = new DirectMessageConversation(playerId, targetPlayerId);
            db.DirectMessageConversations.Add(conversation);
            await db.SaveChangesAsync(ct);

            var created = await GetConversationSummaryAsync(playerId, conversation.Id, ct);
            return created is null ? ("not_found", null) : (null, created);
        }

        public async Task<(string? Error, IReadOnlyList<DirectMessageDto>? Messages)> GetMessagesAsync(
            Guid playerId,
            Guid conversationId,
            CancellationToken ct)
        {
            var participant = await db.DirectMessageConversationParticipants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.PlayerId == playerId, ct);

            if (participant is null)
            {
                var exists = await db.DirectMessageConversations.AsNoTracking().AnyAsync(x => x.Id == conversationId, ct);
                return exists ? ("forbidden", null) : ("not_found", null);
            }

            var messages = await db.DirectMessages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId)
                .OrderBy(x => x.CreatedAtUtc)
                .Join(
                    db.Users.AsNoTracking(),
                    message => message.SenderId,
                    user => user.Id,
                    (message, user) => new DirectMessageDto(
                        message.Id,
                        message.ConversationId,
                        message.SenderId,
                        user.Handle,
                        message.Content,
                        message.Type,
                        message.Status,
                        message.CreatedAtUtc))
                .ToListAsync(ct);

            return (null, messages);
        }

        public async Task<(string? Error, DirectMessageDto? Message)> SendMessageAsync(
            Guid playerId,
            Guid conversationId,
            string content,
            string? clientMessageId,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(content))
                return ("validation_error", null);

            var conversation = await db.DirectMessageConversations
                .Include(x => x.Participants)
                .FirstOrDefaultAsync(x => x.Id == conversationId, ct);

            if (conversation is null)
                return ("not_found", null);

            var senderParticipant = conversation.Participants.FirstOrDefault(x => x.PlayerId == playerId);
            if (senderParticipant is null)
                return ("forbidden", null);

            var normalizedClientMessageId = string.IsNullOrWhiteSpace(clientMessageId) ? null : clientMessageId.Trim();
            if (normalizedClientMessageId is not null)
            {
                var existing = await db.DirectMessages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ConversationId == conversationId &&
                        x.SenderId == playerId &&
                        x.ClientMessageId == normalizedClientMessageId, ct);

                if (existing is not null)
                {
                    var sender = await db.Users.AsNoTracking()
                        .Where(x => x.Id == existing.SenderId)
                        .Select(x => x.Handle)
                        .FirstOrDefaultAsync(ct);

                    return (null, new DirectMessageDto(
                        existing.Id,
                        existing.ConversationId,
                        existing.SenderId,
                        sender ?? "Unknown",
                        existing.Content,
                        existing.Type,
                        existing.Status,
                        existing.CreatedAtUtc));
                }
            }

            var message = new DirectMessage(conversationId, playerId, content, normalizedClientMessageId);
            db.DirectMessages.Add(message);

            var now = DateTimeOffset.UtcNow;
            conversation.Touch(now);
            senderParticipant.MarkRead(message.Id, now);

            await db.SaveChangesAsync(ct);

            var senderName = await db.Users.AsNoTracking()
                .Where(x => x.Id == playerId)
                .Select(x => x.Handle)
                .FirstOrDefaultAsync(ct);

            foreach (var participant in conversation.Participants.Where(x => x.PlayerId != playerId))
            {
                var unreadCount = await GetUnreadCountAsync(participant.PlayerId, ct);
                await notifier.NotifyDirectMessagesUpdatedAsync(participant.PlayerId, conversationId, unreadCount, "message_sent", ct);
            }

            var senderUnreadCount = await GetUnreadCountAsync(playerId, ct);
            await notifier.NotifyDirectMessagesUpdatedAsync(playerId, conversationId, senderUnreadCount, "message_sent", ct);

            return (null, new DirectMessageDto(
                message.Id,
                message.ConversationId,
                message.SenderId,
                senderName ?? "Unknown",
                message.Content,
                message.Type,
                message.Status,
                message.CreatedAtUtc));
        }

        public async Task<string?> MarkConversationReadAsync(Guid playerId, Guid conversationId, CancellationToken ct)
        {
            var participant = await db.DirectMessageConversationParticipants
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.PlayerId == playerId, ct);

            if (participant is null)
            {
                var exists = await db.DirectMessageConversations.AsNoTracking().AnyAsync(x => x.Id == conversationId, ct);
                return exists ? "forbidden" : "not_found";
            }

            var latestMessage = await db.DirectMessages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Id, x.CreatedAtUtc })
                .FirstOrDefaultAsync(ct);

            participant.MarkRead(latestMessage?.Id, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);

            var unreadCount = await GetUnreadCountAsync(playerId, ct);
            await notifier.NotifyDirectMessagesUpdatedAsync(playerId, conversationId, unreadCount, "conversation_read", ct);
            return null;
        }

        public async Task<int> GetUnreadCountAsync(Guid playerId, CancellationToken ct)
        {
            var conversationIds = await db.DirectMessageConversationParticipants
                .AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .Select(x => x.ConversationId)
                .ToListAsync(ct);

            var unreadCounts = await GetUnreadCountsAsync(playerId, conversationIds, ct);
            return unreadCounts.Values.Sum();
        }

        private async Task<DirectConversationSummaryDto?> GetConversationSummaryAsync(Guid playerId, Guid conversationId, CancellationToken ct)
        {
            var list = await GetConversationsAsync(playerId, 1, 200, ct);
            return list.Items.FirstOrDefault(x => x.Id == conversationId);
        }

        private async Task<Dictionary<Guid, int>> GetUnreadCountsAsync(Guid playerId, IReadOnlyCollection<Guid> conversationIds, CancellationToken ct)
        {
            if (conversationIds.Count == 0)
                return new Dictionary<Guid, int>();

            var participantStates = await db.DirectMessageConversationParticipants
                .AsNoTracking()
                .Where(x => x.PlayerId == playerId && conversationIds.Contains(x.ConversationId))
                .Select(x => new
                {
                    x.ConversationId,
                    x.LastReadAtUtc
                })
                .ToListAsync(ct);

            var result = new Dictionary<Guid, int>();
            foreach (var state in participantStates)
            {
                var count = await db.DirectMessages
                    .AsNoTracking()
                    .Where(x => x.ConversationId == state.ConversationId && x.SenderId != playerId)
                    .CountAsync(x => !state.LastReadAtUtc.HasValue || x.CreatedAtUtc > state.LastReadAtUtc.Value, ct);

                result[state.ConversationId] = count;
            }

            return result;
        }
    }
}
