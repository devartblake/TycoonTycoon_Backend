# Synaptix Packet B Implementation Guide
## Phases 2-3: Mode/Theme Foundation + Shell/Navigation Upgrade

**Packet scope:** build the multi-audience Synaptix experience layer and transform the current shell into a real platform home  
**Covers:** Phase 2 and Phase 3  
**Assumption:** Phase 1 visible brand reframe is complete or in progress  
**Do not touch yet:** package roots, backend namespaces, DTO names, API routes, migrations, deep file renames

---

## 1. Packet B purpose

Packet B is where Synaptix stops being just a renamed app and starts becoming a **mode-aware platform**.

This packet does two things:

1. **Phase 2 - Mode and Theme Foundation**  
   Introduce the architecture that allows one app to present differently for kids, teens, and adults.

2. **Phase 3 - Shell and Navigation Upgrade**  
   Turn the current menu shell into a coherent **Synaptix Hub** with stronger information architecture and launch surfaces.

This packet is critical because it creates the foundation needed for later surface rebrands:
- Arena
- Labs
- Pathways
- Journey
- Circles
- Command

Without Packet B, later phases risk becoming a collection of renamed screens rather than one product family.

---

## 2. Packet B success criteria

Packet B is successful when all of the following are true:

- the app can derive and store a Synaptix presentation mode
- the UI can render mode-aware differences without forking the app
- the existing theme system remains intact and extended rather than replaced
- users land on a real Synaptix Hub instead of a placeholder game menu
- navigation feels like a platform shell instead of disconnected modules
- future phases can plug Arena, Labs, Pathways, Journey, and Circles into a stable home surface

---

## 3. Phase 2 - Mode and Theme Foundation

### 3.1 Objective
Introduce a low-risk Synaptix mode and theme layer that supports:
- kids
- teen
- adult

This phase should be additive, not destructive.

### 3.2 Design rule
Do **not** replace the current `AppTheme` model in this phase.  
Extend it.

### 3.3 Why this phase matters
Synaptix naturally reads older than Trivia Tycoon.  
The solution is not a separate kids app.  
The solution is a single product with adaptive presentation.

---

## 4. Phase 2 file targets

### Primary frontend targets
- `lib/core/theme/themes.dart`
- `lib/core/theme/styles.dart`
- `lib/core/services/settings/player_profile_service.dart`
- `lib/main.dart`
- new additive files under:
  - `lib/synaptix/mode/`
  - `lib/synaptix/theme/`
  - `lib/synaptix/brand/`
  - `lib/synaptix/copy/`
  - `lib/synaptix/widgets/`

### Secondary frontend targets
- onboarding files that already collect age group
- settings/profile surfaces where a preferred mode can later be exposed

### Backend target
Only additive profile preference support if needed later.
No required backend contract changes for this packet.

---

## 5. Phase 2 architecture additions

### 5.1 Add `SynaptixMode`
Create a small enum as the canonical presentation mode model.

```dart
enum SynaptixMode {
  kids,
  teen,
  adult,
}
```

### 5.2 Add a mode-mapping helper
This should map the current saved age group into a Synaptix mode **without** changing existing age-group persistence.

```dart
SynaptixMode mapAgeGroupToSynaptixMode(String? ageGroup) {
  switch (ageGroup?.toLowerCase()) {
    case 'kids':
    case 'child':
    case 'elementary':
    case 'k-5':
      return SynaptixMode.kids;
    case 'teen':
    case 'teens':
    case 'middle':
    case 'middle school':
      return SynaptixMode.teen;
    case 'adult':
    default:
      return SynaptixMode.adult;
  }
}
```

### 5.3 Add a provider
Introduce a provider that can derive mode from saved profile/age data but still support later manual overrides.

```dart
final synaptixModeProvider = StateProvider<SynaptixMode>((ref) {
  return SynaptixMode.teen;
});
```

### 5.4 Add a theme extension
Add a `SynaptixTheme` extension instead of replacing your existing theme model.

