print('Starting MongoDB initialization...');

const appDb = process.env.MONGO_APP_DB || process.env.MONGO_ANALYTICS_DB || 'synaptix_analytics';
const authDb = process.env.MONGO_AUTH_DB || appDb;
const analyticsDb = process.env.MONGO_ANALYTICS_DB || appDb;
const cryptoDb = process.env.MONGO_CRYPTO_DB || 'synaptix_crypto';
const appUser = process.env.MONGO_APP_USER || 'synaptix_app_user';
const appPassword = process.env.MONGO_APP_PASSWORD || 'synaptix_app_password_123';
const extraDbs = (process.env.MONGO_EXTRA_DBS || '')
  .split(',')
  .map(x => x.trim())
  .filter(Boolean);

const grantedDbs = Array.from(new Set([appDb, authDb, analyticsDb, cryptoDb, ...extraDbs].filter(Boolean)));

function createCollectionIfMissing(database, collectionName, options) {
  if (!database.getCollectionNames().includes(collectionName)) {
    database.createCollection(collectionName, options || {});
  }
}

function createIndexIfMissing(collection, keys, options) {
  try {
    collection.createIndex(keys, options || {});
  } catch (e) {
    if (String(e).includes('Index already exists')) {
      print(`Index already exists for ${collection.getName()}: ${JSON.stringify(keys)}`);
      return;
    }
    throw e;
  }
}

function ensureAnalyticsIndexes(database) {
  createCollectionIfMissing(database, 'analytics_events');
  createIndexIfMissing(database.analytics_events, { event_id: 1 }, { unique: true, name: 'ux_analytics_events_event_id' });
  createIndexIfMissing(database.analytics_events, { user_id: 1, received_at: -1 }, { name: 'ix_analytics_events_user_received' });
  createIndexIfMissing(database.analytics_events, { event_type: 1, received_at: -1 }, { name: 'ix_analytics_events_type_received' });

  createCollectionIfMissing(database, 'question_answered_events');
  createIndexIfMissing(database.question_answered_events, { PlayerId: 1, QuestionId: 1, AnsweredAtUtc: 1 }, { unique: true, name: 'ux_question_answered_events_player_question_answered' });
  createIndexIfMissing(database.question_answered_events, { PlayerId: 1, AnsweredAtUtc: -1 }, { name: 'ix_question_answered_events_player_answered' });
  createIndexIfMissing(database.question_answered_events, { MatchId: 1 }, { name: 'ix_question_answered_events_match' });

  createCollectionIfMissing(database, 'qa_daily_rollups');
  createIndexIfMissing(
    database.qa_daily_rollups,
    { Day: 1, Mode: 1, Category: 1, Difficulty: 1, SynaptixMode: 1, Surface: 1, AudienceSegment: 1, EntryPoint: 1, BrandVersion: 1 },
    { name: 'ix_qa_daily_rollups_day_dimensions' }
  );
  createIndexIfMissing(database.qa_daily_rollups, { UpdatedAtUtc: -1 }, { name: 'ix_qa_daily_rollups_updated' });

  createCollectionIfMissing(database, 'qa_player_daily_rollups');
  createIndexIfMissing(database.qa_player_daily_rollups, { PlayerId: 1, Day: -1 }, { name: 'ix_qa_player_daily_rollups_player_day' });
  createIndexIfMissing(database.qa_player_daily_rollups, { Day: 1 }, { name: 'ix_qa_player_daily_rollups_day' });
  createIndexIfMissing(database.qa_player_daily_rollups, { UpdatedAtUtc: -1 }, { name: 'ix_qa_player_daily_rollups_updated' });
}

function ensureCryptoIndexes(database) {
  createCollectionIfMissing(database, 'crypto_settlements');
  createIndexIfMissing(database.crypto_settlements, { withdrawal_id: 1 }, { unique: true, name: 'ux_crypto_settlements_withdrawal_id' });
  createIndexIfMissing(database.crypto_settlements, { status: 1, created_at: -1 }, { name: 'ix_crypto_settlements_status_created' });
  createIndexIfMissing(database.crypto_settlements, { player_id: 1, created_at: -1 }, { name: 'ix_crypto_settlements_player_created' });
}

const authDatabase = db.getSiblingDB(authDb);
const roles = grantedDbs.map(name => ({ role: 'readWrite', db: name }));

// Create or update general app user
const existing = authDatabase.getUser(appUser);
if (existing) {
  authDatabase.updateUser(appUser, { pwd: appPassword, roles });
  print(`Updated MongoDB app user ${appUser} in ${authDb}`);
} else {
  authDatabase.createUser({ user: appUser, pwd: appPassword, roles });
  print(`Created MongoDB app user ${appUser} in ${authDb}`);
}

// Create or update crypto-service user (if different from app user)
const cryptoUser = process.env.MONGO_CRYPTO_USER || appUser;
const cryptoPassword = process.env.MONGO_CRYPTO_PASSWORD || appPassword;

if (cryptoUser !== appUser) {
  const cryptoRoles = [
    { role: 'readWrite', db: cryptoDb },
    { role: 'readWrite', db: analyticsDb }
  ];

  const existingCryptoUser = authDatabase.getUser(cryptoUser);
  if (existingCryptoUser) {
    authDatabase.updateUser(cryptoUser, { pwd: cryptoPassword, roles: cryptoRoles });
    print(`Updated MongoDB crypto-service user ${cryptoUser} in ${authDb}`);
  } else {
    authDatabase.createUser({ user: cryptoUser, pwd: cryptoPassword, roles: cryptoRoles });
    print(`Created MongoDB crypto-service user ${cryptoUser} in ${authDb}`);
  }
}

ensureAnalyticsIndexes(db.getSiblingDB(analyticsDb));
ensureCryptoIndexes(db.getSiblingDB(cryptoDb));

print(`MongoDB initialization completed. authDb=${authDb}, analyticsDb=${analyticsDb}, cryptoDb=${cryptoDb}, user=${appUser}`);
