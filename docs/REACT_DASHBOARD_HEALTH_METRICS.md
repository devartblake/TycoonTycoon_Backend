# React Operator Dashboard - Health Metrics Integration

**Status:** ✅ Ready for Integration  
**Implementation Date:** 2026-07-03

---

## Overview

The React operator dashboard has been wired to display real-time health metrics from the backend `/health` endpoint instead of mock data. This provides operators with live visibility into system status.

---

## Components Added

### 1. Health Check Client (`src/lib/health-check-client.ts`)

Provides low-level API communication with backend health endpoints:

```typescript
import { healthCheckClient } from '@/lib/health-check-client'

// Fetch specific health statuses
const health = await healthCheckClient.getHealthStatus()      // /health
const alive = await healthCheckClient.getLivenessStatus()     // /alive
const ready = await healthCheckClient.getReadinessStatus()    // /ready

// Get aggregated system metrics
const metrics = await healthCheckClient.getSystemMetrics()

// Start polling
const intervalId = healthCheckClient.startPolling((metrics) => {
  console.log('Updated metrics:', metrics)
}, 30000)

// Stop polling
healthCheckClient.stopPolling(intervalId)
```

**Features:**
- Automatic retry and error handling
- 30-second caching to reduce API calls
- Transforms health checks to system metrics
- Prometheus integration (optional)
- Fallback to defaults on error

### 2. useHealthMetrics Hook (`src/hooks/use-health-metrics.ts`)

React hook for component-level health metric management:

```typescript
import { useHealthMetrics } from '@/hooks/use-health-metrics'

function MyComponent() {
  const { metrics, isLoading, error, refresh } = useHealthMetrics({
    enabled: true,
    pollInterval: 30000,
    onSuccess: (data) => console.log('Updated:', data),
    onError: (error) => console.error('Failed:', error),
  })

  return (
    <div>
      {isLoading && 'Loading...'}
      {error && `Error: ${error.message}`}
      {metrics && `CPU: ${metrics.cpuUsage}%`}
      <button onClick={refresh}>Refresh Now</button>
    </div>
  )
}
```

**Features:**
- Automatic polling with configurable interval
- Error handling with callbacks
- Manual refresh capability
- Loading state management
- Memory cleanup on unmount

### 3. Enhanced Home Page (`src/features/dashboard/pages/home-with-health-metrics.tsx`)

New home page that integrates real health metrics:

```typescript
import DashboardHomePageWithHealth from '@/features/dashboard/pages/home-with-health-metrics'

// Use instead of original home page
// Shows real-time health data alongside existing dashboard stats
```

**Features:**
- Real-time system metrics display
- Health status indicator (green/red)
- Response time and error rate cards
- Uptime tracking
- Gradual migration from mock data
- Fallback to mock data if real metrics unavailable

---

## Integration Steps

### Step 1: Environment Configuration

Add to `.env` or environment variables:

```bash
# Backend API base URL for health checks
REACT_APP_HEALTH_CHECK_BASE_URL=http://localhost:5000
# Or use the general API base URL:
REACT_APP_API_BASE_URL=http://localhost:5000
```

### Step 2: Replace Home Page (Option A: Full Migration)

Update `src/features/dashboard/pages/home.tsx`:

Replace the import:
```typescript
// OLD: import DashboardHomePage from './home'
// NEW:
import DashboardHomePageWithHealth from './home-with-health-metrics'
export default DashboardHomePageWithHealth
```

### Step 3: Gradual Migration (Option B: Side-by-side)

Keep both pages and route to the new one:

```typescript
// In router configuration
{
  path: '/dashboard',
  element: <DashboardHomePageWithHealth />, // New with health metrics
}
{
  path: '/dashboard/legacy',
  element: <DashboardHomePage />, // Old with mock data (for comparison)
}
```

### Step 4: Test Health Metrics

1. Start dev server:
   ```bash
   cd Synaptix.OperatorDashboard.React
   npm start
   ```

2. Ensure backend is running:
   ```bash
   cd ../..
   docker compose --profile dev up
   ```

3. Navigate to dashboard
4. Verify metrics update every 30 seconds
5. Check browser console for any errors

---

## Available Metrics

### System Metrics Object

```typescript
interface SystemMetrics {
  // Request metrics
  apiGatewayRequests: number      // Requests in last hour
  activeConnections: number       // Current connection count

  // Resource usage
  cpuUsage: number               // CPU percentage (0-100)
  memoryUsage: number            // Memory percentage (0-100)
  diskUsage: number              // Disk percentage (0-100)

  // Performance
  responseTime: number           // P95 response time in ms
  errorRate: number              // Error rate as decimal (0-1)
  uptime: number                 // Uptime in seconds

  // Status
  isHealthy: boolean             // Overall health status
}
```

