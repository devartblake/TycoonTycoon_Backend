using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Messaging;

public sealed class MessagesEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public MessagesEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Conversations_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/messages/conversations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDirectConversation_IsIdempotentForSameParticipants()
    {
        using var senderClient = _factory.CreateClient();
        var sender = await AuthenticateAsync(senderClient, "msg-conv-a");

        using var recipientClient = _factory.CreateClient();
        var recipient = await AuthenticateAsync(recipientClient, "msg-conv-b");

        var request = new CreateDirectConversationRequestDto(Guid.Parse(recipient.UserId));

        var first = await senderClient.PostAsJsonAsync("/messages/conversations/direct", request);
        var second = await senderClient.PostAsJsonAsync("/messages/conversations/direct", request);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = await first.Content.ReadFromJsonAsync<DirectConversationSummaryDto>();
        var secondBody = await second.Content.ReadFromJsonAsync<DirectConversationSummaryDto>();

        firstBody.Should().NotBeNull();
        secondBody.Should().NotBeNull();
        secondBody!.Id.Should().Be(firstBody!.Id);
    }

    [Fact]
    public async Task SelfDm_IsRejected()
    {
        using var client = _factory.CreateClient();
        var user = await AuthenticateAsync(client, "msg-self");
        var playerId = Guid.Parse(user.UserId);

        var response = await client.PostAsJsonAsync("/messages/conversations/direct", new CreateDirectConversationRequestDto(playerId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await response.HasErrorCodeAsync("SELF_DM_NOT_ALLOWED");
    }

    [Fact]
    public async Task SendMessage_ClientMessageIdIsIdempotent_AndUnreadCountTracksReadState()
    {
        using var senderClient = _factory.CreateClient();
        var sender = await AuthenticateAsync(senderClient, "msg-sender");

        using var recipientClient = _factory.CreateClient();
        var recipient = await AuthenticateAsync(recipientClient, "msg-recipient");

        var conversationResponse = await senderClient.PostAsJsonAsync(
            "/messages/conversations/direct",
            new CreateDirectConversationRequestDto(Guid.Parse(recipient.UserId)));
        conversationResponse.EnsureSuccessStatusCode();

        var conversation = await conversationResponse.Content.ReadFromJsonAsync<DirectConversationSummaryDto>();
        conversation.Should().NotBeNull();

        var request = new SendDirectMessageRequestDto("hello there", "client-123");
        var firstSend = await senderClient.PostAsJsonAsync($"/messages/conversations/{conversation!.Id}/messages", request);
        var secondSend = await senderClient.PostAsJsonAsync($"/messages/conversations/{conversation.Id}/messages", request);

        firstSend.StatusCode.Should().Be(HttpStatusCode.OK);
        secondSend.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = await firstSend.Content.ReadFromJsonAsync<DirectMessageDto>();
        var secondBody = await secondSend.Content.ReadFromJsonAsync<DirectMessageDto>();

        firstBody.Should().NotBeNull();
        secondBody.Should().NotBeNull();
        secondBody!.Id.Should().Be(firstBody!.Id);

        var recipientUnread = await recipientClient.GetFromJsonAsync<UnreadCountResponseDto>("/messages/unread-count");
        recipientUnread.Should().NotBeNull();
        recipientUnread!.UnreadCount.Should().Be(1);

        var messages = await recipientClient.GetFromJsonAsync<List<DirectMessageDto>>($"/messages/conversations/{conversation.Id}/messages");
        messages.Should().NotBeNull();
        messages!.Should().ContainSingle();
        messages![0].Content.Should().Be("hello there");

        var readResponse = await recipientClient.PostAsync($"/messages/conversations/{conversation.Id}/read", null);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        recipientUnread = await recipientClient.GetFromJsonAsync<UnreadCountResponseDto>("/messages/unread-count");
        recipientUnread!.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task NonParticipant_CannotReadConversationMessages()
    {
        using var senderClient = _factory.CreateClient();
        var sender = await AuthenticateAsync(senderClient, "msg-owner-a");

        using var recipientClient = _factory.CreateClient();
        var recipient = await AuthenticateAsync(recipientClient, "msg-owner-b");

        using var otherClient = _factory.CreateClient();
        await AuthenticateAsync(otherClient, "msg-owner-c");

        var conversationResponse = await senderClient.PostAsJsonAsync(
            "/messages/conversations/direct",
            new CreateDirectConversationRequestDto(Guid.Parse(recipient.UserId)));
        conversationResponse.EnsureSuccessStatusCode();

        var conversation = await conversationResponse.Content.ReadFromJsonAsync<DirectConversationSummaryDto>();
        conversation.Should().NotBeNull();

        var forbidden = await otherClient.GetAsync($"/messages/conversations/{conversation!.Id}/messages");

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await forbidden.HasErrorCodeAsync("FORBIDDEN");
    }

    private async Task<SignupResponse> AuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        var handle = $"{prefix}_{Guid.NewGuid():N}";
        var user = new User(email, handle, "test-password-hash");
        var token = string.Empty;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
            db.Users.Add(user);
            await db.SaveChangesAsync();
            token = CreateAccessToken(user, jwtSettings);
        }

        var response = new SignupResponse(
            AccessToken: token,
            RefreshToken: "test-refresh-token",
            ExpiresIn: 3600,
            UserId: user.Id.ToString(),
            User: new UserDto(user.Id, user.Handle, user.Email, user.Country, user.AvatarUrl, user.Tier, user.Mmr));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", response.AccessToken);

        return response;
    }

    private static string CreateAccessToken(User user, JwtSettings settings)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("handle", user.Handle),
            new("role", "user"),
            new("scope", "profile:read profile:write gameplay:read gameplay:write"),
            new("client_type", "user"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: "mobile-app",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
