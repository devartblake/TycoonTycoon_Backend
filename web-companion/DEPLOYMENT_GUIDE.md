# Web Companion Deployment Guide

This guide covers deploying the web-companion application and configuring it to connect to the backend API.

## Environment Variables

### Development

In development, the Vite dev server provides a proxy for API requests. The following variables are available:

```bash
# Backend API (used as proxy target in development)
VITE_API_BASE_URL=http://localhost:5000

# WebSocket URLs
VITE_WS_URL=ws://localhost:5000/ws
VITE_SIGNALR_URL=http://localhost:5000/hubs

# Third-party services
VITE_STRIPE_KEY=pk_test_your_key_here
VITE_GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com
VITE_COMPLIANCE_URL=http://localhost:3000/compliance

# App metadata
VITE_APP_VERSION=1.0.0-dev
```

**To run in development:**
```bash
npm install
npm run dev
```

The app will start at `http://localhost:5173` with proxy-based API calls.

### Production

For production builds, you **must** set `VITE_API_BASE_URL` to your backend URL. The build process will compile this value into the final bundle.

```bash
# REQUIRED: Your production backend URL
VITE_API_BASE_URL=https://api.synaptixplay.com

# Other production environment variables
VITE_STRIPE_KEY=pk_live_your_production_key_here
VITE_GOOGLE_CLIENT_ID=your-production-google-client-id.apps.googleusercontent.com
VITE_WS_URL=wss://api.synaptixplay.com/ws
VITE_SIGNALR_URL=https://api.synaptixplay.com/hubs
VITE_COMPLIANCE_URL=https://compliance.synaptixplay.com
```

**To build for production:**
```bash
VITE_API_BASE_URL=https://api.synaptixplay.com npm run build
```

## Backend CORS Configuration (CRITICAL)

For the web app to connect to the backend API, the backend **must** have proper CORS headers configured.

### Required CORS Headers

The backend must return these headers for requests from your frontend URL:

```
Access-Control-Allow-Origin: https://your-frontend-url.com
Access-Control-Allow-Credentials: true
Access-Control-Allow-Methods: GET, POST, PUT, PATCH, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization, X-App-Version, X-Device-Id
Access-Control-Max-Age: 86400
```

### Common CORS Errors and Solutions

#### Error: "No 'Access-Control-Allow-Origin' header"
- **Cause**: Backend is not returning CORS headers
- **Solution**: Configure CORS middleware on the backend
- **Verify with cURL**:
  ```bash
  curl -i -X OPTIONS https://api.synaptixplay.com/api/v1/auth/login \
    -H "Origin: https://your-frontend-url.com" \
    -H "Access-Control-Request-Method: POST"
  ```

#### Error: "Credentials mode is 'include' but Access-Control-Allow-Credentials is missing"
- **Cause**: Backend auth is enabled (`withCredentials: true`) but backend doesn't allow it
- **Solution**: Add `Access-Control-Allow-Credentials: true` to backend CORS config

#### Error: "Method not allowed" or status 405
- **Cause**: Backend CORS configuration doesn't include the HTTP method
- **Solution**: Add the method to `Access-Control-Allow-Methods`

## Network Debugging

### Enable Developer Tools Logging

Open browser DevTools and check the console during API requests:

1. **Network Tab**: Watch for API requests to `/api/v1/*`
   - Should see requests with status `200`, `401`, or `4xx` (not CORS errors)
   - Headers should include `Authorization: ******

2. **Console Tab**: Look for debug messages from the API client
   - `📡 API Request: POST /api/v1/auth/login` → Normal operation
   - `🌐 Network Error` → Backend is unreachable
   - `🔒 CORS Error` → CORS misconfiguration

### Common Issues and Troubleshooting

#### App loads but "Network Error" when logging in

1. Check if backend is running: `curl https://api.synaptixplay.com/health`
2. Check `VITE_API_BASE_URL` is set correctly in production build
3. Verify backend is accessible from your network/location
4. Check firewall/security group rules allow HTTPS traffic

#### "Request blocked by CORS policy"

1. Verify backend CORS headers with:
   ```bash
   curl -i -X OPTIONS https://api.synaptixplay.com/api/v1/auth/login \
     -H "Origin: https://your-frontend-url.com"
   ```
2. Ensure `Origin` header matches `Access-Control-Allow-Origin` exactly
3. Check backend CORS configuration includes your frontend domain

#### Requests work locally but fail in production

1. **Cause**: `VITE_API_BASE_URL` not set during build
2. **Solution**: Build with environment variable: `VITE_API_BASE_URL=https://api.synaptixplay.com npm run build`
3. **Verify**: Check Network tab in DevTools - requests should go to `https://api.synaptixplay.com/api/v1/*`

## Docker Deployment

### Build Docker Image

```dockerfile
# Dockerfile included
DOCKER_BUILDKIT=1 docker build -f Dockerfile -t web-companion:latest .
```

### Environment Variables in Docker

Pass environment variables to the Docker container:

```bash
docker run \
  -e VITE_API_BASE_URL=https://api.synaptixplay.com \
  -e VITE_STRIPE_KEY=pk_live_... \
  -e VITE_GOOGLE_CLIENT_ID=... \
  -p 80:80 \
  web-companion:latest
```

### Docker Compose

For local development with backend:

```bash
docker-compose up
```

Ensure `docker-compose.yml` has the correct backend service name/URL.

## Deployment Checklist

- [ ] Backend API is running and accessible
- [ ] Backend CORS headers are properly configured
- [ ] `VITE_API_BASE_URL` environment variable is set correctly
- [ ] All third-party API keys (Stripe, Google) are configured
- [ ] WebSocket URLs point to correct backend
- [ ] Build completes without warnings
- [ ] Test login flow works end-to-end
- [ ] DevTools Network tab shows requests going to correct API
- [ ] Authentication tokens are being stored in localStorage
- [ ] API errors display helpful messages

## API Endpoints Used

The web app expects these authentication endpoints on the backend:

```
POST   /api/v1/auth/login        → { email, password, deviceId } → { user, token, refreshToken }
POST   /api/v1/auth/signup       → { email, password, deviceId, username? } → { user, token, refreshToken }
POST   /api/v1/auth/refresh      → { refreshToken } → { token, refreshToken }
POST   /api/v1/auth/logout       → { } → { success }
GET    /api/v1/users/me          → returns current user
```

All authenticated requests include:
```
Authorization: ******
X-App-Version: {appVersion}
X-Device-Id: {deviceId}
```

## Support

If experiencing connection issues:

1. Check browser DevTools Network tab for actual requests being sent
2. Run `curl` against the backend to verify it's responding
3. Check CORS headers with `curl -i -X OPTIONS ...`
4. Review backend logs for authentication/authorization errors
5. Ensure frontend and backend URLs match (including protocol: http vs https)
