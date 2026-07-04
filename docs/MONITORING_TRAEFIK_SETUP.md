# Monitoring Services via Traefik

**Status:** ✅ Configured  
**Last Updated:** 2026-07-03

---

## Overview

Prometheus, Grafana, and AlertManager are now accessible through Traefik reverse proxy, providing a unified interface for monitoring services.

---

## Access URLs

### Development Environment

```
Prometheus:   http://localhost/prometheus
Grafana:      http://localhost/grafana
AlertManager: http://localhost/alertmanager
```

**Note:** Traefik is required to be running. If accessing directly:
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000
- AlertManager: http://localhost:9093

### Production Environment

```
Prometheus:   https://monitoring.synaptixplay.com/prometheus
Grafana:      https://monitoring.synaptixplay.com/grafana
AlertManager: https://monitoring.synaptixplay.com/alertmanager
```

**Security:** All require TLS and basic HTTP authentication

---

## Traefik Configuration

### Development (`docker/traefik/dynamic.yml`)

Routes configured for development with HTTP (unencrypted):

```yaml
routers:
  prometheus:
    rule: "PathPrefix(`/prometheus`)"
    service: prometheus
    middlewares: [prometheus-strip]

  grafana:
    rule: "PathPrefix(`/grafana`)"
    service: grafana
    middlewares: [grafana-strip]

  alertmanager:
    rule: "PathPrefix(`/alertmanager`)"
    service: alertmanager
    middlewares: [alertmanager-strip]

services:
  prometheus:
    loadBalancer:
      servers:
        - url: "http://prometheus:9090"
  grafana:
    loadBalancer:
      servers:
        - url: "http://grafana:3000"
  alertmanager:
    loadBalancer:
      servers:
        - url: "http://alertmanager:9093"
```

**Priority Ordering:**
- Backend API: priority 10 (highest)
- Monitoring Services: priority 9
- Operator Dashboard: priority 1 (lowest, catch-all)

### Production (`docker/traefik/dynamic.prod.yml`)

Routes configured for production with HTTPS and authentication:

```yaml
routers:
  prometheus:
    rule: "Host(`monitoring.synaptixplay.com`) && PathPrefix(`/prometheus`)"
    middlewares: [prometheus-strip, auth-basic]
    tls: {}

  grafana:
    rule: "Host(`monitoring.synaptixplay.com`) && PathPrefix(`/grafana`)"
    middlewares: [grafana-strip, auth-basic]
    tls: {}

  alertmanager:
    rule: "Host(`monitoring.synaptixplay.com`) && PathPrefix(`/alertmanager`)"
    middlewares: [alertmanager-strip, auth-basic]
    tls: {}
```

**Features:**
- All monitoring on single subdomain: `monitoring.synaptixplay.com`
- Basic HTTP authentication on all routes
- TLS/HTTPS enforced
- Path stripping removes `/prometheus`, `/grafana`, `/alertmanager` before forwarding

---

## Setup Instructions

### Development (No Additional Setup Needed)

1. Ensure Traefik is running:
   ```bash
   docker compose up traefik
   ```

2. Access services via Traefik:
   ```bash
   curl http://localhost/prometheus
   curl http://localhost/grafana
   curl http://localhost/alertmanager
   ```

3. If Traefik not running, access directly on respective ports:
   ```bash
   # Falls back to direct access
   curl http://localhost:9090      # Prometheus
   curl http://localhost:3000      # Grafana
   curl http://localhost:9093      # AlertManager
   ```

### Production Setup

#### Step 1: Configure DNS

Add DNS entry pointing to production server:

```
monitoring.synaptixplay.com  A  173.255.235.67  (Production Linode IP)
```

#### Step 2: Generate Basic Auth Credentials

Generate bcrypt-hashed credentials for basic authentication:

```bash
# Install htpasswd utility if needed
sudo apt-get install apache2-utils

# Generate credentials (will prompt for password)
htpasswd -c docker/traefik/.htpasswd admin

# View generated hash
cat docker/traefik/.htpasswd
# Output: admin:$2y$05$abcdef...
```

#### Step 3: Update Traefik Configuration

Update `docker/traefik/dynamic.prod.yml` with the generated hash:

```yaml
middlewares:
  auth-basic:
    basicAuth:
      users:
        - "admin:$2y$05$YOUR_GENERATED_HASH_HERE"
```

#### Step 4: Deploy

```bash
# Pull latest configuration
git pull origin main

# Restart Traefik
docker compose restart traefik

# Verify routing
curl -u admin:password https://monitoring.synaptixplay.com/prometheus
curl -u admin:password https://monitoring.synaptixplay.com/grafana
curl -u admin:password https://monitoring.synaptixplay.com/alertmanager
```

---

## Middleware Configuration

### Path Stripping

Each monitoring service has a corresponding path-stripping middleware that removes the route prefix before forwarding to the backend:

