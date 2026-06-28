# Phase 2 Validation - Quick Start Guide

## Environment Setup Issue

If you're getting `ModuleNotFoundError: No module named 'django'`, your Python environment needs to be configured first.

---

## Solution 1: Using Docker (Recommended)

If you're running in Docker, the dependencies should be installed in the container image.

```bash
# Option A: Restart the container to reload Python path
docker compose -f docker/compose.yml down
docker compose -f docker/compose.yml up -d operator-dashboard

# Option B: Execute command inside running container
docker compose exec operator-dashboard python manage.py validate_phase2_performance --quick

# Option C: Check if Python dependencies are installed
docker compose exec operator-dashboard pip list | grep -i django
```

---

## Solution 2: Local Python Environment Setup

If you're running locally, you need to install Python dependencies.

### Step 1: Find the Requirements File

```bash
# From project root
cd Synaptix.OperatorDashboard.Django

# Look for requirements file
ls -la requirements*.txt
```

### Step 2: Create Virtual Environment (Recommended)

```bash
# Create virtual environment
python3 -m venv venv

# Activate it
source venv/bin/activate  # On Linux/Mac
# or
venv\Scripts\activate     # On Windows
```

### Step 3: Install Dependencies

```bash
# If there's a requirements.txt
pip install -r requirements.txt

# If no requirements.txt, install Django manually
pip install django httpx
```

### Step 4: Check Installation

```bash
python3 -m django --version
# Should output: e.g., "5.0.1"
```

### Step 5: Run Validation

```bash
python manage.py validate_phase2_performance --quick
```

---

## Solution 3: If Django is Installed Globally

```bash
# Check if Django is installed
python3 -c "import django; print(django.VERSION)"

# If installed, ensure you're in the right directory
cd Synaptix.OperatorDashboard.Django

# Run validation with full Python path
python3 manage.py validate_phase2_performance --quick
```

---

## Docker Environment (Most Likely Your Case)

Based on the path `/opt/synaptix/`, you're likely using Docker.

### To Run Validation in Docker:

```bash
# SSH into the container or use docker exec
docker exec synaptix_operator_dashboard python manage.py validate_phase2_performance --quick

# Or if that doesn't work, use compose:
docker compose -f docker/compose.yml exec operator-dashboard python manage.py validate_phase2_performance --quick
```

### If that still doesn't work:

```bash
# Check what's in the container
docker exec synaptix_operator_dashboard python -c "import django; print('Django OK')"

# List installed packages
docker exec synaptix_operator_dashboard pip list | grep -i django

# If Django is missing, reinstall
docker compose -f docker/compose.yml up -d --build operator-dashboard
```

---

## Alternative: Manual Performance Testing (No Django Required)

If you can't get Django environment working, run this manual test instead:

```bash
#!/bin/bash
# Manual performance test (no Django required)

echo "=== Phase 2 Manual Performance Test ==="

# Test 1: Single request latency
echo ""
echo "Test 1: Single Request Latency"
for i in {1..5}; do
    echo -n "  Request $i: "
    time curl -s http://localhost:8200/healthz > /dev/null
done

# Test 2: Connection monitoring
echo ""
echo "Test 2: Active Connections"
netstat -an | grep ESTABLISHED | grep -E "5000|8200" | wc -l

# Test 3: Concurrent requests (using ab if available)
echo ""
echo "Test 3: Load Test (50 requests, 10 concurrent)"
if command -v ab &> /dev/null; then
    ab -n 50 -c 10 -t 30 http://localhost:8200/healthz
else
    echo "  Apache Bench (ab) not installed"
    echo "  Install with: apt-get install apache2-utils"
fi

# Test 4: Check for errors in logs
echo ""
echo "Test 4: Check Logs for Errors"
if [ -f "logs/django.log" ]; then
    echo "  Errors found:"
    grep -i "error\|exception" logs/django.log | tail -5
else
    echo "  Django log file not found"
fi

echo ""
echo "=== Manual Test Complete ==="
```

---

## Proper Setup Steps (Recommended)

### For Docker Environment:

**Step 1: Verify Container is Running**
```bash
docker ps | grep operator-dashboard
# Should show synaptix_operator_dashboard running
```

**Step 2: Install Dependencies in Container**
```bash
docker compose exec operator-dashboard pip install django httpx
```

**Step 3: Run Validation**
```bash
docker compose exec operator-dashboard python manage.py validate_phase2_performance --quick
```

**Step 4: Monitor Connections**
```bash
# From host machine
watch -n 1 'netstat -an | grep ESTABLISHED | grep 5000 | wc -l'
```

### For Local Environment:

**Step 1: Set Up Virtual Environment**
```bash
cd Synaptix.OperatorDashboard.Django
python3 -m venv venv
source venv/bin/activate
```

**Step 2: Install Dependencies**
```bash
pip install -r requirements.txt
# or manually:
pip install django==5.0 httpx requests
```

**Step 3: Run Validation**
```bash
python manage.py validate_phase2_performance --quick
```

---

## Quick Diagnostic Commands

Use these to identify the issue:

```bash
# Check Python version
python3 --version

# Check if Django is accessible
python3 -c "import django; print(f'Django {django.VERSION}')"

# List installed packages
pip list | grep -i django

# Check current directory
pwd

# Check manage.py exists
ls -la manage.py

# Try running with explicit Python
/usr/bin/python3 manage.py validate_phase2_performance --quick
```

---

## If You're Still Stuck

Please run these and share the output:

```bash
# 1. Show current directory
pwd

# 2. Show Python version
python3 --version

# 3. Try to import Django
python3 -c "import django" 2>&1

# 4. List installed packages with Django
pip list 2>&1 | grep -i django

# 5. Check if manage.py exists
ls -la manage.py 2>&1

# 6. Show Python path
python3 -c "import sys; print('\n'.join(sys.path))"
```

---

## Expected Success Output

When working correctly, you should see:

```
=== Phase 2 Performance Validation ===

Test 1: Connection Pool Health
  Initial connections: 2
  ✓ Request 1: 1.23s
  ✓ Request 2: 0.15s
  ...
  ✅ PASS: Connection pooling working correctly

Test 2: API Response Time
  Average: 125.5ms
  ✅ PASS: Response times under 200ms

Test 3: KMS Session Caching
  ✅ PASS: KMS caching configured correctly

Test 4: HTTP Client Pooling
  ✅ PASS: Pool configuration correct

=== Validation Complete ===
```

---

## Next Steps

Once you have Django environment set up:

1. **Run quick validation:**
   ```bash
   python manage.py validate_phase2_performance --quick
   ```

2. **Monitor connections during test:**
   ```bash
   watch -n 1 'netstat -an | grep ESTABLISHED | grep 5000 | wc -l'
   ```

3. **Check for errors:**
   ```bash
   tail -f logs/django.log | grep -i error
   ```

4. **When ready for full test:**
   ```bash
   python manage.py validate_phase2_performance --full
   ```

---

## Support

If you're getting a different error, please provide:

1. **Your environment:**
   - Docker? Local? Cloud?
   - OS? (Linux, Mac, Windows?)

2. **Error message:** (The full error)

3. **Output of these commands:**
   ```bash
   python3 --version
   pwd
   ls -la manage.py
   ```

Then I can help you troubleshoot further!

