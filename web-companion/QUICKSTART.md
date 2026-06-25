# Web Companion — Quick Start Guide

## ✅ Status: Ready to Development

The web companion is fully scaffolded with **Phase 1 features complete** and the **dev server is working**.

## 🚀 Start Development

### 1. Open Terminal in Project Directory
```bash
cd "C:\Users\lmxbl\Documents\TycoonTycoon_Backend\web-companion"
```

### 2. Start Dev Server
```bash
npm run dev
```

**Output:**
```
  VITE v5.4.21  ready in 257 ms

  ➜  Local:   http://localhost:5173/
  ➜  Network: http://172.20.10.2:5173/
```

### 3. Open in Browser
Click the link or navigate to: **http://localhost:5173**

---

## 🧪 Test the App

### Login Flow
1. You'll land on the **Login page**
2. Enter any email and password (mock auth accepts all)
3. Click **Sign In**
4. You're logged in! See the dashboard with stats and quick actions

### Test Other Routes
- Click **Play** → Quiz Lobby (placeholder, Phase 2)
- Click **Skills** → Skill Tree (placeholder, Phase 3)
- Click **Leaderboard** → Leaderboard (placeholder, Phase 2)
- Click **Friends** → Friends List (placeholder, Phase 5)
- Toggle sidebar with menu icon (responsive!)
- Click **Logout** → back to login

### Test Signup
1. On login page, click **"Sign up"**
2. Fill in display name, email, password
3. Watch real-time validation (green checkmarks appear)
4. Click **Sign Up**
5. New user created and logged in automatically

---

## 📁 What's in Phase 1

### ✅ Complete
- Authentication system (login, signup, mock auth)
- App shell layout (sidebar, top bar, responsive)
- Protected routes (auth guard)
- Dashboard page with stats
- Placeholder pages for all Phase 2-7 features
- State management (Zustand stores)
- Dark theme with Tailwind CSS

### ⏳ Next Steps (Weeks 2-4)
- Connect to real backend API (`/auth/login`, `/auth/signup`)
- Google Sign-In integration
- User profile fetching
- SignalR real-time notifications
- Dexie.js local storage

---

## 🛠️ Development Commands

```bash
# Start dev server (what you just did)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint

# Format code
npm run format

# TypeScript check
npm run type-check
```

---

## 📂 File Structure

```
src/
├── app/                    # React Router, TanStack Query
├── core/                   # API client, config, env
├── stores/                 # Zustand state (auth, profile, ui)
├── features/               # Feature modules by page
│   ├── auth/pages/         # Login, Signup
│   ├── dashboard/pages/    # Dashboard, Settings
│   ├── quiz/pages/         # Quiz Lobby (Phase 2)
│   ├── skill-tree/pages/   # Skill Tree (Phase 3)
│   └── ...
├── components/             # Shared UI components
│   └── layout/             # AppShell, ProtectedRoute
├── hooks/                  # Custom React hooks
└── lib/                    # Pure utilities

```

---

## 🎨 Design System

- **Theme**: Dark (gray-950 background)
- **Primary color**: Purple (#6366f1)
- **Icons**: lucide-react
- **Styling**: Tailwind CSS
- **Animations**: Smooth transitions on hover/focus

---

## 🔌 API Integration (Week 3)

When you connect to the backend, update:

1. **`src/core/api/client.ts`** — API endpoints
   ```typescript
   // Replace mock calls with real API
   const response = await apiClient.post('/auth/login', { email, password });
   ```

2. **`src/stores/authStore.ts`** — Auth actions
   ```typescript
   const login = async (email, password) => {
     // Call API, store token, update user state
   };
   ```

3. **`src/features/auth/pages/LoginPage.tsx`** — Use real auth
   ```typescript
   const { mutate: login } = useMutation({
     mutationFn: (creds) => apiClient.post('/auth/login', creds),
   });
   ```

---

## 🐛 Troubleshooting

### "Port 5173 already in use"
```bash
# Kill the process
npx kill-port 5173

# Or use a different port
npm run dev -- --port 5174
```

### "Module not found" errors
```bash
# Reinstall dependencies
rm -rf node_modules package-lock.json
npm install
npm run dev
```

### TypeScript errors
```bash
# Check TypeScript
npm run type-check

# Type errors are usually in IDE autocomplete too
```

---

## 📚 Reference

- **Development Plan**: `docs/web-companion/WEB_COMPANION_DEVELOPMENT_PLAN.md`
- **Phase 1 Status**: `PHASE_1_STATUS.md`
- **Setup Guide**: `SETUP.md`
- **Project Overview**: `README.md`

---

## 🎯 Next Priority

The next big milestone is **Week 3: API Integration**. You'll:

1. Get backend API endpoints from your backend team
2. Update `src/core/api/client.ts` with real endpoints
3. Replace mock auth with API calls
4. Test login/signup against real backend
5. Add token refresh logic

---

**Happy coding!** 🚀

Questions? Check the docs or visit the Flutter app (`c:\Users\lmxbl\StudioProjects\trivia_tycoon`) for architecture reference.
