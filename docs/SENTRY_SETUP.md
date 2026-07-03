# Sentry Error Tracking Setup

**Status:** ✅ Implemented  
**Implementation Date:** 2026-07-03

---

## Overview

Sentry provides centralized error tracking and performance monitoring for the Synaptix backend. It captures unhandled exceptions, performance metrics, breadcrumbs, and user context.

### Features

- ✅ Automatic exception capture
- ✅ Performance monitoring (traces)
- ✅ Breadcrumb tracking
- ✅ User identification
- ✅ Request tracking
- ✅ Custom tags and context
- ✅ Source maps support

---

## Setup Instructions

### Step 1: Create Sentry Project

1. Go to https://sentry.io and sign in
2. Click **Projects** → **Create Project**
3. Select **ASP.NET Core** as platform
4. Choose alert settings and create
5. Note your **DSN** (looks like: `https://xxx@xxx.ingest.sentry.io/123456`)

### Step 2: Configure Environment

#### Development

1. Open `docker/.env`
2. Set `SENTRY_DSN` to your project DSN
3. Restart containers:
   ```bash
   docker compose down
   docker compose --profile dev up
   ```

#### Production

1. Update production deployment to include:
   ```bash
   export SENTRY_DSN=https://xxx@xxx.ingest.sentry.io/123456
   ```

### Step 3: Verify Configuration

Test that Sentry is working:

```bash
# Make a request that causes an error
curl http://localhost:5000/api/endpoint-that-errors

# Check Sentry dashboard for the error
# https://sentry.io/organizations/your-org/issues/
```

---

## Configuration Options

### Environment Variables

```bash
# Required: Sentry DSN
SENTRY_DSN=https://xxx@xxx.ingest.sentry.io/123456

# Optional: Environment name (development, staging, production)
SENTRY_ENVIRONMENT=production

# Optional: Application version
APP_VERSION=4.0.0

# Optional: Service name
OBSERVABILITY_SERVICE_NAME=synaptix-backend

# Optional: Cluster name
OBSERVABILITY_CLUSTER=production

# Optional: OTEL endpoint for traces
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

### Application Settings

The configuration is in `appsettings.json` and `appsettings.Production.json`:

```json
{
  "Sentry": {
    "Dsn": "https://xxx@xxx.ingest.sentry.io/123456",
    "Environment": "production",
    "TracesSampleRate": 0.1,
    "CaptureFailedRequests": true
  }
}
```

#### Sampling Rates

- **Development:** 100% (capture all)
- **Production:** 10% (to reduce costs)

Adjust `TracesSampleRate` based on traffic and budget.

---

## What Gets Captured

### Automatically Captured

✅ Unhandled exceptions  
✅ HTTP request/response data  
✅ Stack traces  
✅ User information (if authenticated)  
✅ Browser/device information  
✅ Performance metrics  
✅ Database query errors  

### Excluded

❌ Health check endpoints (`/health`, `/alive`, `/ready`)  
❌ Metrics endpoints (`/metrics`, `/health/metrics`)  
❌ Static files  

### Breadcrumbs

Automatically captured breadcrumbs include:

- HTTP requests
- Console log messages
- Database queries
- Cache operations
- User actions

---

## User Context

When users are authenticated, Sentry captures:

- **ID** - User ID from JWT `sub` claim
- **Email** - User email from claims
- **Username** - User email (fallback)
- **Role** - Custom role field
- **Other** - Additional custom fields

Example:
```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.User = new SentryUser
    {
        Id = userId,
        Email = userEmail,
        Username = userName
    };
});
```

---

## Custom Tags and Context

Add custom information to errors:

```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.SetTag("feature", "leaderboards");
    scope.SetTag("game_id", gameId);
    scope.SetContext("request", new Dictionary<string, object>
    {
        { "player_id", playerId },
        { "score", score },
        { "difficulty", difficulty }
    });
});
```

---

## Alerts and Notifications

### Setting Up Alerts

1. Go to **Sentry Dashboard** → **Alerts**
2. Click **Create Alert Rule**
3. Configure trigger conditions:
   - Error rate threshold
   - New issue alert
   - Regression detection

### Slack Integration

1. Go to **Project Settings** → **Integrations** → **Slack**
2. Click **Add Workspace**
3. Authorize Sentry for your Slack workspace
4. Choose channels for notifications
5. Configure alert rules to post to #alerts-critical

Example alert rule:
```
IF (error.count > 10 AND error.level >= error)
THEN send to #alerts-critical
```

---

## Performance Monitoring

### Transaction Tracking

Transactions track request-to-response performance:

```csharp
// Automatic for ASP.NET Core endpoints
// Custom transaction:
using var transaction = SentrySdk.StartTransaction(
    "background-job",
    "background");

