# API Connection Troubleshooting Guide

This guide helps diagnose and fix issues when the web-companion can't connect to the backend API.

## Quick Diagnosis Checklist

Run these checks in order:

1. **Is the backend running?**
   ```bash
   curl https://api.synaptixplay.com/health
   # Should return 200 OK
   ```

2. **Can you reach the backend from your network?**
   ```bash
   ping api.synaptixplay.com
   # Should have responses, not all lost packets
   ```

3. **Is the app using the right API URL?**
   - Open DevTools (F12) → Console
   - Type: `import.meta.env.VITE_API_BASE_URL`
   - Should show your backend URL (e.g., `https://api.synaptixplay.com`)

4. **Are API requests being made?**
   - Open DevTools (F12) → Network tab
   - Click Login and try to sign in
   - Should see requests to `/api/v1/auth/login` or similar
   - If no requests appear, the frontend is likely not calling the API

## Issue: "Network Error" When Logging In

### Symptoms
- Login form shows "Network Error" message
- Browser console shows `🌐 Network Error` message
- No API requests appear in Network tab

### Diagnosis
```javascript
// In DevTools console:
import.meta.env.VITE_API_BASE_URL
// Check if this is your backend URL or a default value
```

### Solutions

**Solution 1: Backend not running**
```bash
# Verify backend is running
curl https://api.synaptixplay.com/health

# If that fails, backend is down
# Start the backend service
cd ../backend
dotnet run
```

**Solution 2: Wrong API URL in development**
```bash
# Make sure VITE_API_BASE_URL is set correctly for dev
export VITE_API_BASE_URL=http://localhost:5000
npm run dev
```

**Solution 3: Production build missing environment variable**
```bash
# Build with the correct backend URL
export VITE_API_BASE_URL=https://api.synaptixplay.com
npm run build
```

**Solution 4: Network/firewall blocking requests**
```bash
# Test if you can reach the backend
curl -v https://api.synaptixplay.com/api/v1/auth/login

# If connection times out or is refused:
# - Check firewall settings
# - Check if you're behind a proxy
# - Verify backend URL is correct
```

## Issue: "Request blocked by CORS policy"

### Symptoms
- Browser console shows CORS error
- Network tab shows request with status `0` (blocked)
- Error message mentions `Access-Control-Allow-Origin`

### Example Error
```
Access to XMLHttpRequest at 'https://api.synaptixplay.com/api/v1/auth/login'
from origin 'https://my-frontend.com' has been blocked by CORS policy:
No 'Access-Control-Allow-Origin' header is present on the requested resource.
```

### Diagnosis
```bash
# Check if backend returns CORS headers
curl -i -X OPTIONS https://api.synaptixplay.com/api/v1/auth/login \
  -H "Origin: https://my-frontend.com" \
  -H "Access-Control-Request-Method: POST"

# Look for response headers like:
# Access-Control-Allow-Origin: https://my-frontend.com
# Access-Control-Allow-Credentials: true
```

### Solutions

**Solution 1: Backend not configured for CORS**
- Contact backend team to enable CORS
- Backend must return:
  ```
  Access-Control-Allow-Origin: https://my-frontend.com
  Access-Control-Allow-Credentials: true
  Access-Control-Allow-Methods: GET, POST, PUT, PATCH, DELETE, OPTIONS
  Access-Control-Allow-Headers: Content-Type, Authorization, X-App-Version, X-Device-Id
  ```

**Solution 2: Origin mismatch**
- Frontend: `https://my-frontend.com`
- Backend expects: `https://something-else.com`
- **Fix**: Update backend CORS config to include your frontend URL

**Solution 3: Credentials needed but CORS not allowing them**
- Error: "credentials mode is 'include' but Access-Control-Allow-Credentials is missing"
- **Fix**: Backend must add `Access-Control-Allow-Credentials: true`

## Issue: "Unauthorized" or "401" Error

### Symptoms
- Login works locally but not in production
- Requests show status 401
- User is redirected to login page immediately

### Common Causes

1. **Token validation failing**
   ```bash
   # Verify token is being sent
   # Check Network tab → Request Headers → Authorization header
   # Should show: Authorization: ******
   ```

2. **Token expired**
   - Check if refresh token mechanism is working
   - Look in DevTools console for: `Token refresh failed`

3. **Token not being stored**
   ```javascript
   // In DevTools console:
   localStorage.getItem('auth_token')
   // Should return the token, not null
   ```

### Solutions

**Solution 1: Verify token is being stored**
```javascript
// In DevTools console after login:
console.log(localStorage.getItem('auth_token'))
console.log(localStorage.getItem('refresh_token'))
// Both should have values
```

**Solution 2: Clear and retry**
```javascript
// In DevTools console:
localStorage.clear()
location.reload()
// Then try logging in again
```

