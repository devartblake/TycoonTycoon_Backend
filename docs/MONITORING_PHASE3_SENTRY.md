# Phase 3: Sentry Integration & Additional Monitoring Dashboards

**Status:** ✅ Implemented  
**Implementation Date:** 2026-07-04  
**Version:** 1.0

---

## Overview

Phase 3 completes the monitoring stack with centralized error tracking (Sentry) and additional system/application dashboards.

### Components Implemented

1. ✅ **Sentry Error Tracking** - Centralized exception tracking with performance monitoring
2. ✅ **Application Metrics Dashboard** - Request rates, latency percentiles (P50, P95, P99)
3. ✅ **System Resources Dashboard** - Memory, CPU, threads, garbage collection
4. ✅ **Learning Hub Service Fix** - Resolved IAnalyticsEventService missing dependency
5. ✅ **Legacy Sentry Code Disabled** - Consolidated Sentry implementation

---

## Part 1: Sentry Integration

### Architecture

```
Application
  ├─ UseSentry() middleware (Phase 3)
  │  └─ Captures exceptions, performance traces
  │
  ├─ AddSentryMonitoring() service registration
  │  └─ Configures DSN, environment, sampling
  │
  └─ SentryIntegration extensions
     └─ Environment-aware configuration
```

### Configuration

**Environment Variables:**

```bash
# .env files (development, staging, production)
Sentry:Dsn=https://xxxkey@domain.ingest.sentry.io/project-id
```

**Sampling Rates (by environment):**
- Development: 100% (capture all traces)
- Staging: 50% (balance between visibility and cost)
- Production: 10% (minimize quota usage, maintain visibility)

### Features

| Feature | Description | Enabled |
|---------|-------------|---------|
| Exception Capture | Automatic unhandled exception capture | ✅ |
| Performance Tracing | Transaction/request tracing | ✅ |
| Breadcrumbs | Event trail for debugging | ✅ |
| Failed Requests | Auto-capture 5xx errors | ✅ |
| User Context | Track authenticated user | ✅ |
| Custom Tags | Filter errors by service/version | ✅ |
| Source Maps | Match errors to source code | ✅ |
| Request Body | Capture request context (small only) | ✅ |
| Health Check Exclusion | Skip /health, /metrics endpoints | ✅ |

### Usage

**In Program.cs:**

```csharp
// Add Sentry monitoring
builder.AddSentryMonitoring();

// Use Sentry middleware
app.UseSentryMonitoring();
```

**Manual Exception Capture:**

```csharp
using Synaptix.Monitoring.Errors;

try {
    await SomeOperation();
} catch (Exception ex) {
    SentryIntegration.CaptureException(ex, "Operation failed");
}
```

**Add Breadcrumbs:**

```csharp
SentryIntegration.AddBreadcrumb("User logged in", "authentication");
```

**Set User Context:**

```csharp
// After user authenticates
SentryIntegration.SetUserContext(userId, email: userEmail, username: userName);

// On logout
SentryIntegration.ClearUserContext();
```

**Custom Tags:**

```csharp
SentryIntegration.SetTag("deployment", "production");
SentryIntegration.SetTag("region", "us-east-1");
```

### Accessing Sentry Dashboard

1. Navigate to https://sentry.io
2. Select your project (Synaptix)
3. View:
   - **Issues** - All errors grouped by type
   - **Performance** - Transaction duration, slowest endpoints
   - **Releases** - Version tracking and health
   - **Alerts** - Email/Slack notifications

---

## Part 2: Application Metrics Dashboard

**Dashboard:** `application-metrics.json`

### Panels

1. **Request Rate (RPS)** - Requests per second
   - Total RPS, Success RPS, Error RPS
   - Shows traffic trends and error volume

2. **Request Latency Percentiles** - Response time distribution
   - P50, P95, P99 percentiles
   - Identifies performance degradation
   - Threshold lines: Green <200ms, Yellow <500ms, Red >500ms

