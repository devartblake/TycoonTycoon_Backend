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

namespace Synaptix.Backend.Api.Tests.Notifications;

public sealed class PlayerNotificationsEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public PlayerNotificationsEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Inbox_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/notifications/inbox");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FriendRequestAndAccept_CreateInboxItemsForTargetUsers()
    {
        using var senderClient = _factory.CreateClient();
        var sender = await AuthenticateAsync(senderClient, "notif-sender");

        using var recipientClient = _factory.CreateClient();
        var recipient = await AuthenticateAsync(recipientClient, "notif-recipient");

        var senderId = Guid.Parse(sender.UserId);
        var recipientId = Guid.Parse(recipient.UserId);

        var sendResponse = await senderClient.PostAsJsonAsync("/api/v1/friends/request", new
        {
            FromPlayerId = senderId,
            ToPlayerId = recipientId
        });
        sendResponse.EnsureSuccessStatusCode();

        var senderToRecipientInbox = await recipientClient.GetFromJsonAsync<PlayerNotificationsInboxResponseDto>("/api/v1/notifications/inbox");
        senderToRecipientInbox.Should().NotBeNull();
        senderToRecipientInbox!.Items.Should().ContainSingle();
        senderToRecipientInbox.Items[0].Type.Should().Be("friend");
        senderToRecipientInbox.Items[0].ActionRoute.Should().Be("/friends");

        var friendRequest = await sendResponse.Content.ReadFromJsonAsync<FriendRequestDto>();
        friendRequest.Should().NotBeNull();

        var acceptResponse = await recipientClient.PostAsJsonAsync($"/api/v1/friends/request/{friendRequest!.RequestId}/accept", new
        {
            PlayerId = recipientId
        });
        acceptResponse.EnsureSuccessStatusCode();

        var recipientToSenderInbox = await senderClient.GetFromJsonAsync<PlayerNotificationsInboxResponseDto>("/api/v1/notifications/inbox");
        recipientToSenderInbox.Should().NotBeNull();
        recipientToSenderInbox!.Items.Should().Contain(x => x.Title == "Friend request accepted");
    }

    [Fact]
    public async Task MarkRead_Delete_AndUnreadCount_RespectOwnership()
    {
        using var ownerClient = _factory.CreateClient();
        var owner = await AuthenticateAsync(ownerClient, "notif-owner");

        using var otherClient = _factory.CreateClient();
        await AuthenticateAsync(otherClient, "notif-other");

        var ownerId = Guid.Parse(owner.UserId);
        Guid notificationId;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.PlayerNotifications.Add(new PlayerNotification(
                ownerId,
                "system",
                "Welcome",
                "You have mail.",
                "/notifications",
                "{}",
                "mail",
                null));
            await db.SaveChangesAsync();
            notificationId = db.PlayerNotifications.Single(x => x.PlayerId == ownerId).Id;
        }

        var unread = await ownerClient.GetFromJsonAsync<UnreadCountResponseDto>("/api/v1/notifications/unread-count");
        unread.Should().NotBeNull();
        unread!.UnreadCount.Should().Be(1);

        var forbiddenRead = await otherClient.PostAsync($"/api/v1/notifications/{notificationId}/read", null);
        forbiddenRead.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await forbiddenRead.HasErrorCodeAsync("FORBIDDEN");

        var readResponse = await ownerClient.PostAsync($"/api/v1/notifications/{notificationId}/read", null);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        unread = await ownerClient.GetFromJsonAsync<UnreadCountResponseDto>("/api/v1/notifications/unread-count");
        unread!.UnreadCount.Should().Be(0);

        var deleteResponse = await ownerClient.DeleteAsync($"/api/v1/notifications/{notificationId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var inbox = await ownerClient.GetFromJsonAsync<PlayerNotificationsInboxResponseDto>("/api/v1/notifications/inbox");
        inbox.Should().NotBeNull();
        inbox!.Items.Should().BeEmpty();
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