```dart
@immutable
class SynaptixTheme extends ThemeExtension<SynaptixTheme> {
  final Color primarySurface;
  final Color accentGlow;
  final bool useHighEnergyMotion;
  final bool useSoftCorners;
  final double cardRadius;

  const SynaptixTheme({
    required this.primarySurface,
    required this.accentGlow,
    required this.useHighEnergyMotion,
    required this.useSoftCorners,
    required this.cardRadius,
  });

  @override
  SynaptixTheme copyWith({
    Color? primarySurface,
    Color? accentGlow,
    bool? useHighEnergyMotion,
    bool? useSoftCorners,
    double? cardRadius,
  }) {
    return SynaptixTheme(
      primarySurface: primarySurface ?? this.primarySurface,
      accentGlow: accentGlow ?? this.accentGlow,
      useHighEnergyMotion: useHighEnergyMotion ?? this.useHighEnergyMotion,
      useSoftCorners: useSoftCorners ?? this.useSoftCorners,
      cardRadius: cardRadius ?? this.cardRadius,
    );
  }

  @override
  ThemeExtension<SynaptixTheme> lerp(
    covariant ThemeExtension<SynaptixTheme>? other,
    double t,
  ) {
    if (other is! SynaptixTheme) return this;
    return SynaptixTheme(
      primarySurface: Color.lerp(primarySurface, other.primarySurface, t)!,
      accentGlow: Color.lerp(accentGlow, other.accentGlow, t)!,
      useHighEnergyMotion: t < 0.5 ? useHighEnergyMotion : other.useHighEnergyMotion,
      useSoftCorners: t < 0.5 ? useSoftCorners : other.useSoftCorners,
      cardRadius: cardRadius + (other.cardRadius - cardRadius) * t,
    );
  }
}
```

---

## 6. Phase 2 recommended file additions

### 6.1 `lib/synaptix/mode/synaptix_mode.dart`
```dart
enum SynaptixMode {
  kids,
  teen,
  adult,
}
```

### 6.2 `lib/synaptix/mode/synaptix_mode_mapper.dart`
```dart
import 'synaptix_mode.dart';

SynaptixMode mapAgeGroupToSynaptixMode(String? ageGroup) {
  switch (ageGroup?.toLowerCase()) {
    case 'kids':
    case 'child':
    case 'elementary':
    case 'k-5':
      return SynaptixMode.kids;
    case 'teen':
    case 'teens':
    case 'middle':
    case 'middle school':
      return SynaptixMode.teen;
    case 'adult':
    default:
      return SynaptixMode.adult;
  }
}
```

### 6.3 `lib/synaptix/theme/synaptix_theme_extension.dart`
Use the `SynaptixTheme` class shown above.

### 6.4 `lib/synaptix/theme/synaptix_theme_presets.dart`
```dart
import 'package:flutter/material.dart';
import 'synaptix_theme_extension.dart';

const kidsSynaptixTheme = SynaptixTheme(
  primarySurface: Color(0xFFF7FAFF),
  accentGlow: Color(0xFF62D0FF),
  useHighEnergyMotion: true,
  useSoftCorners: true,
  cardRadius: 20,
);

const teenSynaptixTheme = SynaptixTheme(
  primarySurface: Color(0xFF0D1220),
  accentGlow: Color(0xFF5B7CFF),
  useHighEnergyMotion: true,
  useSoftCorners: false,
  cardRadius: 14,
);

const adultSynaptixTheme = SynaptixTheme(
  primarySurface: Color(0xFF11151C),
  accentGlow: Color(0xFF4FA3C8),
  useHighEnergyMotion: false,
  useSoftCorners: false,
  cardRadius: 12,
);
```

### 6.5 `lib/synaptix/mode/synaptix_mode_provider.dart`
```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'synaptix_mode.dart';

final synaptixModeProvider = StateProvider<SynaptixMode>((ref) {
  return SynaptixMode.teen;
});
```

