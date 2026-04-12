using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Tycoon.Backend.Api.Payments.PayPal;

public sealed class PayPalPaymentGateway : IPayPalPaymentGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PayPalOptions _options;

    public PayPalPaymentGateway(IHttpClientFactory httpClientFactory, IOptions<PayPalOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<bool> VerifyWebhookAsync(
        PayPalWebhookVerificationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(request.WebhookId))
            throw new InvalidOperationException("PayPal WebhookId must be configured.");

        using var http = await CreateAuthorizedClientAsync(cancellationToken);
        using var content = new StringContent(
            JsonSerializer.Serialize(new
            {
                auth_algo = request.AuthAlgo,
                cert_url = request.CertUrl,
                transmission_id = request.TransmissionId,
                transmission_sig = request.TransmissionSig,
                transmission_time = request.TransmissionTime,
                webhook_id = request.WebhookId,
                webhook_event = JsonSerializer.Deserialize<JsonElement>(request.WebhookEventJson)
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await http.PostAsync("/v1/notifications/verify-webhook-signature", content, cancellationToken);
        await EnsureSuccessAsync(response);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return string.Equals(
            doc.RootElement.GetProperty("verification_status").GetString(),
            "SUCCESS",
            StringComparison.OrdinalIgnoreCase);
    }

    public async Task<PayPalCreateOrderResult> CreateOrderAsync(
        PayPalCreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var http = await CreateAuthorizedClientAsync(cancellationToken);
        using var response = await http.PostAsJsonAsync(
            "/v2/checkout/orders",
            new
            {
                intent = "CAPTURE",
                application_context = new
                {
                    return_url = request.ReturnUrl,
                    cancel_url = request.CancelUrl,
                    brand_name = _options.BrandName,
                    user_action = "PAY_NOW"
                },
                purchase_units = new[]
                {
                    new
                    {
                        custom_id = BuildCustomId(request.PlayerId, request.Sku, request.Quantity),
                        description = request.Description,
                        amount = new
                        {
                            currency_code = request.Currency,
                            value = (request.UnitAmount * request.Quantity).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                            breakdown = new
                            {
                                item_total = new
                                {
                                    currency_code = request.Currency,
                                    value = (request.UnitAmount * request.Quantity).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                }
                            }
                        },
                        items = new[]
                        {
                            new
                            {
                                name = request.Name,
                                description = request.Description,
                                quantity = request.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                unit_amount = new
                                {
                                    currency_code = request.Currency,
                                    value = request.UnitAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                                }
                            }
                        }
                    }
                }
            },
            cancellationToken);

        await EnsureSuccessAsync(response);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        var approveUrl = FindLink(root, "approve");
        return new PayPalCreateOrderResult(
            root.GetProperty("id").GetString() ?? string.Empty,
            root.GetProperty("status").GetString() ?? string.Empty,
            approveUrl);
    }

    public async Task<PayPalCaptureOrderResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var http = await CreateAuthorizedClientAsync(cancellationToken);
        using var response = await http.PostAsync($"/v2/checkout/orders/{orderId}/capture", content: null, cancellationToken);
        await EnsureSuccessAsync(response);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        var purchaseUnit = root.GetProperty("purchase_units")[0];
        var customId = purchaseUnit.TryGetProperty("custom_id", out var customIdElement) ? customIdElement.GetString() : null;
        var amount = purchaseUnit.GetProperty("payments").GetProperty("captures")[0].GetProperty("amount");
        var captureId = purchaseUnit.GetProperty("payments").GetProperty("captures")[0].GetProperty("id").GetString();

        return new PayPalCaptureOrderResult(
            root.GetProperty("id").GetString() ?? string.Empty,
            root.GetProperty("status").GetString() ?? string.Empty,
            captureId,
            customId,
            amount.GetProperty("currency_code").GetString(),
            decimal.TryParse(amount.GetProperty("value").GetString(), out var total) ? total : null);
    }

    public async Task<PayPalCreateSubscriptionResult> CreateSubscriptionAsync(
        PayPalCreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var http = await CreateAuthorizedClientAsync(cancellationToken);
        using var response = await http.PostAsJsonAsync(
            "/v1/billing/subscriptions",
            new
            {
                plan_id = request.PlanId,
                custom_id = BuildSubscriptionCustomId(request.PlayerId, request.Tier, request.BillingPeriod),
                application_context = new
                {
                    brand_name = _options.BrandName,
                    return_url = request.ReturnUrl,
                    cancel_url = request.CancelUrl,
                    user_action = "SUBSCRIBE_NOW"
                },
                subscriber = request.PlayerEmail is null ? null : new
                {
                    email_address = request.PlayerEmail
                }
            },
            cancellationToken);

        await EnsureSuccessAsync(response);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        return new PayPalCreateSubscriptionResult(
            root.GetProperty("id").GetString() ?? string.Empty,
            root.GetProperty("status").GetString() ?? string.Empty,
            FindLink(root, "approve"));
    }

    public async Task<PayPalSubscriptionDetails> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var http = await CreateAuthorizedClientAsync(cancellationToken);
        using var response = await http.GetAsync($"/v1/billing/subscriptions/{subscriptionId}", cancellationToken);
        await EnsureSuccessAsync(response);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;

        return new PayPalSubscriptionDetails(
            root.GetProperty("id").GetString() ?? string.Empty,
            root.GetProperty("status").GetString() ?? string.Empty,
            root.TryGetProperty("plan_id", out var planId) ? planId.GetString() : null,
            root.TryGetProperty("custom_id", out var customId) ? customId.GetString() : null,
            TryGetDate(root, "status_update_time"),
            root.TryGetProperty("billing_info", out var billingInfo) ? TryGetDate(billingInfo, "next_billing_time") : null,
            root.TryGetProperty("subscriber", out var subscriber) && subscriber.TryGetProperty("email_address", out var email)
                ? email.GetString()
                : null);
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, string reason, CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var http = await CreateAuthorizedClientAsync(cancellationToken);
        using var response = await http.PostAsJsonAsync(
            $"/v1/billing/subscriptions/{subscriptionId}/cancel",
            new { reason },
            cancellationToken);
        await EnsureSuccessAsync(response);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl);

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("PayPal did not return an access token.");
    }

    private void EnsureConfigured()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("PayPal payments are disabled.");

        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("PayPal client credentials must be configured.");

        if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("PayPal BaseUrl must be a valid absolute URL.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"PayPal request failed with {(int)response.StatusCode}: {body}");
    }

    private static string? FindLink(JsonElement root, string rel)
    {
        if (!root.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var link in links.EnumerateArray())
        {
            if (string.Equals(link.GetProperty("rel").GetString(), rel, StringComparison.OrdinalIgnoreCase))
                return link.GetProperty("href").GetString();
        }

        return null;
    }

    private static DateTimeOffset? TryGetDate(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value)
            && DateTimeOffset.TryParse(value.GetString(), out var parsed)
            ? parsed
            : null;
    }

    private static string BuildCustomId(Guid playerId, string sku, int quantity)
        => $"{playerId:N}|{sku}|{quantity}";

    private static string BuildSubscriptionCustomId(Guid playerId, string tier, string billingPeriod)
        => $"{playerId:N}|{tier}|{billingPeriod}";
}
