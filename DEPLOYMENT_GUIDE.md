# Synaptix Operator Dashboard - Deployment Guide

## Overview

This guide covers deployment of the React-based Operator Dashboard across development, staging, and production environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development](#local-development)
3. [Build & Testing](#build--testing)
4. [Docker Deployment](#docker-deployment)
5. [Environment Configuration](#environment-configuration)
6. [Staging Deployment](#staging-deployment)
7. [Production Deployment](#production-deployment)
8. [UAT Checklist](#uat-checklist)
9. [Monitoring & Troubleshooting](#monitoring--troubleshooting)

---

## Prerequisites

### Required
- Node.js 20.x
- Docker & Docker Compose
- Git
- npm or yarn

### Optional
- Kubernetes (for orchestrated deployment)
- Sentry account (for error tracking)

---

## Local Development

### Setup

```bash
cd Synaptix.OperatorDashboard.React
npm install
```

### Development Server

```bash
npm run dev
# Opens at http://localhost:5173
```

### Enable Mock Mode

Visit `/auth/login` and click "Enable Mock Mode" to test without a backend.

---

## Build & Testing

### Type Checking

```bash
npm run type-check
```

### Linting

```bash
npm run lint
```

### Unit Tests

```bash
npm run test:unit          # Run tests
npm run test:unit:ui       # Interactive mode
npm run test:unit:coverage # Coverage report
```

### E2E Tests

```bash
npm run test:e2e           # Run all tests
npm run test:e2e:ui        # Interactive mode
npm run test:e2e:headed    # See browser
npm run test:e2e:debug     # Debug mode
```

### Full Test Suite

```bash
npm test  # Runs unit + E2E tests
```

### Build for Production

```bash
npm run build
# Output in dist/
```

---

## Docker Deployment

### Build Docker Image

```bash
docker build \
  -f docker/Dockerfile.dashboard-react-optimized \
  --build-arg VITE_API_BASE_URL=https://api.example.com \
  --build-arg VITE_SENTRY_DSN=https://your-sentry-dsn \
  -t operator-dashboard:latest .
```

### Run Docker Container

```bash
docker run \
  -p 8300:8300 \
  -e VITE_API_BASE_URL=https://api.example.com \
  operator-dashboard:latest
```

### Docker Compose

```yaml
services:
  operator-dashboard:
    build:
      context: .
      dockerfile: docker/Dockerfile.dashboard-react-optimized
      args:
        VITE_API_BASE_URL: https://api.example.com
        VITE_SENTRY_DSN: ${SENTRY_DSN}
    ports:
      - "8300:8300"
    environment:
      VITE_ENV: staging
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--spider", "http://localhost:8300"]
      interval: 30s
      timeout: 3s
      retries: 3
```

---

## Environment Configuration

### Configuration Files

- **Development**: `.env.development`
- **Staging**: `.env.staging`
- **Production**: `.env.production`

### Environment Variables

```env
# API Configuration
VITE_API_BASE_URL=https://api.example.com

# Error Tracking
VITE_SENTRY_DSN=https://key@sentry.example.com/project-id

# Feature Flags
VITE_ENABLE_MOCK_MODE=false

# Environment
VITE_ENV=production
VITE_DEBUG_MODE=false
```

### Secrets Management

Store sensitive variables in:
- **CI/CD**: GitHub Actions secrets
- **Docker**: Use `docker secret` or environment variables
- **Kubernetes**: Use `Secret` resources

```bash
# Add GitHub secret
gh secret set SENTRY_DSN --body "https://..."
```

---

## Staging Deployment

### Prerequisites

- Staging backend API running
- SSL certificates configured
- Staging database initialized

### Deploy Steps

```bash
# 1. Pull latest code
git pull origin develop

# 2. Install dependencies
npm install

# 3. Run tests
npm test

# 4. Build for staging
npm run build -- --mode staging

# 5. Build and push Docker image
docker build -t operator-dashboard:staging-v1.0.0 .
docker push registry.example.com/operator-dashboard:staging-v1.0.0

# 6. Update deployment
kubectl set image deployment/operator-dashboard \
  dashboard=registry.example.com/operator-dashboard:staging-v1.0.0 \
  -n staging
```

### Verify Staging

```bash
# Check deployment status
kubectl rollout status deployment/operator-dashboard -n staging

# Check logs
kubectl logs -f deployment/operator-dashboard -n staging

# Port forward for testing
kubectl port-forward svc/operator-dashboard 8300:8300 -n staging
# Visit http://localhost:8300
```

---

## Production Deployment

### Pre-Deployment Checklist

- [ ] All tests passing (unit + E2E)
- [ ] Code reviewed and merged to `main`
- [ ] Documentation updated
- [ ] Sentry configured for production
- [ ] API endpoints verified
- [ ] SSL certificates valid
- [ ] Backups of production database
- [ ] Rollback plan documented

### Deploy Steps

```bash
# 1. Ensure on main branch
git checkout main
git pull origin main

# 2. Create release tag
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# 3. Run production build
npm ci  # Clean install
npm run type-check
npm run build  # With VITE_ENV=production

# 4. Build and sign Docker image
docker build \
  --label org.opencontainers.image.version=1.0.0 \
  --label org.opencontainers.image.revision=$(git rev-parse HEAD) \
  -t operator-dashboard:1.0.0 .

# 5. Push to registry
docker tag operator-dashboard:1.0.0 registry.example.com/operator-dashboard:1.0.0
docker push registry.example.com/operator-dashboard:1.0.0

# 6. Deploy (with canary)
kubectl set image deployment/operator-dashboard-canary \
  dashboard=registry.example.com/operator-dashboard:1.0.0 \
  -n production
kubectl wait --for=condition=available --timeout=5m \
  deployment/operator-dashboard-canary -n production

# 7. Monitor metrics for 30 minutes
# Check Sentry, Prometheus, user feedback

# 8. Promote to production
kubectl set image deployment/operator-dashboard \
  dashboard=registry.example.com/operator-dashboard:1.0.0 \
  -n production
kubectl rollout status deployment/operator-dashboard -n production
```

### Rollback Procedure

```bash
# If issues detected, rollback
kubectl rollout undo deployment/operator-dashboard -n production

# Verify rollback
kubectl rollout status deployment/operator-dashboard -n production

# Investigate issue
kubectl logs -f deployment/operator-dashboard -n production

# Create issue for fix
# Deploy fix, test in staging, retry production deployment
```

---

## UAT Checklist

### Functional Testing

#### Authentication
- [ ] Login with mock credentials
- [ ] Forgot password flow works
- [ ] Session persists after refresh
- [ ] Logout clears session

#### Dashboard Navigation
- [ ] All 18+ modules accessible
- [ ] Navigation doesn't break back button
- [ ] Page titles update correctly
- [ ] Sidebar/mobile menu works

#### Data Display
- [ ] Tables load and display data
- [ ] Empty states show when no data
- [ ] Pagination works correctly
- [ ] Sorting/filtering functions
- [ ] Search queries return results

#### CRUD Operations
- [ ] Create new items
- [ ] Read/display existing items
- [ ] Update item properties
- [ ] Delete items with confirmation

### Quality Assurance

#### Performance
- [ ] Page loads in < 3 seconds
- [ ] No layout shifts during load
- [ ] Interactions respond within 200ms
- [ ] Charts/graphs render smoothly

#### Accessibility
- [ ] Tab navigation works
- [ ] Color contrast acceptable
- [ ] Form labels present
- [ ] Error messages clear

#### Responsiveness
- [ ] Mobile layout (375px) - no horizontal scroll
- [ ] Tablet layout (768px) - proper grid
- [ ] Desktop layout (1024px+) - full feature set
- [ ] Touch targets ≥ 44px

#### Dark Mode
- [ ] Toggle appears in settings
- [ ] Colors readable in dark mode
- [ ] All components styled correctly
- [ ] Preference persists

#### Error Handling
- [ ] Network errors shown gracefully
- [ ] Permission denied shows helpful message
- [ ] Invalid input shows validation error
- [ ] 500 errors don't crash app

### Security Testing

- [ ] No XSS vulnerabilities
- [ ] CSRF tokens present
- [ ] Sensitive data not in logs
- [ ] API keys not exposed
- [ ] SQL injection attempts blocked

### Browser Compatibility

- [ ] Chrome (latest 2 versions)
- [ ] Firefox (latest 2 versions)
- [ ] Safari (latest 2 versions)
- [ ] Edge (latest 2 versions)

### Load Testing

- [ ] Handles 100 concurrent users
- [ ] No memory leaks over 1 hour
- [ ] Response times stable
- [ ] Error rate < 0.1%

---

## Monitoring & Troubleshooting

### Health Checks

```bash
# HTTP health check
curl -v http://dashboard-url/health

# Docker health check
docker ps | grep operator-dashboard

# Kubernetes health check
kubectl get pods -n production
kubectl describe pod <pod-name> -n production
```

### Logging

```bash
# View application logs
kubectl logs -f deployment/operator-dashboard -n production

# Stream logs from all pods
kubectl logs -f deployment/operator-dashboard -n production --all-containers=true

# Export logs
kubectl logs deployment/operator-dashboard -n production > logs.txt
```

### Metrics & Monitoring

**Sentry**: Error tracking and performance
```
https://sentry.example.com/organizations/synaptix/issues/
```

**Prometheus**: Application metrics
```
http://prometheus:9090/
```

**Grafana**: Dashboards
```
http://grafana:3000/
```

### Common Issues

**Issue**: White blank page
- **Cause**: JavaScript bundle failed to load or parse error
- **Fix**: Check browser console, check Sentry, verify API connectivity

**Issue**: Login fails
- **Cause**: Backend API unreachable or misconfigured
- **Fix**: Check VITE_API_BASE_URL, verify backend health, check network

**Issue**: Slow page load
- **Cause**: Large bundle, slow API, no caching
- **Fix**: Check bundle size (npm run build --report), check Core Web Vitals, enable compression

**Issue**: Mobile layout broken
- **Cause**: Viewport meta tag missing or CSS not responsive
- **Fix**: Check <head> for viewport tag, verify Tailwind responsive classes

---

## Support

For issues or questions:
1. Check Sentry for error details
2. Review application logs
3. Contact DevOps team
4. Create GitHub issue with reproduction steps

---

**Version**: 1.0.0  
**Last Updated**: 2026-07-04  
**Maintainer**: DevOps Team