---

## 7. Phase 2 modifications to existing files

### 7.1 `lib/core/theme/themes.dart`
Add documentation note plus optional helper integration point.

Recommended additive comment:
```dart
// Synaptix Packet B note:
// AppTheme remains the base theme model.
// SynaptixTheme is an additive mode-aware extension layered on top of it.
```

Do not remove or rename current `ThemeType` values.

### 7.2 `lib/core/services/settings/player_profile_service.dart`
Additive fields only.

Recommended future-safe additions:
```dart
String? synaptixMode;
String? preferredHomeSurface;
bool? reducedMotion;
String? tonePreference;
```

Important rule:
- add new keys
- do not repurpose old keys
- do not rename current profile fields

### 7.3 `lib/main.dart`
Bootstrap the Synaptix mode after age group/profile is loaded.

Recommended pattern:
```dart
final initialMode = mapAgeGroupToSynaptixMode(savedAgeGroup);
// push initialMode into synaptixModeProvider override or state initialization
```

Do not rewrite bootstrap structure or provider initialization order.

---

## 8. Phase 2 mode behavior rules

### Kids mode
Use:
- larger cards
- softer corners
- simpler labels
- brighter surfaces
- fewer simultaneous metrics
- higher icon support

### Teen mode
Use:
- strongest Synaptix identity
- action-forward shell
- progression cues
- neon/accent emphasis
- stronger launch cards for Arena, Labs, and Pathways

### Adult mode
Use:
- cleaner layout
- more restrained animation
- tighter hierarchy
- stronger emphasis on progress, ranking, and mastery

---

## 9. Phase 2 deliverables

At the end of Phase 2, another AI should be able to say:

- a canonical `SynaptixMode` exists
- age group can map to mode
- a mode provider exists
- Synaptix theme extensions exist
- no existing theme system has been broken
- profile settings can later store preferred mode and preferred home surface
- the project is ready for the Synaptix Hub build

---

## 10. Phase 2 risks

| Risk | Why it matters | Mitigation |
|---|---|---|
| replacing instead of extending theme system | destabilizes styling across app | add `SynaptixTheme`, do not replace `AppTheme` |
| tying mode too tightly to age | blocks user override later | treat age as default, not permanent lock |
| adding too many profile changes too early | creates persistence risk | use additive fields only |
| mode-specific UI changes leaking too early | causes visual inconsistency | keep Phase 2 focused on foundation, not surface-wide redesign |

---

## 11. Phase 2 exit criteria

Phase 2 is complete when:
- a Synaptix mode model exists
- a mode provider exists
- theme extension presets exist
- `main.dart` can derive the default mode safely
- no existing theme/type/provider structure has been broken

---

## 12. Phase 3 - Shell and Navigation Upgrade

### 12.1 Objective
Turn the current menu entry into a real Synaptix Hub and improve shell-level information architecture.

### 12.2 Product rationale
The current home/menu experience is the fastest way to communicate that the app is now a platform.

If the home surface still feels placeholder or trivia-only, the rebrand will feel shallow even if the rest of the app is renamed.

---

## 13. Phase 3 file targets

### Primary frontend targets
- `lib/screens/menu/game_menu_screen.dart`
- `lib/core/navigation/app_router.dart`
- shell-level widgets used by home/navigation
- shared card/button widgets if needed

### Secondary frontend targets
- widgets that render quick access blocks
- dashboard-style summary widgets
- optional welcome/header widgets

### Backend targets
No required backend changes for shell upgrade.
Optional later support:
- preferred home surface persistence
- hub analytics
- recommended launch modules

---

## 14. Phase 3 shell design goals

The Synaptix Hub should communicate:
- what the product is
- what the player should do next
- where the main surfaces live
- how progress is tracked

It should not be:
- a generic dashboard wall
- a dense admin-style control panel
- a placeholder screen with renamed text only

---

## 15. Phase 3 recommended Synaptix Hub structure

