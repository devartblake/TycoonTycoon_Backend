# Post-Launch Monitoring & Observability Setup

**Launch Date:** 2026-07-01 (v4.0.0)  
**Monitoring Period:** 24 hours (first critical period)  
**Status:** 🟢 READY FOR SETUP

---

## Overview

Comprehensive monitoring strategy for post-launch stability verification. This ensures we catch issues early and can respond quickly.

---

## Part 1: Key Metrics to Monitor

### Backend API Metrics

#### Response Time (SLI Target: P95 < 200ms)
```
Endpoint: GET /leaderboards/arcade/{gameId}/{difficulty}
- Baseline (uncached): ~200ms
- With cache hit: ~20ms
- Alert threshold: P95 > 300ms
- Critical threshold: P95 > 500ms

Endpoint: POST /leaderboards/arcade/submit
- Target: < 100ms
- Alert threshold: > 150ms
```

#### Error Rate (SLI Target: < 0.1%)
```
4xx errors (client errors):
- Alert if > 5% of requests
- Critical if > 10%

5xx errors (server errors):
- Alert if > 1 of any type (each)
- Critical if > 5 total

Specific 5xx errors to monitor:
- 500 Internal Server Error
- 503 Service Unavailable
- 502 Bad Gateway
```

#### Request Volume
```
Leaderboard queries: Track per game/difficulty
Score submissions: Track per hour
Quiz reviews opened: Track per hour
Learn more clicks: Track per hour (after Phase 2)
```

#### Database Metrics
```
Connection pool usage: Alert if > 75%
Slow queries (> 500ms): Alert on any
Query errors: Alert on any connection failures
Table sizes: Track arcade_scores growth
```

### Frontend App Metrics

#### Crash Rate (Target: < 0.01%)
```
Android:
- Monitor via Google Play Console
- Alert if crash rate > 0.05%
- Critical if > 0.1%

iOS:
- Monitor via App Store Connect
- Alert if crash rate > 0.05%
- Critical if > 0.1%

Web:
- Monitor via Sentry/Rollbar
- Alert on any uncaught exceptions
- Track error rate

Windows:
- Monitor via error reporting
- Track application crashes
```

#### Network Errors
```
Network timeouts: Alert if > 5% of requests
Quiz review load failures: Alert on any
Leaderboard load failures: Alert if > 1%
Offline fallback usage: Track percentage
```

#### User Engagement Metrics
```
Quiz review feature usage:
- % of players who use it
- Average time spent in review screen
- Learn more click-through rate

Leaderboard feature usage:
- % of players viewing leaderboard
- Local vs global view ratio
- Time spent on leaderboard
```

---

## Part 2: Monitoring Tools Setup

### Option A: Cloud-Based Monitoring (Recommended)

#### Datadog Setup
```
1. Create Datadog organization
2. Install Datadog agents:
   - Linux agent on backend servers
   - APM agent in .NET application
3. Create dashboards:
   - API performance dashboard
   - Error rate dashboard
   - Database performance dashboard
4. Set up alerts:
   - Response time > 300ms
   - Error rate > 0.1%
   - Crash rate > 0.01%
```

#### Sentry Setup (Error Tracking)
```
1. Create Sentry project
2. Add to Flutter app:
   await Sentry.init(
     'your-sentry-dsn',
     tracesSampleRate: 1.0,
   );
3. Set up alerts:
   - First crash of any type
   - Error rate threshold
   - Release tracking
```

#### Google Play Console (Android)
```
1. Verify APK signed correctly
2. Set up release notes
3. Monitor:
   - Crash rate
   - ANR (Application Not Responding)
   - User reviews and ratings
4. Set up alerts:
   - Crash spike notifications
   - Low rating alerts
```

#### App Store Connect (iOS)
```
1. Submit IPA for review
2. Monitor:
   - Crash reports
   - Performance metrics
   - User reviews
3. Set up TestFlight (if applicable):
   - Internal testing group
   - Crash reporting
```

### Option B: Self-Hosted Monitoring

#### Prometheus + Grafana Stack
```yaml
# docker-compose.yml for monitoring
version: '3.8'
services:
  prometheus:
    image: prom/prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    depends_on:
      - prometheus

  node-exporter:
    image: prom/node-exporter
    ports:
      - "9100:9100"
```

#### Prometheus Configuration
```yaml
# prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

alerting:
  alertmanagers:
    - static_configs:
        - targets:
            - localhost:9093

scrape_configs:
  - job_name: 'api'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
```

#### Grafana Dashboards
- API Performance Dashboard (response time, error rate, requests/sec)
- Database Dashboard (connections, slow queries, table sizes)
- Application Dashboard (crashes, user engagement, feature usage)

---

## Part 3: Monitoring Dashboard Setup

### Real-Time Dashboard

