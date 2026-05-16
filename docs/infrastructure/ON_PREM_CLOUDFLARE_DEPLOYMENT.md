# On-Prem Linux Deployment with Cloudflare (Ubuntu/Fedora)

This guide helps you run **TycoonTycoon_Backend** on your own Linux server(s), while exposing it safely through Cloudflare.

It supports two edge patterns:

1. **Cloudflare DNS + direct public ingress** (open 80/443 to your server, Traefik handles TLS)
2. **Cloudflare Tunnel (recommended for home/small office/on-prem)** (no inbound ports required)

---

## 1) Deployment architecture

Recommended production path in this repo:

- `docker/compose.yml` = full stack
- `docker/compose.prod.yml` = production hardening (no direct DB ports, TLS routing labels, disabled dev tooling)
- `docker/compose.cloudflare-tunnel.yml` = optional Cloudflare Tunnel sidecar

Traffic flow with Tunnel:

`User -> Cloudflare Edge -> cloudflared container -> Traefik -> backend-api/operator-dashboard`

---

## 2) Host requirements

Minimum host profile (single-node start):

- 4 vCPU
- 16 GB RAM (Elasticsearch + app stack)
- 100+ GB SSD
- Linux kernel with cgroups v2 (modern Ubuntu/Fedora defaults)

Recommended OS packages:

- Docker Engine + Compose plugin
- `git`, `make`, `curl`, `jq`, `ufw` or `firewalld`

### Ubuntu quick install

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg git make jq ufw

sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo $VERSION_CODENAME) stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker "$USER"
```

### Fedora quick install

```bash
sudo dnf -y install dnf-plugins-core
sudo dnf config-manager --add-repo https://download.docker.com/linux/fedora/docker-ce.repo
sudo dnf -y install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin git make jq
sudo systemctl enable --now docker
sudo usermod -aG docker "$USER"
```

Log out/in after group change.

---

## 3) Clone and configure

```bash
git clone <your-fork-or-origin-url>
cd TycoonTycoon_Backend
cp docker/.env.example docker/.env
```

Then edit `docker/.env`:

- Replace all default passwords/secrets.
- Set:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `JWT_SECRET_KEY=<long-random-secret-32+ chars>`
  - `ADMIN_OPS_KEY=<long-random-secret>`
  - `SUPER_ADMIN_EMAIL` / `SUPER_ADMIN_PASSWORD`
  - `DOMAIN=<your-domain>` (for DNS/direct ingress mode)
  - `ACME_EMAIL=<you@domain>` (for DNS/direct ingress mode)
  - `CLOUDFLARE_TUNNEL_TOKEN=<token>` (for tunnel mode)

---

## 4) Choose your Cloudflare strategy

## Option A — Cloudflare DNS + direct public ingress

Use when your server can accept inbound `80/tcp` and `443/tcp`.

1. In Cloudflare DNS, add proxied records:
   - `A`/`AAAA` for `yourdomain.com` -> your public server IP
   - `A`/`AAAA` for `api.yourdomain.com` -> your public server IP
2. Keep proxy enabled (orange cloud).
3. Start stack:

```bash
docker compose -f docker/compose.yml -f docker/compose.prod.yml up -d --build
```

4. Confirm containers and cert issuance:

```bash
docker compose -f docker/compose.yml -f docker/compose.prod.yml ps
docker logs tycoon_traefik --tail 200
```

### Firewall (direct ingress)

- Allow: `22`, `80`, `443`
- Deny public access to data-plane ports (`5432`, `6379`, `27017`, `9200`, etc.)

---

## Option B — Cloudflare Tunnel (recommended)

Use when you **do not** want to open inbound ports.

1. In Cloudflare Zero Trust dashboard:
   - Create a Tunnel.
   - Add public hostnames:
     - `yourdomain.com` -> `http://traefik:80`
     - `api.yourdomain.com` -> `http://traefik:80`
   - Copy tunnel token.
2. Put token in `docker/.env` as `CLOUDFLARE_TUNNEL_TOKEN=...`.
3. Start stack with tunnel override:

