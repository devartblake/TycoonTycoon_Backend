# Flutter Web Static Host

Production Traefik routes `https://app.synaptixplay.com` to this Nginx service.

Build the Flutter app from `trivia_tycoon`, then copy the contents of `build/web`
into `docker/flutter-web/dist` before running the production Compose stack.

The `dist` directory is intentionally ignored by Git because it is a release
artifact, not source.
