# Traefik Configuration for React Operator Dashboard

## Overview

The React operator dashboard is deployed as a Docker container and routed through Traefik reverse proxy. This guide covers the Traefik configuration needed to route traffic to the React dashboard.

## Docker Compose Labels

The React dashboard service in `docker/compose.yml` includes Traefik labels that automatically configure routing:

```yaml
operator-dashboard-react:
  build:
    context: ..
    dockerfile: docker/Dockerfile.dashboard-react
  labels:
    - "traefik.enable=true"
    - "traefik.http.routers.operator-dashboard-react.rule=Host(`admin.synaptixplay.com`)"
    - "traefik.http.routers.operator-dashboard-react.entrypoints=websecure"
    - "traefik.http.routers.operator-dashboard-react.tls.certresolver=letsencrypt"
    - "traefik.http.services.operator-dashboard-react.loadbalancer.server.port=8300"
```

## Configuration Details

### Domain Routing

- **Hostname**: `admin.synaptixplay.com`
- **Protocol**: HTTPS (automatic Let's Encrypt certificate)
- **Port**: 8300 (internal container port)
- **Entry Point**: `websecure` (HTTPS listener)

### TLS Certificate

- **Certificate Resolver**: `letsencrypt`
- **Certificate Type**: Automatically provisioned for `admin.synaptixplay.com`
- **Renewal**: Automatic (Let's Encrypt handles renewal 30 days before expiry)

### Load Balancer

- **Server Port**: 8300 (nginx listening port inside container)
- **Strategy**: Round-robin (single backend in basic setup)

## Traefik Dynamic Configuration

If using `docker/traefik/dynamic.yml`, add this configuration:

```yaml
http:
  routers:
    operator-dashboard-react:
      rule: "Host(`admin.synaptixplay.com`)"
      service: operator-dashboard-react
      entryPoints:
        - websecure
      tls:
        certResolver: letsencrypt

  services:
    operator-dashboard-react:
      loadBalancer:
        servers:
          - url: "http://operator-dashboard-react:8300"
```

## Middleware (Optional)

### Basic Authentication

To add basic auth to the dashboard:

```yaml
middlewares:
  operator-dashboard-auth:
    basicAuth:
      users:
        - "admin:$apr1$h6VJ3jqQ$u3VH3c4qWiA7A6D6VjL/N0"  # htpasswd hash
        - "operator:$apr1$d9hr9HqQ$EYbVogul75WJnxQsvrichN"

http:
  routers:
    operator-dashboard-react:
      rule: "Host(`admin.synaptixplay.com`)"
      service: operator-dashboard-react
      middlewares:
        - operator-dashboard-auth
```

Generate htpasswd hashes:
```bash
htpasswd -c auth-file admin
# (Enter password at prompt)
```

### Rate Limiting

To protect against brute force:

```yaml
middlewares:
  operator-dashboard-ratelimit:
    rateLimit:
      average: 100
      burst: 50
      period: 1m

http:
  routers:
    operator-dashboard-react:
      middlewares:
        - operator-dashboard-ratelimit
```

### Security Headers

```yaml
middlewares:
  operator-dashboard-headers:
    headers:
      contentSecurityPolicy: "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'"
      frameDeny: true
      sslRedirect: true
      stsSeconds: 315360000
      stsIncludeSubdomains: true
```

## DNS Configuration

Ensure your DNS provider points `admin.synaptixplay.com` to your Traefik ingress:

```dns
admin.synaptixplay.com A 203.0.113.1  # Your server IP
```

Or with CNAME:
```dns
admin.synaptixplay.com CNAME synaptix-api.example.com
```

## Testing the Configuration

### Check Traefik Dashboard

Access the Traefik dashboard to verify the route is configured:

```bash
# Local development
curl http://localhost:8080/dashboard/

# Or navigate to http://localhost:8080/ in your browser
```

Look for:
- Route: `operator-dashboard-react`
- Rule: `Host(admin.synaptixplay.com)`
- Service: `operator-dashboard-react`
- Status: Green (healthy)

### Test the Route

```bash
# Test HTTPS connectivity
curl -I https://admin.synaptixplay.com

# Test with basic auth (if configured)
curl -I -u admin:password https://admin.synaptixplay.com

# Check certificate
openssl s_client -connect admin.synaptixplay.com:443

# View certificate info
curl -vI https://admin.synaptixplay.com 2>&1 | grep -A 5 "certificate"
```

## Troubleshooting

### Route not appearing in Traefik dashboard

1. Check service labels are correct in compose.yml:
   ```bash
   docker inspect synaptix_operator_dashboard_react | grep -A 10 "Labels"
   ```

2. Verify service is running:
   ```bash
   docker-compose ps operator-dashboard-react
   ```

3. Check Traefik logs:
   ```bash
   docker logs -f synaptix_traefik
   ```

### Certificate not provisioning

1. Verify DNS is resolving:
   ```bash
   nslookup admin.synaptixplay.com
   ```

2. Check Let's Encrypt logs in Traefik:
   ```bash
   docker logs -f synaptix_traefik | grep -i "letsencrypt"
   ```

3. Verify port 80 and 443 are accessible from Traefik:
   ```bash
   docker exec synaptix_traefik nc -zv 0.0.0.0 80
   docker exec synaptix_traefik nc -zv 0.0.0.0 443
   ```

### React app loads but API calls fail (CORS)

Ensure the backend API CORS configuration allows the React dashboard domain:

In `docker/.env`:
```env
CORS_ORIGIN_5=https://admin.synaptixplay.com
```

Or add to backend environment:
```yaml
Cors__AllowedOrigins__5: "https://admin.synaptixplay.com"
```

### 502 Bad Gateway errors

1. Check React container is healthy:
   ```bash
   docker-compose ps operator-dashboard-react
   ```

2. Check Traefik can reach the container:
   ```bash
   docker exec synaptix_traefik curl http://operator-dashboard-react:8300/index.html
   ```

3. Check container logs:
   ```bash
   docker logs synaptix_operator_dashboard_react
   ```

## Production Checklist

- [ ] DNS record points to Traefik ingress
- [ ] Port 80 and 443 accessible from internet
- [ ] Let's Encrypt certificate provisioned (check Traefik dashboard)
- [ ] HTTPS redirect working (HTTP → HTTPS)
- [ ] React container is healthy
- [ ] Backend API CORS allows admin domain
- [ ] Security headers configured
- [ ] Rate limiting enabled (optional but recommended)
- [ ] Basic auth or other security layer enabled
- [ ] Logs are being collected

## Multi-Environment Setup

### Development

```yaml
labels:
  - "traefik.http.routers.operator-dashboard-react-dev.rule=Host(`admin-dev.synaptixplay.com`)"
  - "traefik.http.routers.operator-dashboard-react-dev.entrypoints=web"  # HTTP only
```

### Staging

```yaml
labels:
  - "traefik.http.routers.operator-dashboard-react-staging.rule=Host(`admin-staging.synaptixplay.com`)"
  - "traefik.http.routers.operator-dashboard-react-staging.entrypoints=websecure"
```

### Production

```yaml
labels:
  - "traefik.http.routers.operator-dashboard-react.rule=Host(`admin.synaptixplay.com`)"
  - "traefik.http.routers.operator-dashboard-react.entrypoints=websecure"
```

## Performance Optimization

### Enable Compression

```yaml
middlewares:
  operator-dashboard-compress:
    compress:
      minResponseBodyBytes: 1024

http:
  routers:
    operator-dashboard-react:
      middlewares:
        - operator-dashboard-compress
```

### Cache Static Assets

```yaml
middlewares:
  operator-dashboard-cache:
    headers:
      customResponseHeaders:
        Cache-Control: "public, max-age=31536000, immutable"  # 1 year for assets

# Apply only to asset paths
http:
  routers:
    operator-dashboard-assets:
      rule: "Host(`admin.synaptixplay.com`) && PathPrefix(`/assets/`)"
      middlewares:
        - operator-dashboard-cache
```

## Rolling Updates

To update the React dashboard without downtime:

1. Build new image:
   ```bash
   docker build -f docker/Dockerfile.dashboard-react -t synaptix/operator-dashboard-react:v1.1.0 .
   ```

2. Update compose file to use new tag

3. Recreate service (Traefik handles traffic seamlessly):
   ```bash
   docker-compose up -d --no-deps --build operator-dashboard-react
   ```

## Related Documentation

- [DEPLOYMENT.md](./DEPLOYMENT.md) — Complete deployment guide
- [DOCKER-REACT.md](./DOCKER-REACT.md) — Docker setup and commands
- `docker/compose.yml` — Service definitions with Traefik labels
- `docker/traefik/dynamic.yml` — Dynamic routing configuration