### Required sections
1. **Welcome header**
2. **Daily challenge or mission strip**
3. **Continue playing card**
4. **Quick launch grid**
5. **Progress snapshot**
6. **Economy/reward access**
7. **Mode-aware emphasis area**

### Recommended quick launch cards
- Arena
- Labs
- Pathways
- Circles
- Journey
- Store/Rewards

### Mode-aware card emphasis
- kids: challenge + practice + profile progress
- teen: Arena + Pathways + Labs
- adult: Arena + Journey + Pathways

---

## 16. Phase 3 implementation approach

### 16.1 Do not over-engineer first pass
The first complete Hub should be:
- structurally strong
- readable
- mode-aware
- easy to extend

Do not block Phase 3 by trying to make the final perfect hub on the first pass.

### 16.2 Recommended file strategy
Keep `game_menu_screen.dart` as the entry point for now.  
Refactor inside it or split small widgets if needed.

Suggested supporting widgets:
- `synaptix_hub_header.dart`
- `synaptix_hub_card.dart`
- `synaptix_progress_snapshot.dart`
- `synaptix_mode_banner.dart`

These can live under:
- `lib/synaptix/widgets/`
or
- the local menu widgets folder if one already exists

---

## 17. Phase 3 reference implementation for `game_menu_screen.dart`

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:trivia_tycoon/synaptix/mode/synaptix_mode.dart';
import 'package:trivia_tycoon/synaptix/mode/synaptix_mode_provider.dart';

class GameMenuScreen extends ConsumerWidget {
  const GameMenuScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final mode = ref.watch(synaptixModeProvider);
    final theme = Theme.of(context);

    final hubCards = _buildCardsForMode(mode);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Synaptix Hub'),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _HubHeader(mode: mode),
            const SizedBox(height: 16),
            _DailyChallengeStrip(mode: mode),
            const SizedBox(height: 20),
            Text(
              'Continue your journey',
              style: theme.textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: hubCards,
            ),
            const SizedBox(height: 24),
            _ProgressSnapshot(mode: mode),
          ],
        ),
      ),
    );
  }

  List<Widget> _buildCardsForMode(SynaptixMode mode) {
    final items = switch (mode) {
      SynaptixMode.kids => const [
          ('Play', 'Jump into a fun challenge'),
          ('Labs', 'Practice and explore'),
          ('Journey', 'See your progress'),
          ('Rewards', 'Check your unlocks'),
        ],
      SynaptixMode.teen => const [
          ('Arena', 'Compete and climb'),
          ('Pathways', 'Grow your skills'),
          ('Labs', 'Train and improve'),
          ('Circles', 'Connect with others'),
        ],
      SynaptixMode.adult => const [
          ('Arena', 'Advance your standing'),
          ('Journey', 'Review your progress'),
          ('Pathways', 'Refine your strategy'),
          ('Labs', 'Sharpen your performance'),
        ],
    };

    return items
        .map((item) => _HubCard(title: item.$1, subtitle: item.$2))
        .toList();
  }
}

class _HubHeader extends StatelessWidget {
  final SynaptixMode mode;
  const _HubHeader({required this.mode});

  @override
  Widget build(BuildContext context) {
    final title = switch (mode) {
      SynaptixMode.kids => 'Welcome to Synaptix Play',
      SynaptixMode.teen => 'Welcome to Synaptix',
      SynaptixMode.adult => 'Welcome back to Synaptix',
    };

    final subtitle = switch (mode) {
      SynaptixMode.kids => 'Play, learn, and unlock new paths.',
      SynaptixMode.teen => 'Compete, train, and build your edge.',
      SynaptixMode.adult => 'Train your mind, track progress, and advance.',
    };

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                fontWeight: FontWeight.bold,
              ),
        ),
        const SizedBox(height: 8),
        Text(subtitle),
      ],
    );
  }
}

