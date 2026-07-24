using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synaptix.Backend.Api.Payments.Stripe;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Store;

public sealed class StoreSubscriptionEndpointTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public StoreSubscriptionEndpointTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubscriptionStatus_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/store/subscription/status/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateSubscription_ThenStatus_ReturnsActiveSubscription()
    {
        using var client = _factory.CreateClient();

        var signupResp = await client.PostAsJsonAsync("/api/v1/auth/signup", new SignupRequest(
            Email: $"sub-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"sub_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        var playerId = Guid.Parse(signup!.UserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        var activate = await client.PostAsJsonAsync("/api/v1/store/subscription/activate", new ActivateSubscriptionRequest(
            PlayerId: playerId,
            Tier: "premium",
            BillingPeriod: "monthly",
            ExternalTransactionId: "sub-tx-001"));
        activate.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await client.GetFromJsonAsync<SubscriptionStatusDto>($"/api/v1/store/subscription/status/{playerId}");
        status.Should().NotBeNull();
        status!.IsActive.Should().BeTrue();
        status.Tier.Should().Be("premium");
        status.BillingPeriod.Should().Be("monthly");
    }

    [Fact]
    public async Task SubscriptionCheckoutSession_WithAuthenticatedOwner_ReturnsStripeSession()
    {
        var fakeGateway = new FakeStripePaymentGateway();
        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "sub-checkout");
        var playerId = Guid.Parse(signup.UserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        await StoreTestSupport.EnableStorePurchasesAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/v1/store/subscription/checkout/session",
            new CreateStripeSubscriptionCheckoutSessionRequest(playerId, "premium", "monthly"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateStripeSubscriptionCheckoutSessionResponse>();
        body.Should().NotBeNull();
        body!.SessionId.Should().Be("cs_sub_test_123");
        body.PriceId.Should().Be("price_premium_monthly");

        fakeGateway.LastSubscriptionCheckoutRequest.Should().NotBeNull();
        fakeGateway.LastSubscriptionCheckoutRequest!.PlayerId.Should().Be(playerId);
        fakeGateway.LastSubscriptionCheckoutRequest.Tier.Should().Be("premium");
        fakeGateway.LastSubscriptionCheckoutRequest.BillingPeriod.Should().Be("monthly");
    }

    [Fact]
    public async Task SubscriptionWebhook_ThenPortalSession_ReturnsCustomerPortalUrl()
    {
        var fakeGateway = new FakeStripePaymentGateway
        {
            NextWebhookEvent = new StripeWebhookEvent(
                "evt_sub_checkout",
                "checkout.session.completed",
                new StripeWebhookCheckoutCompletedData(
                    "cs_sub_completed_123",
                    "subscription",
                    "paid",
                    "usd",
                    999,
                    "sub@example.com",
                    null,
                    "sub_123",
                    "cus_123",
                    new Dictionary<string, string>()),
                null)
        };

        using var factory = CreateFactory(fakeGateway);
        using var client = factory.CreateClient();

        var signup = await SignupAsync(client, "sub-portal");
        var playerId = Guid.Parse(signup.UserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        fakeGateway.NextWebhookEvent = fakeGateway.NextWebhookEvent with
        {
            CheckoutCompleted = fakeGateway.NextWebhookEvent.CheckoutCompleted! with
            {
                Metadata = new Dictionary<string, string>
                {
                    ["player_id"] = playerId.ToString(),
                    ["tier"] = "premium",
                    ["billing_period"] = "monthly"
                }
            }
        };

        using var webhookContent = new StringContent(
            JsonSerializer.Serialize(new { id = "evt_sub_checkout", type = "checkout.session.completed" }),
            Encoding.UTF8,
            "application/json");

        var webhook = await client.PostAsync("/api/v1/store/payments/webhook", webhookContent);
        webhook.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await client.GetFromJsonAsync<SubscriptionStatusDto>($"/api/v1/store/subscription/status/{playerId}");
        status.Should().NotBeNull();
        status!.IsActive.Should().BeTrue();
        status.StripeSubscriptionId.Should().Be("sub_123");
        status.StripeCustomerId.Should().Be("cus_123");

        var portal = await client.PostAsJsonAsync(
            "/api/v1/store/subscription/portal/session",
            new CreateStripeBillingPortalSessionRequest(playerId));

        portal.StatusCode.Should().Be(HttpStatusCode.OK);
        var portalBody = await portal.Content.ReadFromJsonAsync<CreateStripeBillingPortalSessionResponse>();
        portalBody.Should().NotBeNull();
        portalBody!.Url.Should().Be("https://billing.stripe.test/session/bps_test_123");

        fakeGateway.LastPortalRequest.Should().NotBeNull();
        fakeGateway.LastPortalRequest!.CustomerId.Should().Be("cus_123");
    }

    private WebApplicationFactory<Program> CreateFactory(FakeStripePaymentGateway fakeGateway)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Stripe:Enabled"] = "true",
                    ["Stripe:SecretKey"] = "sk_test_123",
                    ["Stripe:PublishableKey"] = "pk_test_123",
                    ["Stripe:WebhookSecret"] = "whsec_test_123",
                    ["Stripe:SuccessUrl"] = "https://localhost:3000/store/success?session_id={CHECKOUT_SESSION_ID}",
                    ["Stripe:CancelUrl"] = "https://localhost:3000/store/cancel",
                    ["Stripe:PortalReturnUrl"] = "https://localhost:3000/store/subscription",
                    ["Stripe:SubscriptionPlans:0:Tier"] = "premium",
                    ["Stripe:SubscriptionPlans:0:BillingPeriod"] = "monthly",
                    ["Stripe:SubscriptionPlans:0:PriceId"] = "price_premium_monthly",
                    ["Stripe:SubscriptionPlans:1:Tier"] = "elite",
                    ["Stripe:SubscriptionPlans:1:BillingPeriod"] = "monthly",
                    ["Stripe:SubscriptionPlans:1:PriceId"] = "price_elite_monthly"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IStripePaymentGateway>();
                services.AddSingleton<IStripePaymentGateway>(fakeGateway);
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
        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        return signup!;
    }

    private sealed class FakeStripePaymentGateway : IStripePaymentGateway
    {
        public StripeSubscriptionCheckoutSessionCreateRequest? LastSubscriptionCheckoutRequest { get; private set; }

        public StripeBillingPortalSessionCreateRequest? LastPortalRequest { get; private set; }

        public StripeWebhookEvent NextWebhookEvent { get; set; } =
            new("evt_default", "checkout.session.completed", null, null);

        public Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
            StripeCheckoutSessionCreateRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StripeCheckoutSessionResult> CreateSubscriptionCheckoutSessionAsync(
            StripeSubscriptionCheckoutSessionCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastSubscriptionCheckoutRequest = request;
            return Task.FromResult(new StripeCheckoutSessionResult(
                "cs_sub_test_123",
                "https://checkout.stripe.test/session/cs_sub_test_123"));
        }

        public Task<StripeBillingPortalSessionResult> CreateBillingPortalSessionAsync(
            StripeBillingPortalSessionCreateRequest request,
            CancellationToken cancellationToken)
        {
            LastPortalRequest = request;
            return Task.FromResult(new StripeBillingPortalSessionResult(
                "bps_test_123",
                "https://billing.stripe.test/session/bps_test_123"));
        }

        public StripeWebhookEvent ParseWebhook(string payload, string? signatureHeader)
        {
            return NextWebhookEvent;
        }

        public Task<StripeSessionStatusResult> GetCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<StripeRefundResult> RefundPaymentIntentAsync(string paymentIntentId, long? amount, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
