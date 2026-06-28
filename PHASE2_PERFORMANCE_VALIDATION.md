# Phase 2 Performance Validation Plan

**Validation Date:** 2026-06-28  
**Goal:** Verify HTTP connection pooling and KMS session caching improvements  
**Target Environment:** Staging (`admin.synaptixplay.com`)

---

## Quick Start

```bash
# 1. Deploy Phase 2 to staging
git push origin main  # Assuming staging auto-deploys from main

# 2. Run baseline metrics (after deployment)
cd Synaptix.OperatorDashboard.Django
python manage.py shell < performance_test.py

# 3. Run load test
ab -n 500 -c 20 http://localhost:8200/healthz

# 4. Compare metrics with expected results below
```

---

## Validation Methodology

### Test Environment Setup

**Required Tools:**
- `ab` (Apache Bench) - HTTP load testing
- `curl` - Manual request testing
- `netstat` or `ss` - Connection monitoring
- Python 3.9+ with Django

**Test Scenarios:**
1. Single request latency
2. Sequential requests (10-50 calls)
3. Concurrent requests (10-100 parallel)
4. Long-running session (repeated auth over 5+ minutes)
5. KMS session expiry and refresh

---

## Performance Metrics to Capture

### 1. **Connection Pool Metrics**

**What to measure:**
```bash
# Monitor active connections during load test
watch -n 1 'netstat -an | grep ESTABLISHED | grep 5000 | wc -l'

# Expected results:
# Before: 100-200+ connections
# After: 10-20 connections
```

**Expected improvements:**
- Active connections: 100+ → 10-20 (90% reduction)
- Connection reuse rate: 0% → 90%+
- TCP handshakes: ~100 per test → ~1-2 per test

### 2. **API Response Time Metrics**

**Measure with Apache Bench:**
```bash
# Single concurrency (sequential requests)
ab -n 50 -c 1 http://localhost:8200/healthz

# Moderate concurrency
ab -n 100 -c 10 http://localhost:8200/healthz

# High concurrency
ab -n 500 -c 50 http://localhost:8200/healthz
```

**Expected before/after:**

| Test | Before | After | Improvement |
|------|--------|-------|------------|
| 50 seq (c=1) | 60s | 5s | 92% ↓ |
| 100 mod (c=10) | 12s | 1.2s | 90% ↓ |
| 500 high (c=50) | 60s | 6s | 90% ↓ |

**Parse results:**
```
Requests per second:      [RPS before] → [RPS after]
Time per request:         [Before ms] → [After ms]
Time per request (mean):  [Before ms] → [After ms]
Transfer rate:            [Before] → [After] [Kbytes/sec]
```

### 3. **KMS Session Cache Metrics**

**Measure KMS call frequency:**
```python
# Add to Django logging
LOGGING = {
    'loggers': {
        'dashboard.services.admin_auth_client': {
            'level': 'DEBUG',
        },
    }
}

# Grep logs for KMS calls
grep -c "POST.*internal/security/sessions/start" logs/django.log
grep -c "POST.*internal/security/encrypt" logs/django.log
grep -c "POST.*internal/security/decrypt" logs/django.log
```

**Expected results:**

| Scenario | Before | After | Improvement |
|----------|--------|-------|------------|
| 10 auth requests | 30 KMS calls | 2-3 KMS calls | 90% ↓ |
| Over 5 minutes | 30+ calls | 1 session + 2-3 calls | 66% ↓ |
| Session expiry (>5min) | N/A | Auto-refresh | N/A |

### 4. **Memory Usage Metrics**

**Measure Django process memory:**
```bash
# Monitor during load test
watch -n 1 'ps aux | grep "python.*manage.py" | grep -v grep'

# Check memory usage
ps -o pid,vsz,rss,comm -p $(pgrep -f 'python.*manage.py')
```

**Expected improvements:**
- RSS (resident set size): 150MB → 100MB (33% reduction)
- VSZ (virtual size): 300MB → 200MB (33% reduction)
- Connection object overhead: 500KB → 50KB per connection

### 5. **CPU Usage Metrics**

**Measure CPU utilization:**
```bash
# During load test
top -b -n 20 -p $(pgrep -f 'python.*manage.py')

# Expected:
# Before: 60-80% CPU (creating TLS connections)
# After: 20-30% CPU (reusing connections)
```

---

## Test Scripts

### Test 1: Single Request Latency

**Test:** Measure time for a single HTTP request