```bash
docker compose \
  -f docker/compose.yml \
  -f docker/compose.prod.yml \
  -f docker/compose.cloudflare-tunnel.yml \
  up -d --build
```

4. Verify tunnel status:

```bash
docker logs tycoon_cloudflared --tail 200
```

### Firewall (tunnel mode)

- Allow only `22` (and optional outbound restrictions per policy).
- No inbound `80/443` required.

---

## 5) First-time migration and health checks

Run migrations explicitly in production:

```bash
docker compose -f docker/compose.yml -f docker/compose.prod.yml run --rm migration
```

Check health:

```bash
docker compose -f docker/compose.yml -f docker/compose.prod.yml ps
curl -fsS http://localhost:${BACKEND_HTTP_PORT:-5000}/healthz
```

If API port is not exposed in your final production profile, run health checks from inside the Docker network or through your public domain.

---

## 6) Operational hardening checklist

- Change every secret in `.env` from defaults.
- Store `.env` in a secret manager if possible (Vault, SOPS, etc.).
- Enable automatic host security updates.
- Back up persistent volumes (`postgres_data`, `mongodb_data`, `minio_data`, etc.).
- Add log shipping/monitoring (Grafana, Prometheus, ELK, or managed equivalent).
- Restrict SSH (keys only, fail2ban, no root login).
- Consider Cloudflare Access for operator dashboard (`yourdomain.com`) to enforce SSO/MFA.

---

## 7) Update/rollback workflow

### Update

```bash
git pull
docker compose -f docker/compose.yml -f docker/compose.prod.yml pull
docker compose -f docker/compose.yml -f docker/compose.prod.yml up -d --build
docker compose -f docker/compose.yml -f docker/compose.prod.yml run --rm migration
```

(Include `-f docker/compose.cloudflare-tunnel.yml` if tunnel mode is enabled.)

### Rollback

- Revert git commit/tag.
- Rebuild and redeploy previous image set.
- Restore DB snapshot if schema-breaking migration was applied.

---

## 8) Fedora vs Ubuntu notes

- Both are valid for production.
- **Ubuntu LTS** usually has broader hosting/vendor examples.
- **Fedora** is great for newer kernels/userspace, but plan for more frequent OS updates.
- For either distro, pin Docker/Compose versions and document a reproducible bootstrap script.

---

## 9) Quick command summary

Direct ingress:

```bash
docker compose -f docker/compose.yml -f docker/compose.prod.yml up -d --build
docker compose -f docker/compose.yml -f docker/compose.prod.yml run --rm migration
```

Cloudflare Tunnel:

```bash
docker compose \
  -f docker/compose.yml \
  -f docker/compose.prod.yml \
  -f docker/compose.cloudflare-tunnel.yml \
  up -d --build

docker compose -f docker/compose.yml -f docker/compose.prod.yml run --rm migration
```


---

## 10) Flutter frontend: do you need to add it to Docker?

Short answer: **not for Flutter mobile apps**.

- If your frontend is iOS/Android Flutter, users run it on their devices; your server only hosts the backend API.
- You only need a server-hosted frontend container if you are shipping a **Flutter Web** build.

### If you are using Flutter Mobile

- Keep current backend stack as-is.
- Point the app's API base URL to your Cloudflare endpoint (for example `https://api.yourdomain.com`).
- In backend config, set CORS origins only for web clients; mobile apps are not browser-origin constrained.

### If you are using Flutter Web

You can host Flutter web static files behind the same Cloudflare + Traefik edge:

1. Build web bundle in your Flutter repo:

```bash
flutter build web --release
```

2. Serve `build/web` via Nginx/Caddy (container or host).
3. Route `yourdomain.com` to that web host and `api.yourdomain.com` to this backend stack.
4. Set backend CORS to allow `https://yourdomain.com`.

This keeps one clean public domain pair:

- `https://yourdomain.com` -> Flutter Web
- `https://api.yourdomain.com` -> Tycoon backend API
