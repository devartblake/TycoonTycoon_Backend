namespace Synaptix.OperatorDashboard.Services;

/// <summary>
/// Scoped per Blazor Server circuit. Holds the current user's Bearer token so every
/// AdminApiClient instance within the same circuit shares the same token automatically.
///
/// The root cause this solves: AddHttpClient registers AdminApiClient as transient, meaning
/// Dashboard.razor's @inject AdminApiClient and AdminAuthService's constructor injection
/// receive two different instances. SetToken() on one instance never affects the other.
/// This store is injected into BearerTokenHandler (which runs on every request) and into
/// AdminAuthService (which writes the token after login/refresh), guaranteeing both see
/// the same value for the lifetime of the circuit.
/// </summary>
public sealed class BearerTokenStore
{
    public string? AccessToken { get; set; }
}
