# Synaptix Phase 1 Implementation Patches
## Product-surface rebrand patches for the current Flutter frontend and backend-visible branding surfaces

**Phase focus:** visible brand reframe only  
**Do not touch yet:** package roots, namespaces, DTO names, API routes, migrations, project names

---

## 1. Grouping strategy answer

Yes — the phases can be grouped in pairs without losing execution quality, with one caveat:

- **Recommended grouping:** `(0-1)`, `(2-3)`, `(4-5)`, `(6-7)`, and **Phase 8 as a standalone optional packet**
- The reason to keep **Phase 8** separate is that it is a higher-risk, optional technical cleanup phase, not a normal product-execution phase.
- If you want exactly four two-phase packets, I would group them as:
  - Packet A: `0-1`
  - Packet B: `2-3`
  - Packet C: `4-5`
  - Packet D: `6-7`
  - and keep `8` as an explicit decision gate

That structure is workable and still precise enough for another AI system.

---

## 2. Phase 1 objective

Make the product visibly read as **Synaptix** at first touch, while preserving the current app architecture and backend contracts.

This phase should change:
- app-facing brand strings
- splash/welcome copy
- logo text
- app shell labels
- obvious “Trivia Tycoon” wording in UI
- backend-visible documentation titles and dashboard headings where product-facing

This phase should **not** change:
- `package:trivia_tycoon/...`
- `Tycoon.*` namespaces
- API paths
- DTO/model names
- database or migration assets
- route constants unless absolutely necessary

---

## 3. Target files for Phase 1

### Frontend - priority files
- `lib/main.dart`
- `lib/widgets/app_logo.dart`
- `lib/screens/splash_variants/main_splash.dart`
- `lib/screens/menu/game_menu_screen.dart`
- `lib/core/navigation/app_router.dart` (display labels only if present)
- any app title/about/settings screens that visibly say “Trivia Tycoon”

### Backend-visible surfaces
- `Tycoon.Backend.Api/Program.cs` or Swagger config location
- `Tycoon.OperatorDashboard/...`
- `Tycoon.OperatorDashboard.Vue/...`
- product-facing docs headings only

---

## 4. Patch 1 - `lib/widgets/app_logo.dart`

### Intent
Change the visible product name from **Trivia Tycoon** to **Synaptix** without changing the image asset pipeline yet.

### Existing issue
This file hardcodes:
- `Trivia Tycoon`
- tagline `Challenge Your Mind`

### Recommended replacement
- main title: `Synaptix`
- tagline: `Train. Compete. Grow.`
- compact title: `Synaptix`

### Patch
```diff
@@
-          Text(
-            'Trivia Tycoon',
+          Text(
+            'Synaptix',
@@
-          Text(
-            'Challenge Your Mind',
+          Text(
+            'Train. Compete. Grow.',
@@
-        Text(
-          'Trivia Tycoon',
+        Text(
+          'Synaptix',
```

### Notes
- keep `tTriviaGameImage` unchanged in Phase 1
- keep fallback `Icons.psychology` - it already fits Synaptix well
- do not rename widget classes yet

---

## 5. Patch 2 - `lib/screens/splash_variants/main_splash.dart`

### Intent
Bring splash copy in line with the new master brand without redesigning the animation system yet.

### Existing issue
The splash is structurally good, but copy is still generic and tied to the old brand shell through `AppLogo`.

### Recommended copy updates
- loading copy: `Loading your Synaptix experience...`
- footer branding line can remain if desired, but should not conflict with Synaptix

### Patch
```diff
@@
-                    Text(
-                      'Loading your experience...',
+                    Text(
+                      'Loading your Synaptix experience...',
```

### Optional footer patch
```diff
@@
-                    Text(
-                      'Powered by Theoretical Minds Technology',
+                    Text(
+                      'Powered by Theoretical Minds Technology',
```

No change required there unless you want a stronger Synaptix footer system later.

### Notes
- do not change splash timing in Phase 1
- do not redesign layout yet
- branding is the only target here

---

## 6. Patch 3 - `lib/main.dart`

### Intent
Make the app read as Synaptix immediately while preserving bootstrap behavior.

### Existing issue
The root widget class is still `TriviaTycoonApp`, and crash recovery copy is generic.

### Phase 1 recommendation
At this stage:
- keep class names stable if another AI is implementing fast
- change visible strings only
- optionally add a comment note for later refactor

### Minimal patch
```diff
@@
-        child: TriviaTycoonApp(initialData: (manager, theme)),
+        child: TriviaTycoonApp(initialData: (manager, theme)),
@@
-        child: const TriviaTycoonApp(),
+        child: const TriviaTycoonApp(),
```

No required class rename yet.

### Copy patch inside recovery dialog
```diff
@@
-            const Text('Welcome Back!'),
+            const Text('Welcome back to Synaptix'),
```

### Optional follow-up note
Add a TODO near the root widget:
```dart
// TODO(Synaptix Phase 8): Rename TriviaTycoonApp class after product-layer migration stabilizes.
```

### Notes
- do not rename the widget class in Phase 1 unless you are prepared for wider refactor churn
- keep `userAgeGroupProvider` override logic intact

---

## 7. Patch 4 - `lib/screens/menu/game_menu_screen.dart`

### Intent
Replace the placeholder “Trivia Game” title with the first visible Synaptix Hub shell.

### Existing issue
This screen is very minimal and is the highest-value place to establish the new product framing.

### Recommended first-pass result
Do not jump straight to the full Hub build yet.  
For Phase 1, upgrade it from placeholder to branded shell.

