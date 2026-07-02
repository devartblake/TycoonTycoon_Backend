# Deployment Guide

## Docker Deployment

### Building the React Dashboard Image

```bash
docker build \
  -f docker/Dockerfile.dashboard-react \
  --build-arg VITE_API_BASE_URL=https://api.synaptixplay.com \
  --build-arg VITE_APP_ENV=production \
  -t synaptix/operator-dashboard-react:latest \
  .
```

### Docker Compose Configuration

Add to your `docker-compose.yml`:

```yaml
operator-dashboard-react:
  build:
    context: .
    dockerfile: docker/Dockerfile.dashboard-react
    args:
      VITE_API_BASE_URL: ${API_BASE_URL:-https://api.synaptixplay.com}
      VITE_APP_ENV: ${APP_ENV:-production}
  container_name: synaptix_operator_dashboard_react
  restart: unless-stopped
  expose:
    - "8300"
  healthcheck:
    test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8300/index.html"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 10s
  networks:
    - synaptix-net
  labels:
    - "traefik.enable=true"
    - "traefik.http.routers.operator-dashboard-react.rule=Host(`admin.synaptixplay.com`)"
    - "traefik.http.routers.operator-dashboard-react.entrypoints=websecure"
    - "traefik.http.routers.operator-dashboard-react.tls.certresolver=letsencrypt"
    - "traefik.http.services.operator-dashboard-react.loadbalancer.server.port=8300"
```

### Environment Variables

Create a `.env` file for docker-compose:

```env
# API Configuration
API_BASE_URL=https://api.synaptixplay.com

# Application Environment
APP_ENV=production
```

### Running Locally

```bash
# Development with Docker
docker build -f docker/Dockerfile.dashboard-react \
  --build-arg VITE_API_BASE_URL=http://localhost:5000 \
  --build-arg VITE_APP_ENV=development \
  -t synaptix/operator-dashboard-react:dev .

docker run -p 8300:8300 synaptix/operator-dashboard-react:dev
```

## Traefik Configuration

The React dashboard is automatically routed via Traefik using the labels in `docker-compose.yml`.

**Domain:** `admin.synaptixplay.com`
**Port (internal):** 8300
**Protocol:** HTTPS (Let's Encrypt certificate)

### Traefik Middleware (optional)

To add basic auth to the admin dashboard:

```yaml
middlewares:
  operator-dashboard-auth:
    basicAuth:
      users:
        - "admin:$apr1$....."  # htpasswd-hashed credentials
```

Then add to the router labels:

```yaml
- "traefik.http.routers.operator-dashboard-react.middlewares=operator-dashboard-auth"
```

## Production Checklist

- [ ] Environment variables configured for production API URL
- [ ] Docker image built and pushed to registry
- [ ] Health checks verified (curl http://localhost:8300/health)
- [ ] Traefik routing configured for admin.synaptixplay.com
- [ ] HTTPS certificate installed (Let's Encrypt)
- [ ] Security headers in place (nginx config includes them)
- [ ] Gzip compression enabled
- [ ] Static asset cache-busting configured
- [ ] Backend API endpoints accessible from container network
- [ ] CORS configured on backend to allow admin domain

## Monitoring

### Health Check Endpoint

```bash
curl http://localhost:8300/health
# Returns: "healthy"
```

### Application Logs

```bash
docker logs synaptix_operator_dashboard_react -f
```

### Performance Monitoring

Monitor these metrics in Prometheus/Grafana:

- `nginx_http_requests_total` - Request count
- `nginx_http_request_duration_seconds` - Request latency
- `nginx_http_request_size_bytes` - Request size
- `nginx_http_response_size_bytes` - Response size

## Troubleshooting

### App loads but API calls fail

1. Check backend connectivity:
   ```bash
   docker exec synaptix_operator_dashboard_react curl -I http://backend-api:5000/health
   ```

2. Verify CORS headers:
   ```bash
   curl -H "Origin: https://admin.synaptixplay.com" \
     -H "Access-Control-Request-Method: GET" \
     -I http://localhost:5000/admin/auth/me
   ```

3. Check environment variable in container:
   ```bash
   docker exec synaptix_operator_dashboard_react env | grep VITE
   ```

### Page shows 404 for SPA routes

1. Verify nginx configuration is loaded:
   ```bash
   docker exec synaptix_operator_dashboard_react nginx -T
   ```

2. Check that index.html exists:
   ```bash
   docker exec synaptix_operator_dashboard_react ls -la /usr/share/nginx/html/
   ```

### High memory usage

1. Check nginx worker processes:
   ```bash
   docker exec synaptix_operator_dashboard_react ps aux
   ```

2. Adjust nginx.conf worker connections if needed:
   ```nginx
   events {
     worker_connections 1024;
   }
   ```

## Rolling Updates

```bash
# Build new version
docker build -f docker/Dockerfile.dashboard-react -t synaptix/operator-dashboard-react:v1.2.0 .

# Push to registry
docker push synaptix/operator-dashboard-react:v1.2.0

# Update compose and restart
docker-compose -f docker/compose.yml up -d operator-dashboard-react

# Verify rollout
docker-compose -f docker/compose.yml ps operator-dashboard-react
```

## Rollback

```bash
# Revert to previous image
docker-compose -f docker/compose.yml up -d operator-dashboard-react --detach

# Or manually specify version
docker run -p 8300:8300 synaptix/operator-dashboard-react:v1.1.0
```
