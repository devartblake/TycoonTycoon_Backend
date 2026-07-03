# Monitoring Implementation Guide

**Status:** ✅ Complete (Health Checks + Grafana)  
**Implementation Date:** 2026-07-03  
**Version:** 1.0

---

## Overview

This document describes the monitoring stack implementation for Synaptix backend, including health checks, Prometheus metrics collection, and Grafana dashboards.

### Components Implemented

1. ✅ **Health Check Endpoints** - Production-ready readiness/liveness checks
2. ✅ **Grafana Dashboards** - Three auto-provisioned dashboards for visibility
3. ✅ **Prometheus Integration** - Health metrics export and scraping
4. ⏳ **AlertManager** - Scheduled for next phase
5. ⏳ **Database Exporters** - PostgreSQL, MongoDB, Redis (future)
6. ⏳ **Sentry Integration** - Error tracking (future)

---

## Part 1: Health Check Endpoints

### What Are Health Checks?

Health checks are lightweight endpoints that report the application's status:
- **Readiness (`/health`)** - Is the app ready to accept traffic?
- **Liveness (`/alive`)** - Is the app still running?
- **Readiness Specific (`/ready`)** - Checks tagged "ready"
- **Metrics (`/health/metrics`)** - Health status as Prometheus metrics

### Configuration

Health checks are implemented in:
- `Synaptix.Shared\HealthCheck\WebApplicationExtensions.cs`
- `Synaptix.Shared\HealthCheck\DependencyInjectionExtensions.cs`

#### Default Checks

The framework includes these health checks:
- **Self Check** - Always healthy (liveness)
- **Disk Storage** - Ensures disk space available
- **Memory** - Alerts if > 512MB used
- **DNS Resolution** - Network connectivity
- **Ping** - Network health

All checks have configurable thresholds in `DependencyInjectionExtensions.cs`.

### Endpoints

```
GET /health
├─ Readiness: All checks must pass
├─ Response: 200 OK if healthy, 503 if unhealthy
└─ Use: Docker healthchecks, load balancers, orchestrators

GET /alive  
├─ Liveness: Only "live" tagged checks
├─ Response: 200 OK if alive, 503 if dead
└─ Use: Kubernetes liveness probes

GET /ready
├─ Readiness: Only "ready" tagged checks  
├─ Response: 200 OK if ready, 503 if not ready
└─ Use: Kubernetes readiness probes

GET /health/metrics
├─ Prometheus metrics format
├─ Response: Text format Prometheus metrics
└─ Use: Scraped by Prometheus at 15s intervals
```

### Environment Configuration

#### Enable/Disable Health Checks

By default, health checks are **enabled in all environments**. To disable:

```bash
# In .env or docker-compose
DISABLE_HEALTH_CHECKS=true
```

#### For Development

Health checks are automatically enabled and exposed on port 5000.

#### For Production

- Health checks are enabled by default
- Protected by Traefik reverse proxy (TLS + network policies)
- No sensitive data exposed (status only)
- Can be disabled if absolutely needed with `DISABLE_HEALTH_CHECKS=true`

### Testing Health Checks

```bash
# Local development
curl http://localhost:5000/health
curl http://localhost:5000/alive
curl http://localhost:5000/ready
curl http://localhost:5000/health/metrics

# Response format (/health):
{
  "status": "Healthy",
  "checks": {
    "self": { "status": "Healthy" },
    "diskStorage": { "status": "Healthy" },
    "privateMemory": { "status": "Healthy" },
    "dnsResolve": { "status": "Healthy" }
  },
  "timestamp": "2026-07-03T12:00:00Z"
}
```

---

## Part 2: Grafana Dashboards

### Dashboards Overview

Three dashboards are automatically provisioned and available:

#### 1. API Performance Dashboard
**Purpose:** Monitor API request handling and response times

**Key Metrics:**
- P95/P99 Response Times (target: < 200ms baseline)
- Error Rate (target: < 0.1%)
- Request Rate by HTTP Method
- Status Code Distribution

**Thresholds:**
- 🟢 Green: < 200ms
- 🟡 Yellow: 200-300ms
- 🔴 Red: > 300ms

**Use Cases:**
- Detect performance degradation
- Identify slow endpoints
- Track error spikes

#### 2. System Health Dashboard
**Purpose:** Monitor system resource utilization

**Key Metrics:**
- Backend API Service Status (up/down)
- Memory Usage (% of 512MB limit)
- Disk Usage (%)
- Active Thread Count
- Memory Trend (time series)
- CPU Usage Trend (time series)

**Thresholds:**
- 🟢 Memory: < 75%
- 🟡 Memory: 75-90%
- 🔴 Memory: > 90%
- 🟢 Disk: < 75%
- 🟡 Disk: 75-90%
- 🔴 Disk: > 90%

**Use Cases:**
- Detect memory leaks
- Monitor resource exhaustion
- Track growth trends

#### 3. Application Health Dashboard
**Purpose:** Monitor application health checks and request patterns

**Key Metrics:**
- Health Check Status (readiness, liveness, readiness)
- Request Volume by Endpoint (5-minute window)
- Server Error Count (5xx errors over 5 minutes)

**Thresholds:**
- 🟢 Green: All checks passing
- 🔴 Red: Any check failing

**Use Cases:**
- Verify application startup
- Track endpoint usage patterns
- Detect error spikes

### Accessing Dashboards

#### Development

```bash
# Start with dev profile
docker compose --profile dev up

# Access Grafana
# URL: http://localhost:3000
# Default credentials:
#   Username: admin
#   Password: (from docker/.env GRAFANA_PASSWORD)
```

