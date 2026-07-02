# React Operator Dashboard — Docker Setup

## Quick Start

### Build the React Dashboard

```bash
docker build \
  -f docker/Dockerfile.dashboard-react \
  --build-arg VITE_API_BASE_URL=http://localhost:5000 \
  --build-arg VITE_APP_ENV=development \
  -t synaptix/operator-dashboard-react:latest \
  .
```

### Run Locally

```bash
docker run -p 8300:8300 synaptix/operator-dashboard-react:latest
```

Access at: `http://localhost:8300`

## Docker Compose Integration

The React dashboard is already configured in `docker/compose.yml`:

```yaml
operator-dashboard-react:
  build:
    context: ..
    dockerfile: docker/Dockerfile.dashboard-react
    args:
      VITE_API_BASE_URL: "${REACT_API_BASE_URL:-http://backend-api:5000}"
      VITE_APP_ENV: "${REACT_APP_ENV:-production}"
  container_name: synaptix_operator_dashboard_react
  restart: unless-stopped
  ports:
    - "${REACT_DASHBOARD_PORT:-8300}:8300"
```

### Start Full Stack with React Dashboard

```bash
cd docker/
docker-compose up -d
```

This starts all services including:
- PostgreSQL
- MongoDB
- Redis
- Elasticsearch
- RabbitMQ
- MinIO
- Backend API (.NET)
- Sidecar (FastAPI)
- Crypto Service
- React Operator Dashboard

### View Logs

```bash
# All services
docker-compose logs -f

# Just React dashboard
docker-compose logs -f operator-dashboard-react

# Last 100 lines
docker-compose logs --tail=100 operator-dashboard-react
```

## Environment Variables

Create `.env` in the docker directory:

```env
# React Dashboard Configuration
REACT_API_BASE_URL=http://backend-api:5000
REACT_APP_ENV=production
REACT_DASHBOARD_PORT=8300

# Backend API
BACKEND_HTTP_PORT=5000
BACKEND_GRPC_PORT=5001

# Database passwords (required)
POSTGRES_PASSWORD=your_postgres_password
MONGO_INITDB_ROOT_PASSWORD=your_mongo_password
REDIS_PASSWORD=your_redis_password
ELASTIC_PASSWORD=your_elastic_password
```

## Build Arguments

The Dockerfile accepts build-time arguments to configure the React app:

| Argument | Default | Description |
|----------|---------|-------------|
| `VITE_API_BASE_URL` | `https://api.synaptixplay.com` | Backend API endpoint |
| `VITE_APP_ENV` | `production` | Application environment (development/staging/production) |

Example with custom API URL:

```bash
docker build \
  -f docker/Dockerfile.dashboard-react \
  --build-arg VITE_API_BASE_URL=https://api-staging.synaptixplay.com \
  --build-arg VITE_APP_ENV=staging \
  -t synaptix/operator-dashboard-react:staging \
  .
```

## Development vs Production

### Development Build

```bash
docker build \
  -f docker/Dockerfile.dashboard-react \
  --build-arg VITE_API_BASE_URL=http://localhost:5000 \
  --build-arg VITE_APP_ENV=development \
  -t synaptix/operator-dashboard-react:dev .

docker run -p 8300:8300 synaptix/operator-dashboard-react:dev
```

### Production Build

```bash
docker build \
  -f docker/Dockerfile.dashboard-react \
  --build-arg VITE_API_BASE_URL=https://api.synaptixplay.com \
  --build-arg VITE_APP_ENV=production \
  -t synaptix/operator-dashboard-react:v1.0.0 .

# Tag for registry
docker tag synaptix/operator-dashboard-react:v1.0.0 \
  registry.synaptixplay.com/operator-dashboard-react:v1.0.0

# Push to registry
docker push registry.synaptixplay.com/operator-dashboard-react:v1.0.0
```

## Health Checks

The React container includes a health check that verifies the nginx server is running:

```bash
# Check health status
docker exec synaptix_operator_dashboard_react wget -q -O- http://localhost:8300/index.html

# View compose health
docker-compose ps
```

Expected output:
```
STATUS: healthy
```

## Troubleshooting

### Container won't start

1. Check logs:
   ```bash
   docker logs synaptix_operator_dashboard_react
   ```

2. Verify build arguments were passed:
   ```bash
   docker inspect synaptix_operator_dashboard_react | grep -i "env\|arg"
   ```

3. Check nginx config is loaded:
   ```bash
   docker exec synaptix_operator_dashboard_react nginx -t
   ```

### API calls fail with 404

1. Check the API base URL:
   ```bash
   docker exec synaptix_operator_dashboard_react env | grep VITE
   ```

2. Verify backend is healthy:
   ```bash
   docker-compose ps backend-api
   curl http://localhost:5000/healthz
   ```

3. Check CORS headers (if calling from browser):
   ```bash
   curl -H "Origin: http://localhost:8300" \
        -H "Access-Control-Request-Method: GET" \
        -I http://localhost:5000/admin/auth/me
   ```

### SPA routes return 404

The nginx config includes SPA routing (try_files fallback). Verify:

```bash
docker exec synaptix_operator_dashboard_react cat /etc/nginx/conf.d/default.conf
```

Should include:
```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

### Port already in use

```bash
# Change port in compose
export REACT_DASHBOARD_PORT=8400
docker-compose up -d

# Or kill conflicting process
lsof -i :8300
kill -9 <PID>
```

## Network Access

### Inside Docker Network

From other containers, access the React dashboard at:
- `http://operator-dashboard-react:8300`

Example from another container:
```bash
docker exec some-container curl http://operator-dashboard-react:8300/index.html
```

### From Host

- `http://localhost:8300`

### From Traefik (Production)

When Traefik routing is enabled, access at:
- `https://admin.synaptixplay.com`

## Multi-Stage Build Details

The Dockerfile uses a two-stage build to minimize image size:

**Stage 1: Builder**
- Node.js 20 Alpine
- Installs dependencies
- Runs `npm run build`
- Output: `/app/dist`

**Stage 2: Runtime**
- nginx 1.27 Alpine (~10MB base)
- Copies built assets from Stage 1
- Runs nginx as non-root user
- Includes SPA routing and security headers
- Final image: ~150MB

## Performance Optimization

The nginx config includes:

- **Gzip compression**: All JavaScript, CSS, JSON responses
- **Cache-busting**: index.html has `max-age=0`, assets have `max-age=1y`
- **Security headers**: X-Frame-Options, X-Content-Type-Options, etc.
- **Static file handling**: Optimized for SPA asset serving

## Next Steps

1. **Traefik Routing** — Configure Traefik labels for `admin.synaptixplay.com`
2. **Image Registry** — Push built image to Docker Hub or private registry
3. **CI/CD** — Automate builds on git push
4. **Health Monitoring** — Add Prometheus metrics collection
5. **Phase 2** — Optional UI components for Store CRUD

## Related Documentation

- [DEPLOYMENT.md](./DEPLOYMENT.md) — Complete deployment guide
- [BACKEND_INTEGRATION.md](./BACKEND_INTEGRATION.md) — Backend API setup
- `docker/Dockerfile.dashboard-react` — Build recipe
- `docker/nginx-react.conf` — SPA routing configuration