```bash
#!/bin/bash
# Single request latency test

echo "=== Single Request Latency Test ==="
echo ""

for i in {1..10}; do
    curl -w "Request $i: %{time_total}s total, %{time_starttransfer}s to first byte\n" \
        -o /dev/null -s \
        http://localhost:8200/healthz
done

echo ""
echo "Expected: Each request <200ms (was 1000ms+)"
```

### Test 2: Connection Pool Health

**Test:** Verify connection pooling is working

```python
# connection_test.py
import httpx
import time
import subprocess
from datetime import datetime

def monitor_connections():
    """Monitor active TCP connections during requests."""
    try:
        result = subprocess.run(
            "netstat -an | grep ESTABLISHED | grep 5000 | wc -l",
            shell=True,
            capture_output=True,
            text=True
        )
        return int(result.stdout.strip())
    except:
        return 0

def test_connection_pooling():
    print("=== Connection Pool Health Test ===\n")
    
    client = httpx.Client()
    
    print(f"Initial connections: {monitor_connections()}")
    
    # Make 10 sequential requests
    print("\nMaking 10 sequential requests...")
    start = time.time()
    
    for i in range(10):
        try:
            response = client.get("http://localhost:5000/healthz", timeout=5)
            elapsed = time.time() - start
            conns = monitor_connections()
            print(f"  Request {i+1}: {elapsed:.2f}s, Active: {conns} connections")
        except Exception as e:
            print(f"  Request {i+1}: FAILED - {e}")
    
    elapsed = time.time() - start
    conns = monitor_connections()
    
    print(f"\nFinal connections: {conns}")
    print(f"Total time: {elapsed:.2f}s")
    print(f"Average per request: {elapsed/10:.2f}s")
    
    # Expected: 10-20 connections (reused), <0.2s per request
    if conns <= 20 and elapsed < 2:
        print("\n✅ PASS: Connection pooling working correctly")
    else:
        print("\n❌ FAIL: Unexpected connection count or latency")
    
    client.close()

if __name__ == "__main__":
    test_connection_pooling()
```

### Test 3: KMS Session Cache Validation

**Test:** Verify KMS session caching works

```python
# kms_cache_test.py
import time
from dashboard.services.admin_auth_client import admin_login, admin_forgot_password
from dashboard.services.http_client_pool import get_kms_session

def test_kms_session_caching():
    print("=== KMS Session Cache Test ===\n")
    
    # Clear cache
    from dashboard.services.http_client_pool import clear_kms_session
    clear_kms_session()
    
    print("1. Initial state - cache empty")
    cached = get_kms_session()
    print(f"   Cached session: {cached}\n")
    
    print("2. Making first auth call (will create KMS session)")
    start = time.time()
    try:
        # This will create and cache a KMS session
        admin_forgot_password("test@example.com")
    except Exception as e:
        print(f"   Error: {e} (expected if no valid user)")
    elapsed_1 = time.time() - start
    
    cached = get_kms_session()
    print(f"   Time taken: {elapsed_1:.2f}s")
    print(f"   Cached session: {cached is not None}\n")
    
    print("3. Making second auth call (will reuse KMS session)")
    start = time.time()
    try:
        admin_forgot_password("test2@example.com")
    except Exception as e:
        print(f"   Error: {e} (expected if no valid user)")
    elapsed_2 = time.time() - start
    
    print(f"   Time taken: {elapsed_2:.2f}s")
    print(f"   Time saved: {elapsed_1 - elapsed_2:.2f}s ({(1 - elapsed_2/elapsed_1)*100:.0f}%)\n")
    
    print("4. Waiting for cache expiry (6 seconds)")
    time.sleep(6)
    
    cached = get_kms_session()
    print(f"   Cached session after expiry: {cached}\n")
    
    if elapsed_2 < elapsed_1 and cached is None:
        print("✅ PASS: KMS session caching working correctly")
    else:
        print("❌ FAIL: Unexpected cache behavior")

if __name__ == "__main__":
    test_kms_session_caching()
```

### Test 4: Load Test with Apache Bench

**Test:** Simulate realistic load

