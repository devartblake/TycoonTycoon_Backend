# Sentry Integration Test Guide

## Overview
This guide explains how to test Sentry error tracking integration with the running backend.

## Prerequisites
- Backend compiled and running (`dotnet run`)
- Sentry account with project created (at https://sentry.io)
- Valid Sentry DSN in environment variables

## Configuration Status

### Development (.env)
- **Status**: Disabled by default
- **Setup**: Leave SENTRY_DSN empty or unset
- **To Enable**: Add valid Sentry DSN to environment

### Staging (.env.staging)
- **Current**: `SENTRY_DSN=https://key@domain.ingest.sentry.io/project`
- **Status**: Template placeholder
- **To Activate**: Replace with actual Sentry project DSN

### Production (.env.production)
- **Current**: Real Sentry DSN configured
- **Status**: ✅ Ready for error tracking

## Running Tests

### Test 1: Verify Backend Startup
```bash
cd Synaptix.Backend.Api
dotnet run
```

Expected output:
```
✅ Configuring Sentry (env: development, sampling: 100%)
```

Or if DSN is disabled:
```
⚠️  Sentry DSN not configured - error tracking disabled
```

### Test 2: Trigger a Test Error
Call any endpoint that might fail, or add a test endpoint:

```bash
# Example: Call a endpoint that doesn't exist or throws
curl -X GET http://localhost:5000/api/v1/test-error

# Example: Submit invalid data to trigger validation error
curl -X POST http://localhost:5000/api/v1/some-endpoint \
  -H "Content-Type: application/json" \
  -d '{"invalid": "data"}'
```

### Test 3: Verify in Sentry Dashboard
1. Go to https://sentry.io
2. Select your Synaptix project
3. Check **Issues** tab
4. Verify error appears with:
   - Exception type
   - Stack trace
   - Request context
   - Breadcrumbs (if captured)

### Test 4: Monitor Health Check Exclusion
Make sure health checks are NOT sent to Sentry:

```bash
curl -X GET http://localhost:5000/health

# Should NOT appear in Sentry Issues
```

## Monitoring Endpoints

These endpoints show monitoring metrics:

```bash
# Job metrics
curl http://localhost:5000/monitoring/jobs/metrics

# Error rate summary
curl http://localhost:5000/monitoring/errors/summary

# Per-endpoint error metrics
curl http://localhost:5000/monitoring/errors/by-endpoint

# High-rate endpoints
curl http://localhost:5000/monitoring/errors/high-rate
```

## Troubleshooting

### Sentry Not Capturing Errors

**Issue**: Errors don't appear in Sentry dashboard

**Solutions**:
1. Verify DSN is correct: `echo $SENTRY_DSN`
2. Check sampling rate (100% in dev, 50% in staging, 10% in prod)
3. Verify endpoint is NOT on exclusion list (/health, /metrics, /alive)
4. Wait 30-60 seconds for event delivery
5. Check Sentry project is active on sentry.io
6. Check no ad-blocker is blocking Sentry endpoints

### Dashboard Not Showing Errors

**Issue**: Sentry dashboard shows 0 errors

**Solutions**:
1. Verify errors were actually triggered
2. Check project is configured correctly
3. Verify DSN format: `https://<key>@<host>/project-id`
4. Try triggering a manual error with test endpoint

## Environment Variables Reference

```bash
# Development
SENTRY_DSN=                    # Disabled (optional)

# Staging
SENTRY_DSN=https://key@domain.ingest.sentry.io/project
SENTRY_TRACE_SAMPLE_RATE=0.5   # 50% sampling

# Production  
SENTRY_DSN=https://actual-key@actual-org.ingest.us.sentry.io/actual-project
SENTRY_TRACE_SAMPLE_RATE=0.1   # 10% sampling
```

## Verification Checklist

After starting the backend:

- [ ] Backend starts without errors
- [ ] Console shows "✅ Configuring Sentry" message
- [ ] GET /health endpoint works
- [ ] GET /monitoring/errors/summary returns valid JSON
- [ ] Trigger test error via API
- [ ] Error appears in Sentry dashboard within 30 seconds
- [ ] Error includes full stack trace
- [ ] Error shows request context
- [ ] Health check endpoints do NOT appear in Sentry

## Next Steps

Once Sentry is verified:

1. **Review dashboards** - Check all 4 Grafana dashboards for data
2. **Adjust thresholds** - Modify alert thresholds based on baseline
3. **Set up Slack** - Route Sentry alerts to Slack channel
4. **Performance tuning** - Adjust trace sample rates if needed

## Support

For integration issues:
- Check Sentry docs: https://docs.sentry.io/product/integrations/platforms/dotnet/
- Check Sentry ASP.NET Core docs: https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/
- Review our implementation: `Synaptix.Monitoring/Errors/SentryIntegration.cs`
