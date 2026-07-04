# Hangfire Job & Error Rate Monitoring

**Status:** ✅ Implemented  
**Implementation Date:** 2026-07-04  
**Version:** 1.0

---

## Overview

This document describes the implementation of Hangfire job queue monitoring and HTTP error rate tracking for the Synaptix backend. These services provide real-time visibility into background job health and endpoint error rates.

### Services Implemented

1. **Hangfire Metrics Collector** - Tracks job queue depth, success/failure rates, and processing times
2. **Error Rate Tracker** - Monitors HTTP error rates per endpoint with alerting
3. **Monitoring Endpoints** - REST APIs for accessing metrics
4. **AlertManager Rules** - Automated alerts for job failures and high error rates
5. **Grafana Dashboards** - Visual monitoring of job queue and error trends

---

## Part 1: Hangfire Job Monitoring

### Architecture

The `HangfireMetricsCollector` queries the Hangfire PostgreSQL backend to collect metrics:

```
Hangfire PostgreSQL Database
  ↓
HangfireMetricsCollector
  ↓ (OpenTelemetry Activity)
Prometheus `/metrics` endpoint
  ↓
Prometheus server (scrapes every 15s)
  ↓
Grafana Dashboard + AlertManager Rules
```

### Metrics Collected

| Metric | Description | Unit |
|--------|-------------|------|
| `hangfire_jobs_enqueued` | Jobs waiting to be processed | count |
| `hangfire_jobs_scheduled` | Jobs scheduled for future execution | count |
| `hangfire_jobs_processing` | Jobs currently being processed | count |
| `hangfire_jobs_succeeded` | Successfully completed jobs | count |
| `hangfire_jobs_failed` | Failed jobs | count |
| `hangfire_jobs_deleted` | Deleted jobs | count |
| `hangfire_job_duration_seconds` | Job execution duration (histogram) | seconds |
| `hangfire_servers_active` | Active Hangfire server instances | count |

### Usage

```csharp
// Inject the collector
public class MyService
{
    private readonly HangfireMetricsCollector _collector;
    
    public MyService(HangfireMetricsCollector collector)
    {
        _collector = collector;
    }
    
    // Get current metrics
    public void CheckJobHealth()
    {
        var metrics = _collector.GetCurrentMetrics();
        Console.WriteLine($"Queue depth: {metrics.QueueDepth}");
        Console.WriteLine($"Failed jobs: {metrics.FailedCount}");
    }
    
    // Record job execution (called by Hangfire filter)
    public void OnJobCompleted(string jobId, string jobType, TimeSpan duration, bool success)
    {
        var status = success 
            ? JobExecutionStatus.Succeeded 
            : JobExecutionStatus.Failed;
        
        _collector.RecordJobExecution(jobId, jobType, status, duration);
    }
}
```

### Monitoring Endpoints

All endpoints are exposed at `/monitoring` and return JSON:

#### Get Job Metrics
```bash
GET /monitoring/jobs/metrics
```

Response:
```json
{
  "timestamp": "2026-07-04T10:30:45.123Z",
  "enqueuedCount": 42,
  "scheduledCount": 15,
  "processingCount": 3,
  "succeededCount": 52341,
  "failedCount": 8,
  "queueDepth": 57
}
```

---

## Part 2: Error Rate Tracking

### Architecture

The `ErrorRateTracker` automatically monitors all HTTP requests:

```
HTTP Request/Response
  ↓
ErrorTrackingMiddleware
  ↓ (records status code + duration)
ErrorRateTracker
  ↓ (OpenTelemetry Activity for errors)
Prometheus metrics
  ↓
Grafana Dashboard + AlertManager Rules
```

### Features

- **Real-time Error Rate Calculation** - Tracks errors in rolling 1-minute windows
- **Per-Endpoint Metrics** - Separate error tracking for each endpoint
- **Automatic Alerts** - Triggers when error rates exceed thresholds
- **OpenTelemetry Integration** - Error events recorded as activities for tracing

### Thresholds

| Alert | Threshold | Severity | Duration |
|-------|-----------|----------|----------|
| High Error Rate | 5% 5xx errors | warning | 2 min |
| Critical Error Rate | 25% 5xx errors | critical | 1 min |
| Error Spike | >1 5xx errors/sec | critical | 1 min |
| Endpoint Error Rate | 10% total errors | warning | 2 min |

### Usage

```csharp
// Inject the tracker
public class MyController : ControllerBase
{
    private readonly ErrorRateTracker _errorTracker;
    
    public MyController(ErrorRateTracker errorTracker)
    {
        _errorTracker = errorTracker;
    }
    
    // Get error metrics for specific endpoint
    public IActionResult GetMetrics()
    {
        var endpoint = "GET /api/users";
        var metrics = _errorTracker.GetEndpointMetrics(endpoint);
        
        if (metrics?.HighErrorRate ?? false)
        {
            // Log alert or take action
            Console.WriteLine($"⚠️  High error rate on {endpoint}: {metrics.ErrorRate:P}");
        }
        
        return Ok(metrics);
    }
}
```