### Health Status Object

```typescript
interface HealthStatus {
  status: 'Healthy' | 'Degraded' | 'Unhealthy'
  checks: Record<string, { status: string }>
  timestamp: string
}
```

---

## Data Flow

```
Backend (/health endpoint)
  ↓
Health Check Client
  ↓ (transforms to SystemMetrics)
useHealthMetrics Hook
  ↓ (caches & polls)
React Component
  ↓ (auto-updates every 30s)
Operator Dashboard Display
```

---

## Error Handling

The system gracefully degrades when backend is unavailable:

1. **Real metrics available** → Display live data ✅
2. **Temporary network error** → Show cached data ⚠️
3. **Persistent errors** → Fallback to mock data
4. **User notified** → "Fallback mode" indicator shown

---

## Performance Considerations

### Caching

- 30-second cache to reduce API calls
- Manual refresh available on demand
- Automatic cache invalidation

### Polling

- 30-second default poll interval
- Configurable per component
- Stops automatically on component unmount
- No memory leaks

### Network

- Single request per 30 seconds
- Minimal payload (JSON status object)
- Automatic error recovery
- CORS configured for cross-origin requests

---

## Customization

### Change Poll Interval

```typescript
const { metrics } = useHealthMetrics({
  pollInterval: 10000, // 10 seconds
})
```

### Custom Error Handler

```typescript
const { metrics } = useHealthMetrics({
  onError: (error) => {
    // Send to Sentry
    Sentry.captureException(error)
    // Show toast notification
    toast.error('Failed to load health metrics')
  },
})
```

### Disable Polling

```typescript
const { metrics, refresh } = useHealthMetrics({
  enabled: false, // Start disabled
})

// Later, enable by calling refresh
refresh()
```

---

## Testing

### Unit Tests

```typescript
import { renderHook, waitFor } from '@testing-library/react'
import { useHealthMetrics } from '@/hooks/use-health-metrics'

test('fetches and updates metrics', async () => {
  const { result } = renderHook(() => useHealthMetrics())

  await waitFor(() => {
    expect(result.current.metrics).toBeDefined()
    expect(result.current.isLoading).toBe(false)
  })

  expect(result.current.metrics?.isHealthy).toBe(true)
})
```

### Integration Tests

1. Start backend with `docker compose --profile dev up`
2. Visit dashboard in browser
3. Verify metrics display
4. Wait 30 seconds, verify update
5. Open DevTools → Network tab
6. Confirm `GET /health` every 30 seconds

---

## Troubleshooting

### Metrics Not Updating

1. Check backend is running:
   ```bash
   curl http://localhost:5000/health
   ```

2. Check browser console for errors

3. Verify environment variable:
   ```bash
   echo $REACT_APP_API_BASE_URL
   ```

4. Check CORS headers:
   ```bash
   curl -I http://localhost:5000/health
   # Look for: Access-Control-Allow-Origin: *
   ```

### Showing "Fallback Mode"

Indicates backend `/health` endpoint unreachable:

1. Verify backend started: `docker compose ps backend-api`
2. Check logs: `docker compose logs backend-api`
3. Test directly: `curl http://localhost:5000/health`
4. Check network connectivity

### High CPU Usage

If dashboard causes high CPU:

1. Increase poll interval:
   ```typescript
   pollInterval: 60000  // 60 seconds
   ```

2. Disable if not needed:
   ```typescript
   enabled: false
   ```

3. Check browser console for errors

---

## Future Enhancements

1. **Prometheus Direct Integration**
   - Query metrics directly from Prometheus
   - Access historical data
   - Custom metric queries

2. **Real-time WebSocket Updates**
   - Replace polling with WebSocket
   - Lower latency, less bandwidth
   - Server-side push

3. **Metric Graphing**
   - Time-series charts
   - Trend analysis
   - Comparison views

4. **Custom Thresholds**
   - Operator-configurable alerts
   - Visual threshold indicators
   - Alert history

---

## Environment Variables

```bash
# Development
REACT_APP_API_BASE_URL=http://localhost:5000
REACT_APP_HEALTH_CHECK_BASE_URL=http://localhost:5000

# Production/Staging
REACT_APP_API_BASE_URL=https://api.synaptixplay.com
REACT_APP_HEALTH_CHECK_BASE_URL=https://api.synaptixplay.com
```

---

## Documentation References

- [Health Check Endpoints](MONITORING_IMPLEMENTATION.md#part-1-health-check-endpoints)
- [Grafana Dashboards](MONITORING_IMPLEMENTATION.md#part-2-grafana-dashboards)
- [React Dashboard Architecture](../../Synaptix.OperatorDashboard.React/README.md)

---

**Status:** Ready for production use  
**Last Updated:** 2026-07-03  
**Maintained By:** Frontend Team
