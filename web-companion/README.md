# Trivia Tycoon Web Companion App

React + TypeScript web application for Trivia Tycoon / Synaptix. A standalone web product complementing the Flutter mobile app, with exclusive features for desktop engagement, competitive play, and content creation.

## Quick Start

### Prerequisites
- Node.js 22+ 
- npm 10+
- Docker & Docker Compose (for local environment with backend)

### Development (Without Docker)

```bash
# Install dependencies
npm install

# Start dev server (HMR enabled)
npm run dev

# Open browser to http://localhost:5173
```

### Development (With Docker)

```bash
# Build and start dev container
docker-compose up web-companion

# Open browser to http://localhost:5173
```

### With Full Backend Stack

```bash
# Start web companion + backend services
docker-compose --profile with-backend up

# Services will be available at:
# - Web: http://localhost:5173
# - Backend API: http://localhost:5000
# - PostgreSQL: localhost:5432
# - Redis: localhost:6379
```

## Project Structure

```
src/
├── app/                 # React Router, app shell, providers
├── core/                # Infrastructure (API client, env config, storage)
├── stores/              # Zustand global state
├── features/            # Feature modules (auth, quiz, skills, etc.)
├── components/          # Shared UI components
├── hooks/               # Custom React hooks
├── lib/                 # Pure utilities (game engine, crypto, etc.)
└── assets/              # Images, audio, icons
```

## Available Scripts

```bash
# Development
npm run dev           # Start Vite dev server with HMR

# Build & Deploy
npm run build         # Production build
npm run preview       # Preview production build locally

# Linting & Formatting
npm run lint          # ESLint
npm run format        # Prettier
npm run type-check    # TypeScript check

# Testing (when available)
npm run test          # Vitest unit tests
npm run test:e2e      # Playwright E2E tests
```

## Environment Configuration

Configuration uses `.env` files matching the deployment environment:

- `.env.local` — Development (local Docker backend at http://localhost:5000)
- `.env.staging` — Staging environment (https://staging-api.synaptixplay.com)
- `.env.production` — Production (https://api.synaptixplay.com)

### Environment Variables

```env
# API & Real-time
VITE_API_URL=http://localhost:5000
VITE_WS_URL=ws://localhost:5000
VITE_SIGNALR_URL=http://localhost:5000/hubs

# Payments
VITE_STRIPE_KEY=pk_test_xxx

# Auth
VITE_GOOGLE_CLIENT_ID=xxx.apps.googleusercontent.com

# Misc
VITE_COMPLIANCE_URL=http://localhost:3000/compliance
VITE_APP_VERSION=1.0.0-dev
```

## Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | React 18 + TypeScript |
| **Build** | Vite 5 |
| **Routing** | React Router v6 |
| **Styling** | Tailwind CSS + shadcn/ui |
| **State** | Zustand (global) + TanStack Query (server) |
| **API** | Axios with interceptors |
| **Real-time** | @microsoft/signalr |
| **Payments** | Stripe.js |
| **Auth** | @react-oauth/google |
| **Animations** | Framer Motion |
| **Charts** | Recharts |
| **Local Storage** | Dexie.js (IndexedDB) |
| **Audio** | Howler.js |
| **Testing** | Vitest + React Testing Library |

## Docker

### Production Build

```bash
# Build production image
docker build -t synaptix-web-companion:latest .

# Run container
docker run -p 3000:3000 synaptix-web-companion:latest
```

The production build uses Nginx to serve the static build artifacts with:
- Gzip compression
- Cache headers (30-day for assets, no-cache for index.html)
- Security headers (X-Frame-Options, CSP, etc.)
- SPA routing (fallback to index.html)

## Development Workflow

1. **Branch**: Create feature branch from `main`
2. **Code**: Write TypeScript + React with full type safety
3. **Test**: Run unit tests locally
4. **Commit**: Follow conventional commit format
5. **PR**: Create pull request, await review
6. **Deploy**: Merge to `main` triggers CI/CD to staging

## Onboarding

For detailed development plan, architecture decisions, and feature roadmap, see:
- [Web Companion Development Plan](../../docs/web-companion/WEB_COMPANION_DEVELOPMENT_PLAN.md)
- [Feature Scope & Architecture](../../docs/web-companion/)

## Contributing

### Code Style

- Use TypeScript for type safety
- Follow ESLint rules (checked in pre-commit)
- Format with Prettier (auto-run in pre-commit)
- Write React hooks, not classes
- Prefer composition over inheritance

### State Management Guidelines

- **Global UI state** → `useUIStore` (Zustand)
- **User auth** → `useAuthStore` (Zustand)
- **Server state** → `useQuery` / `useMutation` (TanStack Query)
- **Component state** → `useState` (React hooks)

### API Integration

All API calls go through `src/core/api/client.ts`:

```typescript
import { apiClient } from '@/core/api/client';

// GET
const { data: profile } = await apiClient.get('/api/profile');

// POST
const { data: result } = await apiClient.post('/api/quiz/complete', { score: 100 });
```

## Troubleshooting

### Port 5173 already in use

```bash
# Kill process using port 5173
npx kill-port 5173

# Or specify different port
npm run dev -- --port 5174
```

### CORS errors connecting to backend

Ensure backend is running and `/hubs` endpoint is accessible:

```bash
# Test backend connectivity
curl -X GET http://localhost:5000/health
```

### Hot Module Reload (HMR) not working in Docker

The Dockerfile.dev exposes the dev server on `0.0.0.0`. Ensure Docker is configured to allow port 5173 access from host.

## Performance

- **Target Lighthouse score**: 90+
- **First Contentful Paint**: < 2.5s
- **Largest Contentful Paint**: < 4s
- **Cumulative Layout Shift**: < 0.1

Monitor with:
```bash
npm run build && npm run preview
# Open DevTools → Lighthouse tab
```

## Next Steps

- [ ] Set up .env files with actual backend URLs
- [ ] Create first feature (login/auth)
- [ ] Integrate with backend API
- [ ] Set up E2E tests
- [ ] Deploy to staging environment

---

**Started**: 2026-06-25  
**Phase 1 Target**: 2026-07-28  
**Expected Launch**: 2026-12-08  

For questions or issues, see the [development plan](../../docs/web-companion/WEB_COMPANION_DEVELOPMENT_PLAN.md).