### Proposed replacement file
```dart
import 'package:flutter/material.dart';

class GameMenuScreen extends StatelessWidget {
  const GameMenuScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Synaptix Hub'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Welcome to Synaptix',
              style: theme.textTheme.headlineSmall?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Train your mind, compete in challenges, and build your journey.',
              style: theme.textTheme.bodyMedium,
            ),
            const SizedBox(height: 24),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: const [
                _HubCard(title: 'Arena', subtitle: 'Climb the ladder'),
                _HubCard(title: 'Labs', subtitle: 'Practice and play'),
                _HubCard(title: 'Pathways', subtitle: 'Grow your skills'),
                _HubCard(title: 'Circles', subtitle: 'Connect with others'),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _HubCard extends StatelessWidget {
  final String title;
  final String subtitle;

  const _HubCard({
    required this.title,
    required this.subtitle,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 180,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
              ),
              const SizedBox(height: 6),
              Text(subtitle),
            ],
          ),
        ),
      ),
    );
  }
}
```

### Notes
- this is intentionally modest; full Hub build belongs in Phase 3
- do not wire navigation from every card yet unless desired
- the point is to eliminate placeholder branding immediately

---

## 8. Patch 5 - `lib/core/navigation/app_router.dart`

### Intent
Update user-facing route names and comments only where easy and safe.

### Existing issue
The router already appears broad and dense. Phase 1 should not destabilize it.

### Safe actions
- update comments or shell labels if they are visible
- do **not** rename route paths
- do **not** rename imported files
- do **not** rename GoRoute names if they are used programmatically elsewhere unless confirmed safe

### Patch policy
Use this rule:

> If the router field is only display-facing, rename it.  
> If it may affect navigation logic, defer it.

### Recommended note to add
```dart
// Synaptix Phase 1 note:
// Keep route paths and internal names stable. Update visible screen labels at the widget level first.
```

### Notes
This file should mostly be deferred to Phase 3 unless there are obvious visible labels.

---

## 9. Patch 6 - `lib/core/services/settings/player_profile_service.dart`

### Intent
No visible rename needed yet, but prepare a low-risk additive note for future phases.

### Phase 1 action
Do not change storage keys yet.

### Recommended TODO additions
```dart
// TODO(Synaptix Phase 2):
// Add additive fields for preferred Synaptix mode and preferred home surface.
// Do not rename existing keys during product-surface migration.
```

### Notes
This file is important, but not a visible branding target in Phase 1.
Do not add live schema changes yet unless you are starting Phase 2 immediately afterward.

---

## 10. Patch 7 - `lib/core/theme/themes.dart`

### Intent
Do not perform the Synaptix theme migration yet, but mark this file as the extension point.

### Existing issue
Current theme types are:
- `main`
- `allStar`
- `competition`

That is still valid for now.

### Phase 1 action
Add a documentation comment only:
```dart
// Synaptix migration note:
// Existing AppTheme remains the active theme model in Phase 1.
// SynaptixMode and SynaptixTheme extensions will be introduced additively in Phase 2.
```

### Notes
Do not begin replacing `AppTheme` in Phase 1.

---

## 11. Patch 8 - backend-visible product branding

### 11.1 Swagger / API docs
Update the visible title only.

### Target shape
```csharp
options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Synaptix API",
    Version = "v1",
    Description = "Platform API for Synaptix gameplay, progression, and live competition."
});
```

### 11.2 Operator dashboard
Update:
- top header -> `Synaptix Command`
- section shell branding
- dashboard welcome copy

Do not:
- rename project names
- rename namespaces
- rewrite routing

### 11.3 Vue dashboard
Update:
- app shell title
- visible navigation labels
- landing heading

Do not refactor structure yet.

---

## 12. Search/replace rules for Phase 1

### Safe search/replace
Search:
```text
Trivia Tycoon
```
Replace:
```text
Synaptix
```

Scope:
- Flutter UI widgets
- dashboard headings
- splash/logo text
- docs headings

### Conditional replacements
Search:
```text
Trivia Game
```
Replace:
```text
Synaptix Hub
```

Only in:
- menu/home shell
- user-facing placeholders

### Forbidden in Phase 1
Never bulk replace:
- `package:trivia_tycoon`
- `Tycoon.Backend`
- `Tycoon.OperatorDashboard`
- route paths
- DTO/property names
- database/table names
- migration identifiers

---

## 13. Phase 1 deliverables

At the end of Phase 1, another AI should be able to say all of the following are true:

- splash visually and textually reads as Synaptix
- app logo text reads as Synaptix
- menu/home no longer says “Trivia Game”
- first-touch branding is coherent
- backend-visible docs/dashboard headers no longer feel disconnected from the frontend brand
- no technical namespace churn has been introduced

---

## 14. Phase 1 risks

| Risk | Why it matters | Mitigation |
|---|---|---|
| mixed old/new copy remains | weakens rebrand credibility | use the rename matrix and scan visible shells first |
| router changes spread too early | can break navigation | keep router logic stable in Phase 1 |
| theme work starts too early | creates duplicate systems | defer mode/theme architecture to Phase 2 |
| backend rename scope grows | delays execution | only change product-facing titles and headings |

---

## 15. Phase 1 exit criteria

Phase 1 is complete when:
- the app launches under Synaptix branding
- the logo and splash are aligned
- the menu/home entry no longer looks placeholder or old-brand
- no package roots or backend namespaces were renamed
- the codebase remains stable and ready for Phase 2

---

## 16. Recommended next move after Phase 1

Immediately after Phase 1, start **Phase 2**:
- `SynaptixMode`
- mode provider
- theme extension
- age group -> mode mapping
- additive profile preferences

That is the phase where Synaptix becomes credible across K-12, teens, and adults under one brand.