### Monitoring Endpoints

#### Get Error Summary
```bash
GET /monitoring/errors/summary
```

Response:
```json
{
  "totalEndpoints": 45,
  "totalRequests": 125342,
  "totalErrors": 423,
  "averageErrorRate": 0.00337,
  "highErrorRateEndpoints": ["POST /api/orders", "GET /api/inventory"],
  "maxErrorRate": 0.0845,
  "lastErrorTime": "2026-07-04T10:30:12.456Z"
}
```

#### Get Errors by Endpoint
```bash
GET /monitoring/errors/by-endpoint
```

Response:
```json
[
  {
    "endpoint": "POST /api/orders",
    "totalRequests": 1200,
    "errorCount": 101,
    "errorRate": 0.0842,
    "averageDuration": "00:00:00.2340000",
    "lastErrorTime": "2026-07-04T10:30:12.456Z",
    "lastErrorStatus": 500,
    "highErrorRate": true
  },
  ...
]
```

#### Get High Error Rate Endpoints
```bash
GET /monitoring/errors/high-rate
```

Response:
```json
{
  "highErrorRateEndpoints": [
    {
      "endpoint": "POST /api/orders",
      "errorRate": 0.0842
    }
  ],
  "count": 1,
  "timestamp": "2026-07-04T10:30:45.123Z"
}
```

---

## Part 3: AlertManager Integration

### Alert Rules

Alert rules are defined in `docker/monitoring/prometheus/rules/alert-rules.yml`:

#### Hangfire Alerts

**1. High Job Queue Depth** (Warning)
- Condition: Queue depth > 1,000 jobs
- Duration: 5 minutes
- Severity: warning
- Action: Investigate slow job processing

**2. Critical Job Queue Depth** (Critical)
- Condition: Queue depth > 5,000 jobs
- Duration: 2 minutes
- Severity: critical
- Action: Page on-call, investigate immediately

**3. High Job Failure Rate** (Critical)
- Condition: Failure rate > 5%
- Duration: 2 minutes
- Severity: critical
- Action: Investigate job logs, check database connectivity

**4. Job Failure Rate Warning** (Warning)
- Condition: Failure rate > 1%
- Duration: 5 minutes
- Severity: warning
- Action: Monitor, investigate if continues

**5. Long Job Processing Time** (Warning)
- Condition: P95 duration > 60 seconds
- Duration: 5 minutes
- Severity: warning
- Action: Optimize slow jobs, check resource contention

#### Error Rate Alerts

**1. Endpoint High Error Rate** (Warning)
- Condition: >10% error rate on specific endpoint
- Duration: 2 minutes
- Severity: warning
- Action: Check logs, verify database connectivity

**2. Endpoint Critical Error Rate** (Critical)
- Condition: >25% 5xx error rate on endpoint
- Duration: 1 minute
- Severity: critical
- Action: Page on-call, initiate incident response

**3. Server Error Spike** (Critical)
- Condition: >1 5xx error per second
- Duration: 1 minute
- Severity: critical
- Action: Immediate investigation, possible rollback

### Slack Notifications

AlertManager sends alerts to Slack:

**Hangfire Alerts** → `#alerts-critical` (if severity=critical) or `#alerts-warning`
**Error Rate Alerts** → `#alerts-critical` (if severity=critical) or `#alerts-warning`

Example Slack message:
```
🚨 Critical: High Hangfire Job Queue Depth
Service: Hangfire
Queue depth is 5,247 jobs (threshold: 5,000)
Started: 2026-07-04 10:30:15
Duration: 2m 15s
Action: Check job processing servers, investigate bottlenecks
```

---

## Part 4: Grafana Dashboards

### Dashboard 1: Hangfire Job Monitoring

**File:** `docker/monitoring/grafana/provisioning/dashboards/hangfire-jobs.json`

**Panels:**

1. **Queue Depth & Processing** (Line Chart)
   - X: Time
   - Y: Number of jobs
   - Series: Queue Depth, Processing
   - Updates: Every 30s

2. **Failed Jobs Count** (Gauge)
   - Shows current failed job count
   - Red: >100, Yellow: >50, Green: <50

3. **Job Success/Failure Rates** (Stacked Area)
   - X: Time
   - Y: Jobs/second
   - Shows success rate vs failure rate over time

4. **Job Processing Duration** (Line Chart)
   - P95 and P99 percentiles
   - Threshold: 60s (warning line)

5. **Active Hangfire Servers** (Status)
   - Green if >0 servers, Red if 0