3. **Average Latency by Bucket** - Detailed latency histogram
   - Shows distribution across latency ranges
   - Identifies slow endpoint patterns

4. **Top 20 Endpoints by RPS** - Table sorted by traffic
   - Lists most-hit endpoints
   - Shows request rate for each

### Metrics Queries

```promql
# Request rate (RPS)
sum(rate(http_requests_total[1m]))

# Latency percentiles
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) * 1000

# Error rate
sum(rate(http_requests_total{status=~"5.."}[1m]))
```

---

## Part 3: System Resources Dashboard

**Dashboard:** `system-resources.json`

### Panels

1. **Memory Usage** - Gauge showing memory as % of 512MB limit
   - Green <70%, Yellow 70-90%, Red >90%

2. **Active Threads** - Current thread count
   - Green <4, Yellow 4-8, Red >8

3. **Memory Usage Over Time** - RSS memory trend
   - Shows memory growth/stability
   - Useful for leak detection

4. **Thread Count Over Time** - Thread count trend
   - Shows thread pool scaling
   - Identifies contention issues

5. **Garbage Collection Activity** - GC rate (collections/sec)
   - Shows GC frequency
   - High GC = memory pressure

### Metrics Queries

```promql
# Memory usage (%)
process_resident_memory_bytes / (536870912) * 100

# Thread count
process_num_threads

# GC rate
rate(dotnet_gc_collections_total[5m])
```

---

## Part 4: Bug Fixes

### 1. Fixed Learning Hub IAnalyticsEventService

**Problem:**
- `LearningHubService` referenced non-existent `IAnalyticsEventService`
- Build failed with "The type or namespace name 'IAnalyticsEventService' could not be found"

**Solution:**
- Made analytics writer optional (`IAnalyticsEventWriter?`)
- Removed call to non-existent `TrackEventAsync()` method
- Added null-safety check for optional writer
- Service still functions without analytics integration

**File:** `Synaptix.Backend.Application/Features/LearningHub/LearningHubService.cs`

### 2. Disabled Legacy Sentry Code

**Problem:**
- Old `Synaptix.Shared.Observability/SentryExtensions.cs` had compilation errors
- Referenced unavailable Sentry types before integration was complete

**Solution:**
- Wrapped entire file with `#if FALSE` conditional compilation
- Added note pointing to new implementation in `Synaptix.Monitoring.SentryIntegration`
- Old code preserved for reference, no conflicts with new implementation

---

## Integration Steps

### Step 1: Ensure Sentry Package is Installed

**Verified in Directory.Packages.props:**
```xml
<PackageVersion Include="Sentry.AspNetCore" Version="4.7.0" />
```

### Step 2: Update Program.cs (Already Done)

```csharp
using Synaptix.Monitoring.Errors;

// Before app.Build()
builder.AddSentryMonitoring();

// After app.Build()
app.UseSentryMonitoring();
```

### Step 3: Configure Environment Variables

Add to `.env`, `.env.staging`, `.env.prod`:

```bash
Sentry:Dsn=https://xxxkey@domain.ingest.sentry.io/project-id
```

### Step 4: Verify Build

```bash
cd Synaptix.Backend.Api
dotnet build
```

### Step 5: Test Sentry

**Manually trigger an error:**

```csharp
// In any controller/service
throw new Exception("Testing Sentry integration");
```

**Verify in Sentry Dashboard:**
1. Check Issues tab for the test error
2. Verify stack trace is captured
3. Confirm error rate shows in graphs

---

## Complete Monitoring Stack

### Layer 1: Data Collection
- ✅ Application Metrics (via OpenTelemetry)
- ✅ Error Rate Tracking (via ErrorRateTracker)
- ✅ Job Metrics (via HangfireMetricsCollector)
- ✅ Exception Capture (via Sentry)
- ✅ Health Checks (via built-in health check endpoints)