```yaml
middlewares:
  prometheus-strip:
    stripPrefix:
      prefixes:
        - /prometheus

  grafana-strip:
    stripPrefix:
      prefixes:
        - /grafana

  alertmanager-strip:
    stripPrefix:
      prefixes:
        - /alertmanager
```

**How it works:**

```
Request:  GET http://localhost/prometheus/api/v1/query
          ↓ (stripPrefix removes /prometheus)
Forwarded: GET http://prometheus:9090/api/v1/query
```

### Basic Authentication (Production Only)

Production routes include `auth-basic` middleware requiring credentials:

```yaml
middlewares:
  auth-basic:
    basicAuth:
      users:
        - "admin:$2y$05$bcrypt_hash_here"
```

To log in, provide credentials in request:

```bash
curl -u admin:password https://monitoring.synaptixplay.com/grafana
```

Or in headers:

```bash
curl -H "Authorization: Basic $(echo -n 'admin:password' | base64)" \
     https://monitoring.synaptixplay.com/grafana
```

---

## Troubleshooting

### Services Not Accessible Through Traefik

1. Verify Traefik is running:
   ```bash
   docker compose ps traefik
   ```

2. Check Traefik logs:
   ```bash
   docker compose logs traefik
   ```

3. Verify route is configured:
   ```bash
   docker compose exec traefik cat /etc/traefik/dynamic.yml
   ```

4. Test backend service directly:
   ```bash
   curl http://prometheus:9090
   curl http://grafana:3000
   curl http://alertmanager:9093
   ```

### Production Auth Not Working

1. Verify basic auth credentials are set:
   ```bash
   grep "auth-basic" docker/traefik/dynamic.prod.yml
   ```

2. Test with credentials:
   ```bash
   curl -u admin:password https://monitoring.synaptixplay.com/grafana
   ```

3. Generate new credentials if needed:
   ```bash
   htpasswd -c docker/traefik/.htpasswd admin
   ```

4. Restart Traefik:
   ```bash
   docker compose restart traefik
   ```

### CORS Issues with Prometheus/Grafana

If cross-origin requests fail, Traefik may need additional headers:

```yaml
middlewares:
  monitoring-headers:
    headers:
      accessControlAllowOriginList:
        - "*"
      accessControlAllowMethods:
        - GET
        - POST
        - OPTIONS
```

Add to routes if needed:

```yaml
routers:
  prometheus:
    middlewares: [prometheus-strip, monitoring-headers]
```

---

## Environment Variables

No additional environment variables needed for Traefik routing. The configuration is static in:
- Development: `docker/traefik/dynamic.yml`
- Production: `docker/traefik/dynamic.prod.yml`

However, monitoring services themselves require environment variables (see respective setup guides):
- **AlertManager**: `SLACK_WEBHOOK_URL`
- **Sentry**: `SENTRY_DSN`

These should be set in:
- `.env.staging` (for staging environment)
- `.env.production` (for production environment)
- `.env` or `.env.example` (for development)

---

## Port Mapping

### Services on Docker Network

| Service      | Internal Port | Traefik Route      | Direct Access    |
|--------------|---------------|--------------------|------------------|
| Prometheus   | 9090          | /prometheus        | :9090            |
| Grafana      | 3000          | /grafana           | :3000            |
| AlertManager | 9093          | /alertmanager      | :9093            |

### Traefik Ports

| Environment | HTTP Port | HTTPS Port | Dashboard |
|-------------|-----------|-----------|-----------|
| Development | 80        | 443       | :8080     |
| Production  | 80        | 443       | N/A       |

---

## DNS Configuration (Production)

Required DNS records for production:

```
Host                              Type    Value
monitoring.synaptixplay.com       A       173.255.235.67 (Linode IP)
monitoring.synaptixplay.com       AAAA    2600:3c03::2000:09ff:fed3:6e1b (IPv6)
```

Or use Cloudflare Tunnel for authentication:

```bash
cloudflared tunnel create monitoring
cloudflared tunnel route dns monitoring monitoring.synaptixplay.com
```

---

## Next Steps

1. ✅ Traefik routes configured
2. → Test in development environment
3. → Generate production credentials (.htpasswd)
4. → Configure DNS for monitoring.synaptixplay.com
5. → Deploy to production
6. → Monitor Traefik access logs

---

## References

- [Traefik Path Stripping](https://doc.traefik.io/traefik/middlewares/http/stripprefix/)
- [Traefik Basic Auth](https://doc.traefik.io/traefik/middlewares/http/basicauth/)
- [Traefik TLS Configuration](https://doc.traefik.io/traefik/routing/entrypoints/#tls)
- [Dynamic Configuration File Provider](https://doc.traefik.io/traefik/providers/file/)

---

**Status:** Ready for deployment  
**Last Updated:** 2026-07-03  
**Maintained By:** DevOps Team
