# MongoDB Authentication Fix

## Problem Summary

The crypto-service container was failing to start with MongoDB authentication errors:

```
pymongo.errors.OperationFailure: Authentication failed., full error: {'ok': 0.0, 'errmsg': 'Authentication failed.', 'code': 18, 'codeName': 'AuthenticationFailed'}
```

This occurred during the crypto-service startup when it tried to connect to MongoDB to list indexes on the crypto settlements collection.

## Root Cause

The crypto-service requires a `MONGO_URL` environment variable to connect to MongoDB, but this variable was not explicitly defined in any `.env` files. Instead, the Docker Compose file was attempting to construct the connection string from individual MongoDB environment variables using variable substitution.

While the individual variables (`MONGO_APP_USER`, `MONGO_APP_PASSWORD`, `MONGO_AUTH_DB`, `MONGO_CRYPTO_DB`) were correctly configured, the lack of an explicit `MONGO_URL` variable meant:

1. The crypto-service couldn't reliably construct the connection string
2. The connection string format was ambiguous (which authSource to use, exact URL format)
3. Environment variable substitution in compose files can be fragile across different deployment contexts

## Solution

Added explicit `MONGO_URL` environment variables to all `.env` files with the proper MongoDB connection string format:

```
MONGO_URL=mongodb://username:password@mongodb:27017/database?authSource=auth_db
```

### Files Updated

#### 1. `docker/.env` (Development)
```env
MONGO_URL=mongodb://tycoon_app_user:tycoon_app_password_123@mongodb:27017/synaptix_crypto?authSource=synaptix_analytics
```

**Components:**
- **Host**: `mongodb` (Docker service name)
- **Port**: `27017` (default MongoDB port)
- **Username**: `tycoon_app_user` (MONGO_APP_USER)
- **Password**: `tycoon_app_password_123` (MONGO_APP_PASSWORD)
- **Database**: `synaptix_crypto` (MONGO_CRYPTO_DB - initial database)
- **authSource**: `synaptix_analytics` (MONGO_AUTH_DB - where user is authenticated)

#### 2. `docker/.env.production`
```env
MONGO_URL=mongodb://adminSynaptix:UCIf1KI6lCH73hANlGsz8ga8e+0IAoir@mongodb:27017/synaptix_crypto?authSource=synaptix_analytics
```

**Note:** Uses production credentials with the same structure.

#### 3. `docker/.env.staging`
```env
MONGO_URL=mongodb://adminSynaptix:UCIf1KI6lCH73hANlGsz8ga8e+0IAoir@mongodb:27017/synaptix_crypto?authSource=synaptix_analytics
```

**Note:** Uses staging credentials.

#### 4. `docker/.env.example`
```env
MONGO_URL=mongodb://<MONGO_APP_USER>:<MONGO_APP_PASSWORD>@mongodb:27017/<MONGO_CRYPTO_DB>?authSource=<MONGO_AUTH_DB>
```

**Template**: Shows the format with placeholders for customization.

## MongoDB Connection String Explained

### Format
```
mongodb://username:password@host:port/database?authSource=auth_db
```

### Parameters

| Parameter | Purpose | Example |
|-----------|---------|---------|
| `username` | User to authenticate as | `adminSynaptix` |
| `password` | User password | `UCIf1KI6lCH73hANlGsz8ga8e+0IAoir` |
| `host` | MongoDB server hostname | `mongodb` (Docker service) |
| `port` | MongoDB server port | `27017` |
| `database` | Initial database to connect to | `synaptix_crypto` |
| `authSource` | Database where user is authenticated | `synaptix_analytics` |

### Important Notes

- **authSource vs database**: These are often different in MongoDB. The `authSource` is the database where the user was created (typically `admin` or `synaptix_analytics`), while `database` is where you want to perform operations.
- **Credentials**: The MongoDB initialization script (`docker/init/mongo/01-init.js`) creates users in the `authSource` database with `readWrite` roles for multiple databases.
- **Docker Service Name**: When connecting from within Docker, use the service name (`mongodb`) as the hostname, not `localhost` or an IP address.

## How MongoDB Initialization Works

1. **Root User Creation**: MongoDB creates a root user using:
   - `MONGO_INITDB_ROOT_USERNAME`
   - `MONGO_INITDB_ROOT_PASSWORD`

2. **App User Creation**: The initialization script (`01-init.js`) creates an app user:
   - Username: `MONGO_APP_USER`
   - Password: `MONGO_APP_PASSWORD`
   - Authenticated in: `MONGO_AUTH_DB` (e.g., `synaptix_analytics`)
   - Roles: `readWrite` for all databases including `synaptix_analytics`, `synaptix_crypto`, etc.

3. **Collection Setup**: The script also creates collections and indexes for:
   - Analytics events in `synaptix_analytics` database
   - Crypto settlements in `synaptix_crypto` database

## Verification

To verify the MongoDB connection is working:

```bash
# From inside a container
docker exec synaptix_operator_dashboard_react mongosh \
  --authenticationDatabase=synaptix_analytics \
  -u adminSynaptix \
  -p UCIf1KI6lCH73hANlGsz8ga8e+0IAoir \
  --eval "db.adminCommand('ping')"
```

Expected output:
```json
{ ok: 1 }
```

## Deployment Notes

- **Production (.env.production)**: Production credentials are hardcoded and the file is ignored by git (correct for security)
- **Staging (.env.staging)**: Staging credentials follow the same pattern
- **Development (.env)**: Development credentials are checked into git for team consistency
- **Template (.env.example)**: Shows the connection string format for reference

## Critical Discovery: MongoDB Healthcheck Issue

The initial problem was compounded by a **timing issue** in the MongoDB healthcheck:

### Original Healthcheck
```yaml
healthcheck:
  test: ["CMD", "mongosh", "--eval", "db.adminCommand('ping')"]
  interval: 10s
  timeout: 5s
  retries: 10
```

**Problem**: This only verified that MongoDB was responding, NOT that:
- The initialization script had completed
- The MONGO_APP_USER had been created
- User credentials were valid

This caused the crypto-service and backend-api to connect before the initialization completed, resulting in authentication failures.

### Updated Healthcheck (Fixed)
```yaml
healthcheck:
  test: ["CMD", "mongosh", "--authenticationDatabase=${MONGO_AUTH_DB:-synaptix_analytics}", "-u", "${MONGO_APP_USER:-synaptix_app_user}", "-p", "${MONGO_APP_PASSWORD:-synaptix_app_password_123}", "--eval", "db.adminCommand('ping')"]
  interval: 10s
  timeout: 5s
  retries: 10
```

**Solution**: Now the healthcheck:
1. Authenticates with the app user credentials
2. Specifies the correct authentication database
3. Verifies the user exists and can authenticate
4. Proves initialization has completed

### Impact
Services now wait for MongoDB to be **fully initialized** before attempting to connect, eliminating the race condition that caused authentication failures.

## Related Files

- `docker/compose.yml` - Service definitions and environment variable references (updated healthcheck)
- `docker/init/mongo/01-init.js` - MongoDB initialization script
- `BACKEND_INTEGRATION.md` - Backend integration documentation
- `DEPLOYMENT.md` - Deployment guide

## Testing

After deploying with these fixes:

1. Verify Docker containers start successfully
2. Check container logs for authentication errors
3. Confirm crypto-service starts and can list indexes
4. Test backend API connections through the React dashboard

```bash
# View logs for specific service
docker compose logs -f crypto-service

# Check if all services are healthy
docker compose ps
```

All services should show `healthy` status.