```bash
#!/bin/bash
# load_test.sh

echo "=== Phase 2 Load Test ==="
echo ""

# Test 1: Sequential requests
echo "Test 1: Sequential requests (50 requests, 1 concurrent)"
ab -n 50 -c 1 -t 60 http://localhost:8200/healthz
echo ""

# Test 2: Moderate load
echo "Test 2: Moderate load (100 requests, 10 concurrent)"
ab -n 100 -c 10 -t 60 http://localhost:8200/healthz
echo ""

# Test 3: High load
echo "Test 3: High load (500 requests, 50 concurrent)"
ab -n 500 -c 50 -t 60 http://localhost:8200/healthz
echo ""

echo "=== Expected Results ==="
echo "Requests/sec: 50+ (was 10-15)"
echo "Mean response: <100ms (was 1000ms+)"
echo "Failed requests: 0"
```

---

## Validation Checklist

### Pre-Deployment Validation

- [ ] Phase 2 code reviewed and merged
- [ ] No syntax errors in Python modules
- [ ] All imports correctly configured
- [ ] HTTP client pool module tested locally
- [ ] KMS session cache logic verified

### Post-Deployment Validation (Staging)

- [ ] Django app starts without errors
- [ ] Health check endpoint responds (<200ms)
- [ ] Login flow works (no auth errors)
- [ ] API calls succeed with pooled client
- [ ] No connection pool exhaustion errors

### Performance Validation

**Connection Pool:**
- [ ] Active connections < 20 during normal load
- [ ] Connection reuse rate > 90%
- [ ] No connection timeout errors
- [ ] Client closes cleanly on shutdown

**API Performance:**
- [ ] Single request < 200ms (was 1000ms+)
- [ ] Health check < 300ms for 3 services
- [ ] Sequential 10 requests < 2s (was 10s+)
- [ ] Concurrent 50 requests handled smoothly

**KMS Caching:**
- [ ] KMS session cached after first call
- [ ] Second auth call faster than first
- [ ] Session expires after 5 minutes
- [ ] New session created automatically after expiry

**Memory & Resources:**
- [ ] Memory usage stable (no growth over time)
- [ ] CPU usage < 50% during load test
- [ ] No memory leaks detected
- [ ] Graceful shutdown without errors

### Regression Testing

- [ ] User login works
- [ ] Admin password recovery flow works
- [ ] Permission checks still enforced
- [ ] Security audit logging intact
- [ ] No authentication failures
- [ ] No API errors introduced

---

## Expected Results vs Acceptance Criteria

### Acceptance Criteria

| Metric | Target | Pass/Fail |
|--------|--------|-----------|
| Single request latency | < 200ms | Must achieve |
| Sequential 10 requests | < 2s total | Must achieve |
| KMS session reuse | > 80% | Must achieve |
| Active connections | < 20 | Must achieve |
| Memory reduction | > 20% | Should achieve |
| No regressions | 0 failures | Must achieve |

### Performance Improvement Targets

```
Single Request:
  Before: 1200ms (TCP handshake + TLS + request)
  After:  100ms (reused connection + request)
  Target: 90% improvement ✓

Sequential 10 Requests:
  Before: 12s (10 × 1.2s)
  After:  1s (1 connection + 10 requests)
  Target: 90% improvement ✓

Concurrent 50 Requests:
  Before: 60s (connection pool exhaustion)
  After:  6s (20 pooled connections)
  Target: 90% improvement ✓

KMS Session Cache:
  Before: 3 API calls per auth request
  After:  1 API call per 5-minute session
  Target: 66% reduction in KMS calls ✓

Memory Usage:
  Before: 50MB per 100 requests
  After:  5MB per 100 requests
  Target: 90% reduction ✓
```

---

## Monitoring & Logging During Validation

### Enable Debug Logging

```python
# settings.py
LOGGING = {
    'version': 1,
    'disable_existing_loggers': False,
    'formatters': {
        'verbose': {
            'format': '{levelname} {asctime} {module} {process:d} {thread:d} {message}',
            'style': '{',
        },
    },
    'handlers': {
        'file': {
            'level': 'DEBUG',
            'class': 'logging.FileHandler',
            'filename': 'logs/django_debug.log',
            'formatter': 'verbose',
        },
    },
    'loggers': {
        'dashboard.services.http_client_pool': {
            'handlers': ['file'],
            'level': 'DEBUG',
            'propagate': False,
        },
        'dashboard.services.admin_auth_client': {
            'handlers': ['file'],
            'level': 'DEBUG',
            'propagate': False,
        },
    },
}
```

### Key Log Statements to Watch For