try
{
    // Do work
}
finally
{
    transaction.Finish();
}
```

### Performance Metrics

Captured automatically:

- Request duration (P50, P90, P99)
- Database query performance
- External API call duration
- Custom spans

### Trace Sample Rate

**Development:** 100% (trace everything)  
**Production:** 10% (sample for cost)  

Adjust based on:
- Traffic volume
- Sentry plan quota
- Required visibility level

---

## Troubleshooting

### Sentry Not Capturing Errors

1. Check `SENTRY_DSN` is set correctly
2. Verify network connectivity to Sentry
3. Check application logs for Sentry initialization
4. Ensure exception is actually unhandled (not caught)

### Too Many Events (High Costs)

1. Reduce `TracesSampleRate` in production
2. Use `CaptureFailedRequests` filter
3. Exclude certain endpoints
4. Implement release health tracking

### Missing User Information

1. Ensure JWT contains required claims (`sub`, `email`)
2. Check `SentryUserFactory` implementation
3. Verify authentication is working

### Missing Breadcrumbs

1. Increase `MaxBreadcrumbs` if needed
2. Ensure logging is configured
3. Check ASP.NET Core middleware order

---

## Costs

### Sentry Pricing

Free tier: 5,000 events/month  
Paid tiers: $29-$99+/month

### Optimization Tips

1. Use appropriate sample rates
2. Filter out non-critical errors
3. Implement release health tracking
4. Monitor quota usage in dashboard
5. Archive resolved issues

---

## Security

### Sensitive Data

Sentry automatically filters:
- Passwords
- API keys
- Credit card numbers
- Authentication tokens

For custom sensitive data:

```csharp
SentrySdk.ConfigureScope(scope =>
{
    scope.BeforeCapture = (sentryEvent) =>
    {
        // Remove sensitive data
        foreach (var breadcrumb in sentryEvent.Breadcrumbs)
        {
            if (breadcrumb.Message?.Contains("password") == true)
            {
                breadcrumb.Message = "[redacted]";
            }
        }
        return sentryEvent;
    };
});
```

### Data Retention

- **Free tier:** 90 days
- **Paid tiers:** Configurable (up to unlimited)

Configure in **Project Settings** → **Retention**

---

## Integration with Existing Tools

### With AlertManager

Sentry can trigger AlertManager alerts:

1. Create Sentry webhook in AlertManager
2. Configure in **Project Settings** → **Webhooks**
3. POST to AlertManager `/api/v1/alerts`

### With Grafana

Create Grafana dashboard panel that queries Sentry API:

```
Sentry API: GET https://sentry.io/api/0/projects/org/project/events/
```

### With Slack

Direct Slack integration (see "Alerts and Notifications" above)

---

## Best Practices

1. **Sample appropriately** - 100% in dev, 10% in prod
2. **Tag strategically** - Use tags for common filters
3. **Monitor performance** - Check P99 transaction times
4. **Archive resolved issues** - Keep dashboard clean
5. **Review regularly** - Weekly issue review cadence
6. **Set up alerts** - Critical errors → Slack channel
7. **Track releases** - Link to deployments
8. **Use source maps** - For better stack traces

---

## Next Steps

1. ✅ Implementation complete
2. → Set up Slack alerts in Sentry dashboard
3. → Configure appropriate sample rates
4. → Test with a real error
5. → Monitor initial error volume
6. → Adjust configuration as needed

---

## References

- [Sentry ASP.NET Core Docs](https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/)
- [Sentry Performance Monitoring](https://docs.sentry.io/product/performance/)
- [Sentry Release Tracking](https://docs.sentry.io/product/releases/)
- [Sentry Webhook Integration](https://docs.sentry.io/product/integrations/integration-platform/webhooks/)

---

**Status:** Ready for deployment  
**Last Updated:** 2026-07-03  
**Maintained By:** Backend Team
