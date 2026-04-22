# MinIO Seed Data Format

The `MigrationService` reads JSON seed files from MinIO at startup and upserts them into the database idempotently. Upload these files to your MinIO bucket before running migrations.

## MinIO Object Keys

| Entity | Key |
|--------|-----|
| Store Items | `seeds/store-items.json` |
| Skill Nodes | `seeds/skill-nodes.json` |
| Season Reward Rules | `seeds/season-rewards.json` |
| Questions | `seeds/questions.json` |

If a key does not exist the step is silently skipped — you only need to upload the files you want to seed.

---

## `seeds/store-items.json`

Array of store catalog items. Upserted by `sku` (create or update).

```json
[
  {
    "sku": "avatar:cartoon-hero:v1",
    "name": "Cartoon Hero",
    "description": "A bold cartoon-style 3D avatar.",
    "itemType": "avatar",
    "priceCoins": 500,
    "priceDiamonds": 0,
    "grantQuantity": 1,
    "maxPerPlayer": 1,
    "isActive": true,
    "sortOrder": 10,
    "mediaKey": "avatars/cartoon-hero-v1",
    "thumbnailUrl": "https://cdn.example.com/avatars/cartoon-hero-thumb.png",
    "isFeatured": true,
    "version": "1.0.0"
  },
  {
    "sku": "powerup:skip",
    "name": "Question Skip",
    "description": "Skip any question once.",
    "itemType": "powerup",
    "priceCoins": 50,
    "priceDiamonds": 0,
    "grantQuantity": 1,
    "maxPerPlayer": 0,
    "isActive": true,
    "sortOrder": 1,
    "mediaKey": null,
    "thumbnailUrl": null,
    "isFeatured": false,
    "version": null
  }
]
```

**Field notes:**
- `sku` — stable unique identifier; used as the upsert key
- `itemType` — routing type: `"avatar"`, `"powerup"`, `"cosmetic"`, etc.
- `grantQuantity` — quantity granted per purchase; defaults to 1 if 0 or omitted
- `maxPerPlayer` — 0 = unlimited purchases
- `mediaKey` — object storage key prefix (`.zip` appended for avatar archives)

---

## `seeds/skill-nodes.json`

Array of skill tree nodes. Upserted by `key` (create or update).

```json
[
  {
    "key": "knowledge:recall-boost:1",
    "branch": "Knowledge",
    "tier": 1,
    "title": "Recall Boost I",
    "description": "Increase XP gained from correct answers by 5%.",
    "prereqKeys": [],
    "costs": [
      { "currency": "Coins", "amount": 100 }
    ],
    "effects": {
      "xp_multiplier": 1.05
    }
  },
  {
    "key": "strategy:time-extend:1",
    "branch": "Strategy",
    "tier": 1,
    "title": "Time Extend I",
    "description": "Add 2 seconds to your answer timer.",
    "prereqKeys": [],
    "costs": [
      { "currency": "Coins", "amount": 150 }
    ],
    "effects": {
      "timer_bonus_seconds": 2
    }
  }
]
```

**Field notes:**
- `key` — stable unique node key; used as the upsert key
- `branch` — `"Knowledge"`, `"Strategy"`, or `"Powerups"` (case-insensitive)
- `costs[].currency` — `"Coins"` or `"Diamonds"` (case-insensitive)
- `effects` — arbitrary key/value map consumed by the gameplay engine

---

## `seeds/season-rewards.json`

Array of reward rules for each season tier rank. Created only if the `(tier, maxTierRank)` pair does not already exist.

```json
[
  { "tier": 1, "maxTierRank": 100, "rewardXp": 0,    "rewardCoins": 25  },
  { "tier": 2, "maxTierRank": 50,  "rewardXp": 0,    "rewardCoins": 75  },
  { "tier": 3, "maxTierRank": 25,  "rewardXp": 50,   "rewardCoins": 150 },
  { "tier": 4, "maxTierRank": 10,  "rewardXp": 100,  "rewardCoins": 300 },
  { "tier": 5, "maxTierRank": 1,   "rewardXp": 250,  "rewardCoins": 750 }
]
```

**Field notes:**
- `(tier, maxTierRank)` — composite upsert key; existing rows are not updated
- `tier` — matches the Tier ordinal (1=Bronze … 5=Diamond)
- `maxTierRank` — top-N player cutoff for this reward bracket

> **Migration required:** `SeasonRewardRule` is a new table (`season_reward_rules`). Run `dotnet ef migrations add AddSeasonRewardRules` before seeding.

---

## `seeds/questions.json`

Array of quiz questions. Upserted by `text` (create or update). Each question must have at least one option; the `correctOptionId` must match one of the `options[].optionId` values.

```json
[
  {
    "text": "What is the capital of France?",
    "category": "Geography",
    "difficulty": "Easy",
    "correctOptionId": "A",
    "mediaKey": null,
    "status": "Approved",
    "options": [
      { "optionId": "A", "text": "Paris" },
      { "optionId": "B", "text": "London" },
      { "optionId": "C", "text": "Berlin" },
      { "optionId": "D", "text": "Madrid" }
    ],
    "tags": ["europe", "capitals"]
  },
  {
    "text": "Which element has atomic number 79?",
    "category": "Science",
    "difficulty": "Medium",
    "correctOptionId": "C",
    "mediaKey": null,
    "status": "Approved",
    "options": [
      { "optionId": "A", "text": "Silver" },
      { "optionId": "B", "text": "Platinum" },
      { "optionId": "C", "text": "Gold" },
      { "optionId": "D", "text": "Copper" }
    ],
    "tags": ["chemistry", "elements"]
  }
]
```

**Field notes:**
- `text` — unique question text used as the upsert key (trimmed)
- `difficulty` — `"Easy"`, `"Medium"`, or `"Hard"` (case-insensitive)
- `status` — `"Draft"`, `"Approved"`, `"Rejected"`, or `"Archived"`
- `options` — replaced entirely on update; `optionId` is a stable string like `"A"`, `"B"`, etc.
- `tags` — replaced entirely on update

---

## Uploading Seed Files to MinIO

Using the MinIO CLI (`mc`):

```bash
mc alias set local http://localhost:9000 minioadmin minioadmin
mc cp seeds/store-items.json   local/tycoon/seeds/store-items.json
mc cp seeds/skill-nodes.json   local/tycoon/seeds/skill-nodes.json
mc cp seeds/season-rewards.json local/tycoon/seeds/season-rewards.json
mc cp seeds/questions.json     local/tycoon/seeds/questions.json
```

Replace `tycoon` with your configured bucket name (`Minio:Bucket` in `appsettings`).

Then run the migration service:

```bash
dotnet run --project Tycoon.MigrationService
```

The seeder runs at every startup but is fully idempotent — re-uploading an updated file and restarting the service will apply the delta.