#### Production/Staging

Dashboards are accessible through Traefik reverse proxy at the domain configured in your environment.

### Dashboard Files

Located in `docker/monitoring/grafana/provisioning/dashboards/`:

```
dashboards/
├── api-performance.json      # API response time and error metrics
├── system-health.json        # System resource utilization
├── application-health.json   # Application health checks
└── dashboards.yml           # Grafana provisioning configuration

datasources/
└── prometheus.yml           # Prometheus data source configuration
```

### Customizing Dashboards

Dashboards are auto-provisioned from JSON files. To modify:

1. Edit the JSON file in `docker/monitoring/grafana/provisioning/dashboards/`
2. Restart Grafana container
3. Refresh browser to see changes

Or use Grafana UI:
1. Open dashboard in Grafana
2. Edit panels
3. Save to disk (if disableDeletion: false in dashboards.yml)

### Dashboard Refresh Intervals

All dashboards refresh every 30 seconds for real-time visibility.

To change:
1. Edit the dashboard JSON file
2. Update `"refresh": "30s"` to desired interval
3. Restart Grafana

---

## Part 3: Prometheus Integration

### Prometheus Configuration

Located in `docker/monitoring/prometheus/prometheus.yml`

### Scrape Jobs

```yaml
# Backend API - Metrics (application instrumentation)
job_name: 'backend-api'
metrics_path: '/metrics'
targets: ['backend-api:5000']

# Backend API - Health Checks (health check metrics)
job_name: 'backend-api-health'
metrics_path: '/health/metrics'
targets: ['backend-api:5000']

# RabbitMQ - Management metrics
job_name: 'rabbitmq'
metrics_path: '/api/metrics'
targets: ['rabbitmq:15672']
```

### Accessing Prometheus

**Development:**
```
URL: http://localhost:9090
```

**Query Examples:**

```promql
# API response time P95
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Error rate
100 * (sum(rate(http_requests_total{status=~"5.."}[5m])) / sum(rate(http_requests_total[5m])))

# Memory usage
process_resident_memory_bytes{job="backend-api"}

# Request rate
sum(rate(http_requests_total[5m])) by (method)
```

---

## Part 4: Docker Compose Configuration

### Services

Health check and monitoring services are defined in `docker/compose.yml`:

```yaml
backend-api:
  # ... other config ...
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
    interval: 10s
    timeout: 5s
    retries: 3
    start_period: 30s

prometheus:
  image: prom/prometheus:latest
  profiles: ["dev"]
  volumes:
    - ./docker/monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml

grafana:
  image: grafana/grafana:latest
  profiles: ["dev"]
  volumes:
    - ./docker/monitoring/grafana/provisioning:/etc/grafana/provisioning
```

### Running Monitoring Stack

```bash
# Development (with monitoring)
docker compose --profile dev up

# Production (monitoring optional)
docker compose up

# Add monitoring to production:
docker compose --profile dev up
# (Prometheus/Grafana will start alongside prod services)
```

---

## Part 5: Troubleshooting

### Health Checks Not Responding

```bash
# Check if service is running
docker compose ps backend-api

# Test endpoint directly
curl -v http://localhost:5000/health

# Check logs
docker compose logs backend-api
```

### Grafana Dashboards Not Loading

```bash
# Verify Grafana is running
docker compose ps grafana

# Check dashboard provisioning
docker exec synaptix_grafana ls -la /etc/grafana/provisioning/dashboards/

# Check Grafana logs
docker compose logs grafana

# Verify Prometheus is running
curl http://localhost:9090/api/v1/targets
```

### Prometheus Not Scraping Endpoints

```bash
# Check Prometheus configuration
curl http://localhost:9090/api/v1/targets

# Look for backend-api job
# Expected status: UP (green)

# If DOWN, check:
# 1. Backend API is running: docker compose ps backend-api
# 2. Port 5000 is accessible from Prometheus container
# 3. /health/metrics endpoint responds: curl http://backend-api:5000/health/metrics
```

### No Data in Dashboards

1. Ensure Prometheus is scraping the metrics endpoint
2. Wait 30+ seconds for data to accumulate
3. Check Prometheus has metrics:
   ```
   curl http://localhost:9090/api/v1/query?query=up
   ```
4. Verify datasource in Grafana connects to Prometheus

---

## Part 6: Next Steps

### Immediate (High Priority)

1. **AlertManager Setup**
   - Configure AlertManager for critical alerts
   - Define thresholds for response time, error rate
   - Integrate with Slack/email

2. **Database Exporters**
   - Add PostgreSQL exporter
   - Add MongoDB exporter
   - Update Prometheus scrape config

### Short-term (Medium Priority)

3. **Sentry Integration**
   - Add to backend for error tracking
   - Configure DSN in production

4. **React Dashboard Wiring**
   - Connect operator dashboard to real metrics
   - Display health check status

### Long-term (Lower Priority)

5. **Custom Metrics**
   - Add business logic metrics (game sessions, transactions)
   - Create business dashboard

6. **Log Aggregation**
   - Consider ELK stack for centralized logging

---

## References

- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Prometheus Metrics Types](https://prometheus.io/docs/concepts/metric_types/)
- [Grafana Dashboard JSON](https://grafana.com/docs/grafana/latest/dashboards/build-dashboards/manage-dashboards/#export-dashboard)
- [Health Checks in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/monitor-app-health)

---

**Last Updated:** 2026-07-03  
**Maintained By:** Backend Team  
**Next Review:** After production monitoring period