### Dashboard 2: Error Rate Monitoring

**File:** `docker/monitoring/grafana/provisioning/dashboards/error-rates.json`

**Panels:**

1. **Error Rate by Endpoint** (Line Chart)
   - X: Time
   - Y: Error rate (%)
   - Threshold zones: Green <1%, Yellow <5%, Red >5%

2. **Request Status Distribution** (Pie Chart)
   - 2xx Success (blue)
   - 3xx Redirects (green)
   - 4xx Client Errors (yellow)
   - 5xx Server Errors (red)

3. **Server Errors Per Second** (Bar Chart)
   - Real-time spike detection
   - Threshold: 1 error/sec

4. **Top 10 Endpoints by Error Rate** (Table)
   - Sortable by error rate
   - Shows last error time and status code

---

## Setup & Configuration

### Prerequisites

- Backend API with monitoring services registered
- Prometheus running and configured to scrape backend `/metrics`
- AlertManager configured with Slack webhook

### Step 1: Register Monitoring Services

In `Program.cs`:

```csharp
// Add monitoring
builder.Services.AddMonitoring();

// Add error tracking middleware
app.UseErrorTracking();

// Map monitoring endpoints
app.MapMonitoringEndpoints();

// Configure Hangfire with job monitoring
GlobalConfiguration.Configuration
    .UsePostgreSqlStorage(hangfireDb)
    .AddJobMonitoring();
```

### Step 2: Verify Metrics Endpoint

```bash
curl http://localhost:5000/metrics | grep hangfire
# Should output Hangfire metrics
```

### Step 3: Check Grafana Dashboards

1. Go to http://localhost:3000 (Grafana)
2. Navigate to **Dashboards** → **Browse**
3. You should see:
   - "Hangfire Job Monitoring"
   - "Error Rate Monitoring"

### Step 4: Test Alerts

Create a test failure:

```bash
# Simulate high error rate
for i in {1..100}; do curl http://localhost:5000/api/test-error & done

# Check AlertManager
curl http://localhost:9093/api/v1/alerts
```

---

## Environment Variables

```bash
# Enable/disable monitoring (optional, default: true)
MONITORING_ENABLED=true

# Error rate alert threshold (5% = 0.05)
ERROR_RATE_THRESHOLD=0.05

# Hangfire alert thresholds
HANGFIRE_QUEUE_DEPTH_WARNING=1000
HANGFIRE_QUEUE_DEPTH_CRITICAL=5000
HANGFIRE_JOB_FAILURE_RATE_WARNING=0.01
HANGFIRE_JOB_FAILURE_RATE_CRITICAL=0.05
```

---

## Troubleshooting

### Metrics Not Appearing in Prometheus

1. Check backend is running and metrics endpoint is accessible:
   ```bash
   curl http://localhost:5000/metrics | head -20
   ```

2. Verify Prometheus is scraping:
   ```bash
   curl http://localhost:9090/api/v1/query?query=hangfire_jobs_enqueued
   ```

3. Check `docker/monitoring/prometheus/prometheus.yml` has backend-api target

### Alerts Not Firing

1. Verify AlertManager has alert rules loaded:
   ```bash
   curl http://localhost:9093/api/v1/alerts
   ```

2. Check alert rule syntax in `alert-rules.yml`:
   ```bash
   docker compose exec prometheus cat /etc/prometheus/rules/alert-rules.yml
   ```

3. Test rule manually in Prometheus UI:
   - Go to http://localhost:9090
   - Alerts tab
   - Check rule status

### No Slack Notifications

1. Verify `SLACK_WEBHOOK_URL` is set:
   ```bash
   echo $SLACK_WEBHOOK_URL
   ```

2. Test webhook manually:
   ```bash
   curl -X POST $SLACK_WEBHOOK_URL -H 'Content-Type: application/json' \
     -d '{"text": "Test alert"}'
   ```

3. Check AlertManager logs:
   ```bash
   docker compose logs alertmanager
   ```

---

## Next Steps

1. ✅ Hangfire job monitoring implemented
2. ✅ Error rate tracking implemented
3. ✅ AlertManager rules configured
4. ✅ Grafana dashboards created
5. → Add custom application metrics (QPS, latency percentiles, business metrics)
6. → Integrate Sentry for exception tracking
7. → Set up dashboards for SLA tracking

---

## Performance Notes

- **ErrorRateTracker** uses in-memory rolling window (minimal overhead)
- **HangfireMetricsCollector** queries PostgreSQL (1-2ms per query)
- **Prometheus scrape interval** set to 15s
- **Dashboard refresh** set to 30s for reasonable freshness
- **No significant memory overhead** (<50MB additional)

---

**Status:** Production Ready  
**Last Updated:** 2026-07-04  
**Maintained By:** DevOps Team