Create a simple web dashboard showing:
- 🟢 System Status (Healthy/Warning/Critical)
- API Response Time (P95 graph)
- Error Rate (line chart)
- Active Users (counter)
- Feature Adoption (pie chart)

```dart
// lib/admin/screens/monitoring_dashboard_screen.dart
class MonitoringDashboardScreen extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final statusAsync = ref.watch(systemStatusProvider);
    
    return Scaffold(
      appBar: AppBar(title: Text('System Monitoring')),
      body: statusAsync.when(
        loading: () => Center(child: CircularProgressIndicator()),
        error: (e, _) => ErrorScreen(error: e),
        data: (status) => SingleChildScrollView(
          child: Padding(
            padding: EdgeInsets.all(16),
            child: Column(
              children: [
                // System Status Card
                _buildStatusCard(status),
                SizedBox(height: 16),
                
                // Metrics Row
                Row(
                  children: [
                    Expanded(child: _buildMetricCard('Response Time', status.responseTimeP95, 'ms')),
                    SizedBox(width: 8),
                    Expanded(child: _buildMetricCard('Error Rate', status.errorRate, '%')),
                  ],
                ),
                SizedBox(height: 16),
                
                // Alerts
                if (status.alerts.isNotEmpty)
                  _buildAlertsCard(status.alerts),
              ],
            ),
          ),
        ),
      ),
    );
  }
  
  Widget _buildStatusCard(SystemStatus status) {
    final color = status.isHealthy ? Colors.green : Colors.red;
    return Card(
      color: color.withOpacity(0.1),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Row(
          children: [
            Container(
              width: 20,
              height: 20,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: color,
              ),
            ),
            SizedBox(width: 16),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('System Status', style: Theme.of(context).textTheme.labelLarge),
                Text(status.isHealthy ? 'All Systems Operational' : 'Issues Detected'),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
```

---

## Part 4: Alert Configuration

### Alert Rules

#### Critical Alerts (Immediate Notification)
```
1. API Response Time P95 > 500ms
   - Notification: Slack + Email
   - Action: Page on-call engineer

2. Error Rate > 1%
   - Notification: Slack + SMS
   - Action: Page on-call engineer

3. Database Connection Pool > 90%
   - Notification: Slack + Email
   - Action: Investigate connection leaks

4. Crash Rate (Android/iOS) > 0.1%
   - Notification: Slack + Email + App Store
   - Action: Prepare rollback

5. Quiz Review Feature Failure Rate > 5%
   - Notification: Slack + Email
   - Action: Investigate feature
```

#### Warning Alerts (Batch Notification)
```
1. API Response Time P95 > 300ms
   - Frequency: Every 15 minutes
   - Action: Monitor trend

2. Error Rate > 0.1%
   - Frequency: Every 15 minutes
   - Action: Monitor trend

3. Cache Hit Rate < 80%
   - Frequency: Every hour
   - Action: Investigate cache issues

4. Slow Queries (> 500ms) Detected
   - Frequency: Daily summary
   - Action: Review slow query log
```

### Slack Integration

```bash
# Create Slack webhook for alerts
# Go to https://api.slack.com/apps
# Create new app
# Enable Incoming Webhooks
# Create webhook URL: https://hooks.slack.com/services/YOUR/WEBHOOK/URL

# Test alert
curl -X POST -H 'Content-type: application/json' \
  --data '{"text":"Test alert"}' \
  https://hooks.slack.com/services/YOUR/WEBHOOK/URL
```

---

## Part 5: Health Check Endpoints

### Backend Health Checks

```csharp
// Create health check endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                x => x.Key,
                x => new { status = x.Value.Status.ToString() }
            ),
            timestamp = DateTime.UtcNow
        }));
    }
});

// Add checks
services.AddHealthChecks()
    .AddDbContextCheck<AppDb>("database")
    .AddCheck("api", () => HealthCheckResult.Healthy("API is running"))
    .AddCheck("memory", () =>
    {
        var memory = GC.GetTotalMemory(false) / 1024 / 1024; // MB
        return memory < 500 ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded();
    });
```

### Frontend Health Checks (Periodic)

```dart
// Check API connectivity
Future<bool> checkApiHealth() async {
  try {
    final response = await http.get(Uri.parse('$apiBaseUrl/health'))
        .timeout(Duration(seconds: 5));
    return response.statusCode == 200;
  } catch (e) {
    return false;
  }
}
```

---

## Part 6: Monitoring Checklist (First 24 Hours)

### Immediate Post-Launch (0-2 hours)

- [ ] **Hour 0:** API responding normally
  ```bash
  curl https://api.synaptix.com/health
  # Expected: 200 OK, {"status":"Healthy"}
  ```

- [ ] **Hour 0:** Database working
  ```bash
  curl https://api.synaptix.com/leaderboards/arcade/patternSprint/normal?page=1
  # Expected: 200 OK with empty leaderboard
  ```