### Layer 2: Aggregation & Storage
- ✅ Prometheus (scrapes `/metrics` every 15s)
- ✅ Sentry (stores exceptions with full context)
- ✅ Time-series database (Prometheus TSDB)

### Layer 3: Alerting
- ✅ AlertManager (routes alerts by severity)
- ✅ Slack Integration (sends notifications to channels)
- ✅ Alert Rules (9 total for hangfire, errors, performance)

### Layer 4: Visualization
- ✅ Grafana (5 dashboards)
  1. Health Metrics (auto-provisioned)
  2. Database Metrics (postgres, mongodb, redis)
  3. Hangfire Job Monitoring
  4. Error Rate Monitoring
  5. Application Metrics
  6. System Resources
- ✅ Sentry Dashboard (exceptions and performance)

---

## Monitoring Workflows

### Error Investigation

1. **Alert triggered** (high error rate in AlertManager)
2. **Check Grafana** (Error Rate Monitoring dashboard)
   - Identify which endpoint has errors
   - Check if correlated with resource issue
3. **Check Sentry** (Issues tab)
   - View stack trace
   - See affected users
   - Check breadcrumbs for context
4. **View logs** (if needed)
   - Use endpoint name from Grafana
   - Correlate with error timestamp

### Performance Investigation

1. **Alert triggered** (high latency)
2. **Check Grafana** (Application Metrics dashboard)
   - View P95/P99 latencies
   - Identify slow endpoints
3. **Check System Resources dashboard**
   - Memory/thread usage at time of spike
   - GC activity
4. **Check Sentry Performance tab**
   - Transaction traces
   - Slowest operations

### Resource Bottleneck

1. **Alert triggered** (high memory/threads)
2. **Check System Resources dashboard**
   - Memory trend (leak?)
   - Thread trend (pool exhaustion?)
   - GC activity
3. **Check Application Metrics**
   - Correlation with request volume
   - Identify problematic endpoint
4. **Take action**
   - Scale up if volume-driven
   - Investigate leak if steady climb
   - Optimize if GC-driven

---

## Environment Configuration

### Development

```bash
# .env
Sentry:Dsn=                         # Disabled (optional)
SENTRY_TRACE_SAMPLE_RATE=1.0        # 100% in development
```

### Staging

```bash
# .env.staging
Sentry:Dsn=https://xxxkey@sentry.io/staging-project-id
SENTRY_TRACE_SAMPLE_RATE=0.5        # 50% to control costs
```

### Production

```bash
# .env.production
Sentry:Dsn=https://xxxkey@sentry.io/prod-project-id
SENTRY_TRACE_SAMPLE_RATE=0.1        # 10% to minimize costs
```

---

## Next Steps

1. ✅ **Phase 1 & 2** - Hangfire job monitoring, error rate tracking
2. ✅ **Phase 3** - Sentry integration, additional dashboards
3. → **Phase 4** - Business metrics dashboards
4. → **Phase 5** - SLA tracking and uptime monitoring
5. → **Phase 6** - Incident response automation

---

## Troubleshooting

### Sentry Not Capturing Errors

1. Check DSN is configured: `echo $SENTRY_DSN`
2. Verify environment variables loaded in Program.cs
3. Check Sentry project is active on sentry.io
4. Trigger test error and wait 30 seconds
5. Check Sentry Issues tab

### Dashboard Not Showing Data

1. Verify Prometheus is scraping backend: http://localhost:9090/targets
2. Check metric names exist: http://localhost:9090/api/v1/query?query=http_requests_total
3. Restart Grafana: `docker compose restart grafana`
4. Reload dashboard in browser (Ctrl+F5)

### High Sentry Costs

1. Reduce trace sample rate: `SENTRY_TRACE_SAMPLE_RATE=0.05` (5%)
2. Filter endpoints: add `ShouldLogUrl` filter for less critical paths
3. Set error-only mode for production

---

**Status:** Production Ready  
**Last Updated:** 2026-07-04  
**Maintained By:** DevOps & Backend Teams
