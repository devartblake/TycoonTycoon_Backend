print('Starting MongoDB initialization...');

const appDb = process.env.MONGO_APP_DB || 'tycoon_db';
const appUser = process.env.MONGO_APP_USER || 'tycoon_app_user';
const appPassword = process.env.MONGO_APP_PASSWORD || 'tycoon_app_password_123';

db = db.getSiblingDB(appDb);

// Collections (keep your validation rules)
db.createCollection('game_events', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['player_id', 'event_type', 'timestamp'],
      properties: {
        player_id: { bsonType: 'string' },
        event_type: { bsonType: 'string' },
        event_data: { bsonType: 'object' },
        timestamp: { bsonType: 'date' }
      }
    }
  }
});

// Add other collections as needed (retain your original ones if you use them)
// (You can paste your existing createCollection blocks here.)

// Index example
db.game_events.createIndex({ player_id: 1, timestamp: -1 });
db.game_events.createIndex({ event_type: 1 });
db.game_events.createIndex({ timestamp: -1 });

// Create app user
db.createUser({
  user: appUser,
  pwd: appPassword,
  roles: [{ role: 'readWrite', db: appDb }]
});

print(`MongoDB initialization completed successfully! DB=${appDb}, user=${appUser}`);
