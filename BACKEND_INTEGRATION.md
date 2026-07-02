# Backend Integration Guide

## Overview

The React Operator Dashboard is configured to work with the .NET backend API. This guide covers environment setup, API configuration, and deployment options.

## Environment Configuration

### Development Setup

1. **Copy environment template:**
   ```bash
   cp .env.example .env.development.local
   ```

2. **Configure for local backend:**
   ```env
   VITE_API_BASE_URL=http://localhost:5000
   VITE_MOCK_MODE=false
   VITE_APP_ENV=development
   ```

3. **Start development server:**
   ```bash
   npm run dev
   ```

### Staging Setup

```env
VITE_API_BASE_URL=https://api-staging.synaptixplay.com
VITE_MOCK_MODE=false
VITE_APP_ENV=staging
```

### Production Setup

```env
VITE_API_BASE_URL=https://api.synaptixplay.com
VITE_MOCK_MODE=false
VITE_APP_ENV=production
```

## API Base URL

The API client will use `VITE_API_BASE_URL` environment variable to construct endpoint requests.

**Development:** `http://localhost:5000`
**Staging:** `https://api-staging.synaptixplay.com`
**Production:** `https://api.synaptixplay.com`

All API requests will be prefixed with this base URL.

## Authentication Flow

1. **Login**: `POST /admin/auth/login`
   - Request: `{ email, password }`
   - Response: `{ accessToken, refreshToken, expiresIn, admin }`
   - Tokens stored in Zustand auth store

2. **Refresh Token**: `POST /admin/auth/refresh`
   - Automatically triggered on 401 response
   - Updates accessToken and refreshToken

3. **Me Endpoint**: `GET /admin/auth/me`
   - Fetches current admin profile
   - Used for permission checks

## Required .NET Backend Endpoints

### Auth Endpoints
- `POST /admin/auth/login` — Admin login
- `POST /admin/auth/refresh` — Token refresh
- `GET /admin/auth/me` — Get current admin

### User Management
- `GET /admin/users/list` — Get users with pagination
- `GET /admin/users/{id}` — Get user detail
- `POST /admin/users/{id}/ban` — Ban user
- `POST /admin/users/{id}/unban` — Unban user
- `GET /admin/users/saved-views` — Get saved filter views
- `POST /admin/users/saved-views` — Create saved view
- `DELETE /admin/users/saved-views/{id}` — Delete saved view

### Moderation
- `GET /admin/moderation/players/{id}` — Get player moderation data
- `POST /admin/moderation/players/{id}/ban` — Ban player
- `POST /admin/moderation/players/{id}/unban` — Unban player
- `POST /admin/moderation/players/{id}/suspend` — Suspend player
- `POST /admin/moderation/players/{id}/warn` — Warn player

### Anti-Cheat
- `GET /admin/anti-cheat/stats` — Queue statistics
- `GET /admin/anti-cheat/queue` — Get next pending flag
- `GET /admin/anti-cheat/flags/{id}` — Get flag detail
- `POST /admin/anti-cheat/flags/{id}/verdict` — Submit verdict

### Audit & Security
- `GET /admin/audit/events` — Get audit events
- `GET /admin/audit/stats` — Audit statistics
- `GET /admin/audit/ip-locations` — IP geolocation data

### Economy
- `GET /admin/economy/players/{id}` — Get player economy
- `GET /admin/economy/players/{id}/transactions` — Get transaction history
- `POST /admin/economy/players/{id}/adjust-balance` — Adjust balance
- `POST /admin/economy/players/{id}/refund` — Issue refund
- `GET /admin/economy/players/search` — Search players
- `GET /admin/economy/stats` — Economy statistics

### Content Moderation
- `GET /admin/content/questions` — Get questions for review
- `POST /admin/content/questions/{id}/review` — Submit verdict
- `POST /admin/content/questions/bulk-review` — Bulk review
- `GET /admin/content/questions/stats` — Content statistics
- `GET /admin/content/categories` — Question categories

### Operations
- `GET /admin/operations/seasons` — Get seasons
- `GET /admin/operations/events` — Get game events
- `POST /admin/operations/{id}/action` — Perform lifecycle action
- `GET /admin/operations/stats` — Operations statistics

### Store
- `GET /admin/store/products` — List products
- `POST /admin/store/products` — Create product
- `PUT /admin/store/products/{id}` — Update product
- `DELETE /admin/store/products/{id}` — Delete product
- `GET /admin/store/flash-sales` — List flash sales
- `GET /admin/store/stock-policies` — List stock policies
- `GET /admin/store/reward-limits` — List reward limits
- `GET /admin/store/stats` — Store statistics

### Dashboard
- `GET /admin/dashboard/stats` — System health stats
- `GET /admin/dashboard/services/{id}/history` — Service history

### Notifications
- `GET /admin/notifications/templates` — List templates
- `POST /admin/notifications/templates` — Create template
- `GET /admin/notifications/channels` — List channels
- `POST /admin/notifications/schedules` — Schedule notification
- `GET /admin/notifications/dead-letter` — Failed messages

## Error Handling

The API client automatically handles:
- **401 Unauthorized**: Attempts token refresh, then redirects to login
- **403 Forbidden**: Shows permission denied message
- **5xx Server Errors**: Shows error message, allows retry
- **Network Errors**: Shows connection error message

## Mock Mode (Development Only)

To test without a backend:

```env
VITE_MOCK_MODE=true
```

This enables the mock API layer with realistic test data. Useful for:
- UI development without backend
- Feature demonstrations
- Local testing before backend integration

**Note:** Mock mode should NEVER be enabled in production.

## CORS Configuration

The backend must allow requests from the React app's origin:

**Development:**
```
Access-Control-Allow-Origin: http://localhost:3001
```

**Production:**
```
Access-Control-Allow-Origin: https://admin.synaptixplay.com
```

## Testing the Integration

1. **Verify backend is running:**
   ```bash
   curl http://localhost:5000/admin/health
   ```

2. **Check login endpoint:**
   ```bash
   curl -X POST http://localhost:5000/admin/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@synaptix.local","password":"password"}'
   ```

3. **Test in React app:**
   - Navigate to http://localhost:3001/auth/login
   - Enter admin credentials
   - Verify redirect to dashboard
   - Check browser DevTools Network tab for API calls

## Deployment

See [DEPLOYMENT.md](./DEPLOYMENT.md) for Docker and production deployment details.
