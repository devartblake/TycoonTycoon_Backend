# Backend Asset Installer

The backend asset installer uploads seed JSON and media/frontend assets to the configured MinIO bucket. Database imports still run through the migration service; the installer only places the source files in object storage.

## Usage

```powershell
./scripts/install-backend-assets.ps1 `
  -BundleRoot ./path/to/install-bundle `
  -FrontendAssetsRoot ./path/to/frontend/dist `
  -Mode docker `
  -RunMigration
```

Use `-DryRun` to validate the manifest and print the upload plan without requiring MinIO access or the `mc` client.

For real uploads, install the MinIO client (`mc`) and configure MinIO through `docker/.env` or process environment variables. The default bucket is `synaptix-assets`.

The Django operator dashboard also exposes a native installer page at `/installer`. It accepts an asset ZIP, validates or generates the manifest, routes uploads through the backend `/admin/storage` policy endpoints, and then shows the migration-service command to import seed JSON from MinIO. Operators need `storage:write`; raw MinIO credentials are not configured in Django or exposed to the browser.

The dashboard storage console is available at:

- `/storage/objects` for approved-prefix object browsing and metadata drill-down.
- `/storage/upload` for policy-validated uploads into approved MinIO prefixes.

The current `assets.zip` bundle was extracted to:

```text
c:\Users\lmxbl\StudioProjects\trivia_tycoon\installer-bundle
```

The generated manifest is:

```text
c:\Users\lmxbl\StudioProjects\trivia_tycoon\installer-bundle\installer.manifest.json
```

## Manifest

Place `installer.manifest.json` in the bundle root:

```json
{
  "seeds": {
    "storeItems": "seeds/store-items.json",
    "skillNodes": "seeds/skill-nodes.json",
    "seasonRewards": "seeds/season-rewards.json",
    "questions": "seeds/questions.json"
  },
  "assets": [
    {
      "source": "media/avatars/default.glb",
      "key": "avatars/default.glb",
      "contentType": "model/gltf-binary"
    },
    {
      "source": "media/songs/theme.mp3",
      "key": "songs/theme.mp3"
    }
  ]
}
```

Seed keys default to the migration service paths:

- `seeds/store-items.json`
- `seeds/skill-nodes.json`
- `seeds/season-rewards.json`
- `seeds/questions.json`

Assets can either provide an explicit `key` or let the installer infer a stable prefix. Backend policy allows only approved prefixes such as `seeds/`, `avatars/`, `avatar-packages/`, `songs/`, `audio/`, `models/`, `images/`, `videos/`, `frontend/assets/`, and `questions/`.
