// Idempotent repair script for existing MongoDB volumes.
// Run with:
// docker compose -f docker/compose.yml exec mongodb mongosh -u <root-user> -p <root-password> --authenticationDatabase admin /docker-entrypoint-initdb.d/repair-indexes.js

load('/docker-entrypoint-initdb.d/01-init.js');
