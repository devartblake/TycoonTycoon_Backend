using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaptix.Backend.Api.Payments.PayPal;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Store;

public sealed class StorePayPalEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public StorePayPalEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePayPalOrder_WithAuthenticatedOwner_ReturnsApproveUrl()
    {
        var fakeGateway = new FakePayPalGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "paypal-order");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);
        var playerId = Guid.Parse(signup.UserId);

        await SeedStoreItemAsync(factory, new StoreItem
        {
            Sku = "powerup:skip",
            Name = "Skip Powerup",
            Description = "Skip one question.",
            ItemType = "powerup",
            GrantQuantity = 1,
            IsActive = true,
            SortOrder = 1
        });

        await StoreTestSupport.EnableStorePurchasesAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/store/payments/paypal/order",
            new CreatePayPalOrderRequest(playerId, "powerup:skip", 2));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreatePayPalOrderResponse>();
        body.Should().NotBeNull();
        body!.OrderId.Should().Be("ORDER-123");
        body.ApproveUrl.Should().Be("https://paypal.test/approve/ORDER-123");
        body.TotalAmount.Should().Be(5.98m);
    }

    [Fact]
    public async Task CapturePayPalOrder_AppliesInventoryTransaction()
    {
        var fakeGateway = new FakePayPalGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "paypal-capture");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);
        var playerId = Guid.Parse(signup.UserId);

        await SeedStoreItemAsync(factory, new StoreItem
        {
            Sku = "powerup:skip",
            Name = "Skip Powerup",
            Description = "Skip one question.",
            ItemType = "powerup",
            GrantQuantity = 3,
            IsActive = true,
            SortOrder = 1
        });

        fakeGateway.NextCaptureResult = new PayPalCaptureOrderResult(
            "ORDER-123",
            "COMPLETED",
            "CAPTURE-123",
            $"{playerId:N}|powerup:skip|2",
            "USD",
            5.98m);

        await StoreTestSupport.EnableStorePurchasesAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/store/payments/paypal/capture",
            new CapturePayPalOrderRequest(playerId, "ORDER-123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var transaction = await db.PlayerTransactions
            .Include(t => t.ItemChanges)
            .Include(t => t.Actors)
            .FirstOrDefaultAsync(t => t.Kind == "paypal-order-payment"
                                      && t.Actors.Any(a => a.PlayerId == playerId));

        transaction.Should().NotBeNull();
        transaction!.Actors.Should().ContainSingle(a => a.PlayerId == playerId);
        transaction.ItemChanges.Should().ContainSingle(i => i.ItemType == "powerup:skip" && i.Quantity == 6);
    }

    [Fact]
    public async Task CreateAndCancelPayPalSubscription_StoresStatus()
    {
        var fakeGateway = new FakePayPalGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "paypal-sub");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);
        var playerId = Guid.Parse(signup.UserId);

        await StoreTestSupport.EnableStorePurchasesAsync(factory);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/store/subscription/paypal/create",
            new CreatePayPalSubscriptionRequest(playerId, "premium", "monthly"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createBody = await createResponse.Content.ReadFromJsonAsync<CreatePayPalSubscriptionResponse>();
        createBody.Should().NotBeNull();
        createBody!.SubscriptionId.Should().Be("I-SUB-123");

        fakeGateway.NextSubscriptionDetails = new PayPalSubscriptionDetails(
            "I-SUB-123",
            "ACTIVE",
            "P-PLAN-123",
            $"{playerId:N}|premium|monthly",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            "player@example.com");

        var cancelResponse = await client.PostAsJsonAsync(
            "/api/v1/store/subscription/paypal/cancel",
            new CancelPayPalSubscriptionRequest(playerId, "I-SUB-123", "testing"));

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var statusTx = await db.PlayerTransactions
            .Include(t => t.Actors)
            .Where(t => t.Kind == "paypal-subscription-status")
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync();

        statusTx.Should().NotBeNull();
        statusTx!.Actors.Should().ContainSingle(a => a.PlayerId == playerId);
    }

    [Fact]
    public async Task PayPalWebhook_WithInvalidSignature_ReturnsBadRequest()
    {
        var fakeGateway = new FakePayPalGateway
        {
            VerifyWebhookResult = false
        };

        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        using var request = BuildWebhookRequest(new
        {
            id = "WH-INVALID-1",
            event_type = "BILLING.SUBSCRIPTION.ACTIVATED",
            resource = new { id = "I-SUB-123", custom_id = $"{Guid.NewGuid():N}|premium|monthly", status = "ACTIVE" }
        });

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await response.HasErrorCodeAsync("PAYPAL_WEBHOOK_INVALID");
    }

    [Fact]
    public async Task PayPalWebhook_SubscriptionActivated_UpdatesSubscriptionStatus()
    {
        var fakeGateway = new FakePayPalGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "paypal-wh-sub");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);
        var playerId = Guid.Parse(signup.UserId);

        using var request = BuildWebhookRequest(new
        {
            id = "WH-SUB-1",
            event_type = "BILLING.SUBSCRIPTION.ACTIVATED",
            resource = new
            {
                id = "I-SUB-999",
                custom_id = $"{playerId:N}|premium|monthly",
                status = "ACTIVE",
                billing_info = new
                {
                    next_billing_time = DateTimeOffset.UtcNow.AddDays(30).ToString("O")
                }
            }
        });

        var webhookResponse = await client.SendAsync(request);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await client.GetFromJsonAsync<SubscriptionStatusDto>($"/api/v1/store/subscription/status/{playerId}");
        status.Should().NotBeNull();
        status!.Provider.Should().Be("paypal");
        status.ProviderSubscriptionId.Should().Be("I-SUB-999");
        status.ProviderStatus.Should().Be("ACTIVE");
        status.IsActive.Should().BeTrue();
    }

    private WebApplicationFactory<Program> CreateFactory(FakePayPalGateway fakeGateway)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PayPal:Enabled"] = "true",
                    ["PayPal:ClientId"] = "client-test",
                    ["PayPal:ClientSecret"] = "secret-test",
                    ["PayPal:BaseUrl"] = "https://api-m.sandbox.paypal.com",
                    ["PayPal:ReturnUrl"] = "https://localhost:3000/store/paypal/return",
                    ["PayPal:CancelUrl"] = "https://localhost:3000/store/paypal/cancel",
                    ["PayPal:Catalog:0:Sku"] = "powerup:skip",
                    ["PayPal:Catalog:0:Currency"] = "USD",
                    ["PayPal:Catalog:0:UnitAmount"] = "2.99",
                    ["PayPal:SubscriptionPlans:0:Tier"] = "premium",
                    ["PayPal:SubscriptionPlans:0:BillingPeriod"] = "monthly",
                    ["PayPal:SubscriptionPlans:0:PlanId"] = "P-PLAN-123"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPayPalPaymentGateway>();
                services.AddSingleton<IPayPalPaymentGateway>(fakeGateway);
            });
        });
    }

    private static async Task<SignupResponse> SignupAsync(HttpClient client, string prefix)
    {
        var signupResp = await client.PostAsJsonAsync(
            "/api/v1/auth/signup",
            new SignupRequest(
                Email: $"{prefix}-{Guid.NewGuid():N}@example.com",
                Password: "Passw0rd!",
                DeviceId: "ios-sim",
                Username: $"{prefix}_{Guid.NewGuid():N}"));

        signupResp.EnsureSuccessStatusCode();
        return (await signupResp.Content.ReadFromJsonAsync<SignupResponse>())!;
    }

    private static async Task SeedStoreItemAsync(WebApplicationFactory<Program> factory, StoreItem item)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var existing = await db.StoreItems.Where(i => i.Sku == item.Sku).ToListAsync();
        db.StoreItems.RemoveRange(existing);
        db.StoreItems.Add(item);
        await db.SaveChangesAsync();
    }

    private static HttpRequestMessage BuildWebhookRequest(object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/store/payments/paypal/webhook")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("PAYPAL-TRANSMISSION-ID", "transmission-1");
        request.Headers.Add("PAYPAL-TRANSMISSION-TIME", DateTimeOffset.UtcNow.ToString("O"));
        request.Headers.Add("PAYPAL-TRANSMISSION-SIG", "signature");
        request.Headers.Add("PAYPAL-CERT-URL", "https://api-m.sandbox.paypal.com/certs/test");
        request.Headers.Add("PAYPAL-AUTH-ALGO", "SHA256withRSA");
        return request;
    }

    private sealed class FakePayPalGateway : IPayPalPaymentGateway
    {
        public bool VerifyWebhookResult { get; set; } = true;

        public PayPalCaptureOrderResult NextCaptureResult { get; set; } =
            new("ORDER-123", "COMPLETED", "CAPTURE-123", null, "USD", 2.99m);

        public PayPalSubscriptionDetails NextSubscriptionDetails { get; set; } =
            new("I-SUB-123", "ACTIVE", "P-PLAN-123", null, null, null, null);

        public Task<bool> VerifyWebhookAsync(PayPalWebhookVerificationRequest request, CancellationToken cancellationToken)
            => Task.FromResult(VerifyWebhookResult);

        public Task<PayPalCreateOrderResult> CreateOrderAsync(PayPalCreateOrderRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PayPalCreateOrderResult("ORDER-123", "CREATED", "https://paypal.test/approve/ORDER-123"));

        public Task<PayPalCaptureOrderResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken)
            => Task.FromResult(NextCaptureResult);

        public Task<PayPalCreateSubscriptionResult> CreateSubscriptionAsync(PayPalCreateSubscriptionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PayPalCreateSubscriptionResult("I-SUB-123", "APPROVAL_PENDING", "https://paypal.test/approve/I-SUB-123"));

        public Task<PayPalSubscriptionDetails> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken)
            => Task.FromResult(NextSubscriptionDetails);

        public Task CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
