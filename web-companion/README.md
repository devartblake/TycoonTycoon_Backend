# Trivia Tycoon Web Companion App

React + TypeScript web application for Trivia Tycoon / Synaptix. A standalone web product complementing the Flutter mobile app, with exclusive features for desktop engagement, competitive play, and content creation.

## 📊 Current Progress

**Phase**: 2 ✅ COMPLETE  
**Status**: API Integration & Core Gameplay Live  
**Last Updated**: 2026-06-25

- ✅ Phase 1: Authentication & Routing (Complete)
- ✅ Phase 2: API Integration & Gameplay (Complete)
- 🔄 Phase 3: UX Polish & Animations (In Planning)
- ⏳ Phase 4-7: Advanced Features (Planned)

**Live Features**:
- 🎮 Full Quiz System (lobby → gameplay → results with real API)
- 🏆 Global Leaderboard with rankings
- 👤 Player Profiles with stats & achievements
- 👥 Friends/Social with challenges
- 🛍️ Store with power-ups & cosmetics
- 📋 Daily/Weekly/Seasonal Missions
- 💰 Economy system (coins & diamonds)
- 🎯 Real-time wallet display

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

| Layer | Technology | Status |
|---|---|---|
| **Framework** | React 18 + TypeScript (strict mode) | ✅ Active |
| **Build** | Vite 5 + PostCSS | ✅ Active |
| **Routing** | React Router v6 | ✅ Active |
| **Styling** | Tailwind CSS v3 + CSS Variables | ✅ Active |
| **State** | Zustand (global store) | ✅ Active |
| **API** | Axios with JWT interceptors | ✅ Active |
| **Icons** | Lucide React | ✅ Active |
| **Real-time** | @microsoft/signalr | ⏳ Phase 4 |
| **Payments** | Stripe.js | ⏳ Phase 5 |
| **Auth** | @react-oauth/google | ⏳ Phase 1 Week 3 |
| **Animations** | Framer Motion | ⏳ Phase 3 |
| **Offline** | Dexie.js (IndexedDB) | ⏳ Phase 3 |
| **Testing** | Vitest + React Testing Library | ⏳ Phase 3 |
| **Analytics** | Google Analytics / Mixpanel | ⏳ Phase 4 |

### Current Bundle Metrics
- **JavaScript**: 485.48 KB (uncompressed)
- **Gzip**: 144.26 KB (compressed)
- **Modules**: 1,927 optimized bundles
- **Build Time**: ~4 seconds

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

## Next Steps (Phase 3)

### High Priority
- [ ] Loading skeletons & empty states
- [ ] Error boundaries & fallback UI
- [ ] Toast notification system
- [ ] Framer Motion animations
- [ ] Offline support (Dexie.js)

### Medium Priority
- [ ] Unit & E2E tests (Vitest + Playwright)
- [ ] Skill tree implementation
- [ ] Seasonal ranking system
- [ ] Challenge/multiplayer invites
- [ ] Daily login rewards

### Low Priority
- [ ] Analytics integration
- [ ] Performance monitoring
- [ ] Advanced animations
- [ ] Code splitting optimization

See [Phase 3 Roadmap](./PHASE_3_ROADMAP.md) for full details.

---

## Documentation

| Document | Purpose |
|----------|---------|
| [PHASE_1_STATUS.md](./PHASE_1_STATUS.md) | ✅ Authentication & routing foundation |
| [PHASE_2_STATUS.md](./PHASE_2_STATUS.md) | ✅ API integration & gameplay complete |
| [PHASE_3_ROADMAP.md](./PHASE_3_ROADMAP.md) | 🔄 UX polish & animations planning |
| [THEMING.md](./THEMING.md) | Theme system documentation |
| [QUICKSTART.md](./QUICKSTART.md) | Quick setup guide |
| [SETUP.md](./SETUP.md) | Detailed setup instructions |

---

## Project Timeline

| Phase | Description | Status | ETA |
|-------|-------------|--------|-----|
| Phase 1 | Auth & Routing | ✅ Complete | 2026-06-30 |
| Phase 2 | API & Gameplay | ✅ Complete | 2026-06-25 |
| Phase 3 | UX & Animations | 🔄 In Planning | 2026-07-22 |
| Phase 4 | Real-time & Advanced | ⏳ Planned | 2026-08-19 |
| Phase 5 | Payments & Monetization | ⏳ Planned | 2026-09-16 |
| Phase 6 | Advanced Content | ⏳ Planned | 2026-10-14 |
| Phase 7 | Mobile Parity & Polish | ⏳ Planned | 2026-12-08 |

**Current Date**: 2026-06-25  
**Expected Launch**: 2026-12-08

For questions or issues, refer to the detailed status documents above or contact the development team.