```
[HTTP Client Pool] Creating new HTTP client with limits...
[HTTP Client Pool] Reusing existing HTTP client
[Admin Auth] KMS session cache hit
[Admin Auth] KMS session cache miss - creating new session
[Admin Auth] KMS session expired - creating new session
[Admin Auth] Using pooled HTTP client for request
```

---

## Troubleshooting During Validation

### Issue: Connection pool exhausted

**Symptoms:** `httpx.ConnectTimeout`, `Connection pool exhausted`

**Investigation:**
```bash
# Check active connections
netstat -an | grep ESTABLISHED | grep 5000 | wc -l

# Should be < 20. If > 20, pool is exhausted.
```

**Solution:**
- Increase pool limits in `http_client_pool.py`
- Check for connection leaks (connections not being released)
- Monitor slow/hanging requests

### Issue: KMS session not caching

**Symptoms:** KMS calls not reducing, cache always empty

**Investigation:**
```python
from dashboard.services.http_client_pool import get_kms_session, _kms_session_cache
print(f"Cache contents: {_kms_session_cache}")
print(f"Cached session: {get_kms_session()}")
```

**Solution:**
- Verify `cache_kms_session()` is called in `_start_internal_session()`
- Check TTL setting (default 300s = 5 min)
- Verify session ID is valid

### Issue: Higher latency than expected

**Symptoms:** Requests still taking 800ms+

**Investigation:**
```bash
# Trace request timing
curl -w "@curl_format.txt" -o /dev/null -s http://localhost:8200/healthz

# Check if connections are being pooled
netstat -an | grep ESTABLISHED | grep 5000
```

**Solution:**
- Verify client is singleton (same instance reused)
- Check for DNS resolution delays
- Verify no SSL/TLS renegotiation
- Monitor backend API latency

---

## Sign-Off & Next Steps

### Validation Sign-Off Template

```markdown
# Phase 2 Validation Sign-Off

Date: [date]
Validated by: [name]
Environment: [staging/production]

## Test Results

### Connection Pool
- Active connections: [actual] (target: <20) ✓/✗
- Reuse rate: [actual]% (target: >90%) ✓/✗
- Pool errors: [count] (target: 0) ✓/✗

### API Performance
- Single request: [actual]ms (target: <200ms) ✓/✗
- Sequential 10: [actual]ms (target: <2s) ✓/✗
- Concurrent 50: [actual]ms (target: <6s) ✓/✗

### KMS Caching
- Cache hit rate: [actual]% (target: >80%) ✓/✗
- Session reuse: [actual]s (target: 5min TTL) ✓/✗

### Memory & Resources
- Memory reduction: [actual]% (target: >20%) ✓/✗
- CPU during load: [actual]% (target: <50%) ✓/✗
- Regressions: [count] (target: 0) ✓/✗

## Recommendation

- [ ] PASS - All metrics met, proceed to Phase 3
- [ ] PASS WITH WARNINGS - Some metrics below target, needs monitoring
- [ ] FAIL - Significant issues, investigate before Phase 3

## Notes

[Any issues, anomalies, or observations during validation]

---
Signed: [name]
Date: [date]
```

### Proceed to Phase 3 When:

✅ Single request latency < 200ms  
✅ Sequential requests 90%+ faster  
✅ Connection pool < 20 active connections  
✅ KMS session caching working (>80% hit rate)  
✅ Memory usage reduced by >20%  
✅ Zero new regressions  
✅ All error logs clean  

---

## Phase 3 Readiness

Once Phase 2 validation passes:

1. **Document findings** in this sign-off section
2. **Archive performance baseline** for future comparison
3. **Update monitoring dashboards** with Phase 2 metrics
4. **Schedule Phase 3 kickoff** (Static Asset Minification + Redis Caching)

---

## Appendix: Quick Reference Commands

```bash
# Monitor connections in real-time
watch -n 1 'netstat -an | grep ESTABLISHED | grep 5000 | wc -l'

# Test single endpoint
curl -w "@curl_format.txt" -o /dev/null -s http://localhost:8200/healthz

# Load test (50 sequential)
ab -n 50 -c 1 http://localhost:8200/healthz

# Load test (100 concurrent)
ab -n 100 -c 10 http://localhost:8200/healthz

# Monitor memory
ps -o pid,vsz,rss,comm -p $(pgrep -f 'python.*manage.py')

# View error logs
tail -f logs/django.log | grep -i error

# Test KMS cache (Python shell)
python manage.py shell < kms_cache_test.py
```

---

**Validation Complete When All Checklist Items Checked ✅**