class _DailyChallengeStrip extends StatelessWidget {
  final SynaptixMode mode;
  const _DailyChallengeStrip({required this.mode});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        title: const Text('Daily Challenge'),
        subtitle: Text(
          mode == SynaptixMode.kids
              ? 'Complete today’s fun challenge.'
              : 'Complete today’s challenge and keep your momentum.',
        ),
        trailing: const Icon(Icons.arrow_forward_ios),
      ),
    );
  }
}

class _ProgressSnapshot extends StatelessWidget {
  final SynaptixMode mode;
  const _ProgressSnapshot({required this.mode});

  @override
  Widget build(BuildContext context) {
    final title = switch (mode) {
      SynaptixMode.kids => 'Your progress',
      SynaptixMode.teen => 'Your momentum',
      SynaptixMode.adult => 'Performance snapshot',
    };

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Text(
          title,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
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

---

## 18. Phase 3 router guidance

### 18.1 Keep route paths stable
Do not rename:
- `/leaderboard`
- `/arcade`
- `/profile`
- other route paths

at this stage.

### 18.2 Change display-facing labels first
What can change safely:
- app bar titles
- home card labels
- shell-level group labels
- comments/documentation
- visible bottom/tab labels if they are not contract-sensitive

### 18.3 Suggested route display mapping
- leaderboard route -> Arena
- arcade route -> Labs
- skill tree route -> Pathways
- messages/group chat -> Circles
- profile -> Journey
- admin -> Command

Keep the underlying route wiring stable unless the router clearly separates display labels from internal names.

---

## 19. Phase 3 additive settings/profile support

### Recommended future-safe additions
Packet B can optionally prepare for:
- preferred home surface
- preferred shell density
- reduced motion
- mode override

Do not block shell upgrade on full settings work.
A placeholder or TODO is acceptable if the model path is clear.

---

## 20. Phase 3 deliverables

At the end of Phase 3, another AI should be able to say:

- there is a real Synaptix Hub
- the hub adapts to kids/teen/adult modes
- navigation reads as a platform shell
- the quick-launch structure exists
- the home surface is ready for later Arena/Labs/Pathways/Journey/Circles refinements
- route stability has been preserved

---

## 21. Phase 3 risks

| Risk | Why it matters | Mitigation |
|---|---|---|
| overbuilding the hub in first pass | slows execution and increases bugs | build a clean expandable shell first |
| changing route paths too early | creates regressions | keep route contracts stable |
| weak mode differences | reduces value of Packet B | make header/cards/progress copy mode-aware |
| dashboard-style overload | harms kids and casual users | keep the hub focused and readable |

---

## 22. Phase 3 exit criteria

Phase 3 is complete when:
- the user lands on Synaptix Hub
- quick-launch cards are visible and coherent
- the header and progress language adapt by mode
- the app reads as a platform shell rather than a placeholder game menu
- no route path regressions have been introduced

---

## 23. Packet B implementation order

Another AI should execute Packet B in this exact order:

1. add `SynaptixMode`
2. add mode mapper
3. add mode provider
4. add `SynaptixTheme` extension
5. add mode presets
6. prepare additive profile fields
7. bootstrap mode in `main.dart`
8. refactor `game_menu_screen.dart` into Synaptix Hub
9. add mode-aware hub header
10. add daily challenge strip
11. add quick-launch cards
12. add progress snapshot
13. audit router labels for safe display-only updates

---

## 24. Packet B handoff checklist

Before moving to Packet C, confirm:

- `SynaptixMode` exists
- mode mapping exists
- theme extension exists
- no existing theme types were removed
- no package roots were renamed
- no route paths were changed
- Synaptix Hub exists
- mode-aware shell copy is present
- future surfaces can plug into the hub cleanly

---

## 25. Recommended next packet after B

Once Packet B is complete, move to **Packet C**:
- Phase 4: Core Feature Surface Rebrand
- Phase 5: Backend Product-Language Alignment

That packet is where Arena, Labs, Pathways, Journey, Circles, and Command become the dominant visible feature surfaces.
