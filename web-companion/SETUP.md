# Web Companion Setup Guide

## Initial Setup Complete ✅

The Trivia Tycoon Web Companion React + TypeScript project has been scaffolded and configured.

### What's Been Set Up

- ✅ Vite 5 project with React 18 + TypeScript
- ✅ Tailwind CSS 3 with PostCSS
- ✅ Core dependencies installed:
  - Zustand (state management)
  - TanStack Query (server state)
  - Axios (API client)
  - React Router v6 (routing)
  - Framer Motion (animations)
  - Recharts (charts)
  - And more...
- ✅ Project structure scaffolded (`src/` folders created)
- ✅ Configuration files (Tailwind, TSConfig, ESLint ready)
- ✅ Docker setup (Dockerfile, Dockerfile.dev, docker-compose.yml, nginx.conf)
- ✅ Environment files (.env.local, .env.staging, .env.production)
- ✅ Core application files (API client, stores, routing)

### Directory Structure

```
web-companion/
├── src/
│   ├── app/                 # App shell, routing, providers
│   │   ├── App.tsx
│   │   ├── providers.ts     # TanStack Query config
│   │   └── router.tsx       # React Router v6 routes
│   ├── core/                # Infrastructure
│   │   ├── api/             # Axios client
│   │   ├── env.ts           # Environment config
│   │   ├── realtime/        # SignalR (placeholder)
│   │   └── storage/         # Dexie.js (placeholder)
│   ├── stores/              # Zustand stores
│   │   ├── authStore.ts
│   │   ├── profileStore.ts
│   │   ├── uiStore.ts
│   │   └── index.ts
│   ├── features/            # Feature modules (structure ready)
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── quiz/
│   │   ├── skill-tree/
│   │   └── ...
│   ├── components/          # Shared components (structure ready)
│   ├── hooks/               # Custom hooks (placeholder)
│   ├── lib/                 # Pure utilities (placeholder)
│   └── assets/              # Static assets
├── public/                  # Favicon, etc.
├── Dockerfile               # Production build
├── Dockerfile.dev           # Development with HMR
├── docker-compose.yml       # Docker orchestration
├── nginx.conf               # Production Nginx config
├── .env.local               # Development env
├── .env.staging             # Staging env
├── .env.production          # Production env
├── tailwind.config.ts       # Tailwind configuration
├── postcss.config.ts        # PostCSS with Tailwind
├── vite.config.ts           # Vite configuration with path aliases
├── tsconfig.json            # TypeScript root config
├── tsconfig.app.json        # App TypeScript config
├── package.json             # Dependencies
└── README.md                # Project overview
```

## Next Steps

### 1. Configure Environment Variables

The `.env.local` file is pre-configured for local development connecting to `http://localhost:5000`. Update if needed:

```bash
# .env.local
VITE_API_URL=http://localhost:5000
VITE_GOOGLE_CLIENT_ID=your_google_client_id_here
# ... other vars
```

### 2. Start Development

Without Docker:
```bash
npm install  # (already done)
npm run dev
# Open http://localhost:5173
```

With Docker (if backend is available):
```bash
docker-compose up web-companion
# Open http://localhost:5173
```

### 3. Begin Phase 1 Development

The project is ready for Phase 1: Foundation & Core Game Loop (Weeks 1–4 per the development plan).

**Current status**: Scaffolding complete, core infrastructure in place.

**What's next**:
1. Create placeholder pages for each route (each returns a simple `<div>`)
2. Implement login/auth screens (Phase 1: Weeks 3–4)
3. Build API client integration tests
4. Implement game session store and question engine (Phase 2)

### 4. Install Pre-Commit Hooks (Optional)

```bash
npm install husky lint-staged --save-dev
npx husky install
# Husky will auto-run eslint/prettier on commits
```

### 5. Keep Flutter Session Available

The original Flutter `trivia_tycoon` repo is available at:
```
c:\Users\lmxbl\StudioProjects\trivia_tycoon
```

Refer to it for:
- Architecture patterns (services, providers, etc.)
- API contracts and endpoints
- Game logic (question engine, XP calculation, skill effects)
- UI inspiration (animations, responsive design)

**Migration helper**: See [Flutter → React Quick Reference](../../docs/web-companion/WEB_COMPANION_DEVELOPMENT_PLAN.md#appendix-flutter--react-quick-reference) in the development plan.

## Known Issues & Workarounds

### Rolldown Native Binding (Windows)

If `npm run build` fails with a native binding error, this is a known Windows issue with Rolldown. 

**Workaround**: The dev server (`npm run dev`) works fine. For production builds, use Docker:

```bash
docker build -t synaptix-web-companion:latest .
```

### CORS During Development

If the dev server can't reach the backend API, check:

1. Backend is running on http://localhost:5000
2. Backend allows CORS for http://localhost:5173
3. Check `.env.local` has the correct API URL

If needed, temporarily mock API responses in Zustand stores for testing.

## Key Files to Update First

As you begin development, these files will grow:

| File | Purpose | Phase |
|---|---|---|
| `src/app/router.tsx` | Add real route components | 1-7 |
| `src/core/api/client.ts` | Add endpoint methods | 1 |
| `src/stores/` | Extend stores as features ship | 2-7 |
| `src/features/auth/` | Build login/registration | 1 |
| `src/features/quiz/` | Build game engine | 2 |
| `src/features/skill-tree/` | Build hex grid + planner | 3 |

## Full Development Plan

For the complete 24-week development plan, feature roadmap, architecture decisions, and post-launch v2+ features:

📘 **[WEB_COMPANION_DEVELOPMENT_PLAN.md](../../docs/web-companion/WEB_COMPANION_DEVELOPMENT_PLAN.md)**

## CI/CD & Deployment

Once initial features are working, set up:

1. **GitHub Actions**: Auto-test on PR, build on merge to `main`
2. **Staging deployment**: Auto-deploy to staging.synaptixplay.com
3. **Production deployment**: Manual trigger to synaptixplay.com

(See CI/CD documentation in the backend repo when ready)

---

**Created**: 2026-06-25  
**Status**: Foundation phase scaffolding complete  
**Next**: Implement Phase 1 (auth, routing, basic pages)

For questions, refer to the [web-companion docs](../../docs/web-companion/) or the development plan linked above.