**Solution 3: Check backend authentication**
- Backend might not recognize the token format
- Compare with Flutter app's token format
- Verify backend is using the same auth method

## Issue: "404 Not Found" on API Endpoints

### Symptoms
- Login fails with 404 error
- Network tab shows requests to `/api/v1/auth/login` returning 404

### Diagnosis
```bash
# Verify the endpoint exists on the backend
curl https://api.synaptixplay.com/api/v1/auth/login -X POST -d '{"email":"test@test.com"}'

# Check what endpoints are available
curl https://api.synaptixplay.com/swagger
# or
curl https://api.synaptixplay.com/api/v1/
```

### Solutions

**Solution 1: Endpoint not implemented**
- Backend might be using different endpoint names
- Check backend OpenAPI/Swagger documentation
- Update API client calls to match backend endpoints

**Solution 2: API path incorrect**
- Make sure `/api/v1/` prefix is correct
- Some backends use `/api/` or `/v1/` instead
- Update `src/core/env.ts` if needed

## Issue: Requests Work Locally but Fail in Production

### Symptoms
- `npm run dev` works fine
- Production build fails to connect
- Different behavior in different environments

### Diagnosis

1. **Check if environment variable is set in production build**
   ```javascript
   // In production app, open DevTools console:
   import.meta.env.VITE_API_BASE_URL
   // Should show your production URL, not localhost
   ```

2. **Verify actual requests in Network tab**
   - Open DevTools → Network tab
   - Try to login
   - Check URL of POST request to `auth/login`
   - Should be `https://api.synaptixplay.com/...`, not localhost

### Solutions

**Solution 1: Missing environment variable during build**
```bash
# Wrong - builds with default localhost
npm run build

# Correct - builds with production URL
VITE_API_BASE_URL=https://api.synaptixplay.com npm run build
```

**Solution 2: Environment variable not persisted in Docker**
```dockerfile
# In Dockerfile, make sure env vars are passed:
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

# Build with: docker build --build-arg VITE_API_BASE_URL=https://api.synaptixplay.com
```

**Solution 3: Check in DevTools that build used correct URL**
```javascript
// After production build is deployed:
import.meta.env.VITE_API_BASE_URL
// This will show what URL was baked into the build
```

## Issue: Mixed Content Error (HTTP/HTTPS)

### Symptoms
- Error: "Mixed Content: The page was loaded over HTTPS, but requested an insecure resource..."
- This happens when frontend is HTTPS but API is HTTP

### Solutions

**Solution 1: Use HTTPS for both**
```bash
# Ensure both frontend and API use HTTPS
export VITE_API_BASE_URL=https://api.synaptixplay.com
npm run build
```

**Solution 2: Use HTTPS upgrade**
- If backend only has HTTP, enable HTTPS
- Or use a reverse proxy that upgrades HTTP to HTTPS

## Advanced Debugging

### Enable Verbose API Logging

1. **In development**, API requests are logged to console:
   ```
   📡 API Request: POST /api/v1/auth/login
   📡 API Request: GET /api/v1/users/me
   ```

2. **In browser console**, manually log requests:
   ```javascript
   import { apiClient } from './src/core/api/client';
   
   // Try a test request
   apiClient.login('test@test.com', 'password')
     .then(res => console.log('Success:', res))
     .catch(err => {
       console.log('Error:', err.message)
       console.log('Response:', err.response?.data)
       console.log('Status:', err.response?.status)
     })
   ```

### Test Endpoints with cURL

```bash
# Test authentication endpoint
curl -X POST https://api.synaptixplay.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"password"}'

# Test with CORS preflight
curl -i -X OPTIONS https://api.synaptixplay.com/api/v1/auth/login \
  -H "Origin: https://my-frontend.com" \
  -H "Access-Control-Request-Method: POST"

# Test with authorization header (if you have a token)
curl -X GET https://api.synaptixplay.com/api/v1/users/me \
  -H "Authorization: ******"
```

### Check App Version Header

```bash
# In DevTools Network tab, check request headers:
# Should include:
# X-App-Version: 1.0.0-dev
# X-Device-Id: web_xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

# If missing, check that src/core/env.ts is exporting correctly
```

## Getting Help

If you're still unable to connect, provide:

1. **Output from DevTools Console** (copy full error message)
2. **Network tab screenshot** showing failed request:
   - URL being requested
   - Response status
   - Response headers (especially CORS headers)
3. **Backend URL** being used
4. **Frontend URL** (where the app is deployed)
5. **Browser and OS** information
6. **Result of**: `curl -i -X OPTIONS https://your-backend/api/v1/auth/login -H "Origin: https://your-frontend"`

---

**Remember**: The most common issues are:
1. CORS not configured on backend
2. Wrong API URL in production build
3. Backend not actually running
4. Firewall/network blocking the connection