- [ ] **Hour 1:** App store submissions appearing
  - [ ] Android Play Store showing build
  - [ ] iOS TestFlight shows build
  - [ ] Web site deployed

- [ ] **Hour 2:** First users testing
  - [ ] Monitor real-time request logs
  - [ ] Watch for errors in Sentry
  - [ ] Monitor app crash reports

### Early Monitoring (2-6 hours)

- [ ] **Hourly:** Check API response times (P95 < 200ms)
  ```
  Expected: < 200ms baseline
  Alert if: > 300ms
  ```

- [ ] **Hourly:** Check error rate (< 0.1%)
  ```
  Expected: 0 errors ideal
  Alert if: > 0.5%
  ```

- [ ] **Hourly:** Check user engagement
  ```
  Track: Quiz reviews opened, leaderboard views
  Watch for: Unexpected spikes or drops
  ```

- [ ] **Every 2 hours:** Check app stores
  ```
  Monitor: User reviews, crash reports, ratings
  Watch for: Negative trends
  ```

### Continuous Monitoring (6-24 hours)

- [ ] **Every 15 minutes:** Automated health checks
  ```
  Check: API health, database connectivity
  Alert on: Any failures
  ```

- [ ] **Every hour:** Performance dashboard review
  ```
  Review: Response times, error rates, user metrics
  Check for: Anomalies or trends
  ```

- [ ] **Every 6 hours:** Log analysis
  ```
  Check: Error patterns, slow queries, timeouts
  Investigate: Any new error types
  ```

- [ ] **Continuous:** Slack monitoring
  ```
  Watch: #monitoring channel for alerts
  Respond: To any critical alerts immediately
  ```

---

## Part 7: Escalation Procedures

### Response Time Degradation

**Alert:** P95 response time > 300ms

**Response Steps:**
1. Check CPU/Memory on backend
2. Check database query performance
3. Check network connectivity
4. Review recent code changes
5. If persists > 15 minutes: Investigate cache
6. If persists > 30 minutes: Consider rollback

### Error Rate Spike

**Alert:** Error rate > 0.1%

**Response Steps:**
1. Identify which endpoint has errors
2. Check error logs in backend
3. Check database connectivity
4. Review for any recent changes
5. If database errors: Check database logs
6. If timeout errors: Check network
7. If > 1% error rate: Prepare rollback

### Crash Rate Spike

**Alert:** Crash rate > 0.05%

**Response Steps:**
1. Identify crash type (get stack trace)
2. Check if it's Quiz Review or Leaderboard feature
3. Check app store for user feedback
4. If quiz review crashes: Disable feature flag
5. If leaderboard crashes: Revert to local-only
6. If > 0.1%: Initiate rollback

### High Database Load

**Alert:** Connection pool > 75%

**Response Steps:**
1. Check for long-running queries
2. Kill slow/idle queries if safe
3. Check for connection leaks in code
4. Restart database if necessary
5. If connection pool fills: Scale database or rollback

---

## Part 8: Post-Monitoring Report (After 24 hours)

Create a summary report with:

```markdown
# Post-Launch Monitoring Report (v4.0.0)
**Period:** July 1, 00:00 - July 2, 00:00 UTC

## System Health
- Uptime: 99.95%
- Avg Response Time: 125ms (P95: 180ms)
- Error Rate: 0.02%
- Crash Rate: 0.005%

## Alerts Triggered
- [List of alerts]
- [Actions taken]
- [Resolutions]

## User Metrics
- Active Users: [N]
- Quiz Reviews Opened: [N]
- Leaderboard Views: [N]
- Learn More Clicks: [N after Phase 2]

## Issues Found & Fixed
1. [Issue 1: Description]
   - Impact: [Severity]
   - Fix: [What was done]
   - Time to Fix: [Duration]

## Performance Findings
- Cache hit rate: [N]%
- Database query times: [Avg ms]
- API response times: [Trend]

## Recommendations
- [Recommendation 1]
- [Recommendation 2]

## Approval
- [ ] Engineering Lead
- [ ] Operations Lead
- [ ] Product Manager
```

---

## Monitoring Success Criteria

**Post-launch monitoring is successful when:**

✅ Uptime ≥ 99.9% in first 24 hours  
✅ API response time P95 < 200ms  
✅ Error rate < 0.1%  
✅ Crash rate < 0.01%  
✅ All critical alerts responded to < 15 minutes  
✅ No critical bugs requiring rollback  
✅ User feedback positive  

---

## Contacts & Escalation

| Role | Name | Slack | Phone | On-Call |
|------|------|-------|-------|---------|
| Backend Lead | | @backend-lead | | |
| DevOps Lead | | @devops-lead | | |
| Mobile Lead | | @mobile-lead | | |
| Product Manager | | @product | | |

---

**Status:** 🟢 Ready for deployment  
**Last Updated:** 2026-07-01  
**Next Review:** 2026-07-02 (Post-launch report)
