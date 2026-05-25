-- Player lookup short-code table.
-- Apply before enabling /admin/player-lookup/resolve outside EnsureCreated dev/test databases.

CREATE TABLE IF NOT EXISTS player_lookup_codes (
    "Id" uuid PRIMARY KEY,
    "PlayerId" uuid NOT NULL,
    "UserId" uuid NULL,
    "ShortCode" varchar(6) NOT NULL,
    "CreatedAtUtc" timestamptz NOT NULL,
    "UpdatedAtUtc" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_player_lookup_codes_PlayerId"
    ON player_lookup_codes ("PlayerId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_player_lookup_codes_ShortCode"
    ON player_lookup_codes ("ShortCode");

CREATE INDEX IF NOT EXISTS "IX_player_lookup_codes_UserId"
    ON player_lookup_codes ("UserId");
