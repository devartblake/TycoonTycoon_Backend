# Rewards Module — Backend Integration Audit

**Date:** 2026-05-08  
**Branch:** `claude/fix-hexagon-alignment-CrQVu`  
**Prepared for:** Backend / API Team  

---

## Overview

This document covers every file in the rewards and spin-wheel subsystems, identifies what has already been wired to real providers, what still reads from local storage only, and what new API endpoints or providers are needed to complete server-side integration.

---

## Status Key

| Symbol | Meaning |
|--------|---------|
| ✅ | Fully wired to backend provider |
| 🟡 | Partially wired — local storage fallback in use |
| 🔴 | Hardcoded / local-only — backend endpoint needed |

---

## Rewards Screens (`lib/screens/rewards/`)

### `reward_screen.dart` — Main Rewards Hub  🟡

**What was wired (this session):**
- Daily claim status now reads from `RewardSettingsService.isDailyRewardAvailable()` (Hive-backed).
- Claiming calls `RewardSettingsService.claimDailyReward()` which records the date and adds coins.
- Coin balance updated via `coinBalanceProvider` after each claim.
- Spin statistics loaded via `spinStatisticsProvider` (wraps `EnhancedSpinTracker`).

**Still local-only:**
| Data | Current source | Needed API |
|------|---------------|-----------|
| Daily reward definitions (icon, amount, type) | Hardcoded in UI (`'Daily Mystery Box'`, 100 coins) | `GET /rewards/daily-config` — returns reward definition for the day |
| Daily claim persistence | Hive (`lastDailyReward` key) | `POST /rewards/daily/claim` — persists claim server-side; prevents multi-device double-claim |
| Spin stats | `EnhancedSpinTracker` local cache | `GET /spins/stats/{userId}` — server-authoritative daily/weekly counts |

**Provider to create (backend team):**
```dart
// Fetches today's server-configured daily reward
final dailyRewardConfigProvider = FutureProvider<DailyRewardConfig>((ref) async {
  final userId = await ref.read(currentUserIdProvider.future);
  return ref.read(storeServiceProvider).getDailyRewardConfig(userId: userId);
});
```

---

### `weekly_rewards_widget.dart` — 7-Day Login Streak  🟡

**What was wired (this session):**
- Converted from `StatelessWidget` to `ConsumerStatefulWidget`.
- Reads `RewardSettingsService.isDailyRewardAvailable()` to determine today's claimable state.
- Claim taps call `RewardSettingsService.claimDailyReward()` + update `coinBalanceProvider`.
- 7-day cycle progress tracked via two `AppSettings` keys: `weeklyLoginDay` (1–7) and `weeklyLoginCycleStart` (ISO date).

**Still local-only:**
| Data | Current source | Needed API |
|------|---------------|-----------|
| Which days are claimed | `AppSettings` Hive keys on-device only | `GET /rewards/weekly-streak/{userId}` — returns `{ currentDay: int, cycleStart: date, claimedDays: [1,2,3] }` |
| Weekly reward definitions (what each day gives) | Hardcoded in widget (Day 1 = 100 coins, Day 2 = 5 gems, etc.) | `GET /rewards/weekly-schedule` — returns configurable reward schedule per day |
| Claim persistence | Hive only | `POST /rewards/weekly/claim` with `{ userId, day }` |

**Multi-device impact:** A user who logs in on a second device will lose their streak because it's stored in Hive locally. This is the highest-priority backend gap in this widget.

**Provider to create (backend team):**
```dart
final weeklyStreakProvider = FutureProvider<WeeklyStreakData>((ref) async {
  final userId = await ref.read(currentUserIdProvider.future);
  return ref.read(storeServiceProvider).getWeeklyStreak(userId: userId);
});

final weeklyRewardScheduleProvider = FutureProvider<List<WeeklyRewardDay>>((ref) async {
  return ref.read(storeServiceProvider).getWeeklyRewardSchedule();
});
```

---

### `mission_screen.dart` — Missions Center  🟡

**What was wired (this session):**
- User ID stub `'current_user_id'` replaced — now reads from `currentUserIdProvider` (auth service).
- Mission list now watches `liveMissionsProvider` from `HybridMissionNotifier`.
- Daily tab shows active missions; Weekly tab shows completed missions.
- Header XP is summed from real mission reward values.

**Still local-only / gaps:**
| Data | Current source | Needed API |
|------|---------------|-----------|
| Mission list | `HybridMissionNotifier` — loads from JSON assets if backend fails | `GET /missions/{userId}` — server-authoritative active missions |
| Mission progress | JSON local progress (reset on reinstall) | `PUT /missions/{userId}/{missionId}/progress` — persisted server-side |
| Mission claim | No claim endpoint wired | `POST /missions/{userId}/{missionId}/claim` — awards XP/coins |
| `currentUserIdProvider` in `hybrid_mission_state.dart` | Returns hardcoded `'current-user-id'` stub | **Fix this stub** — import `currentUserIdProvider` from `profile_providers.dart` |
| Swap mission | `HybridMissionNotifier.swapMission()` has backend path | Confirm `DELETE /missions/{userId}/{missionId}` + `POST /missions/{userId}/generate` works |

**Critical fix needed in `hybrid_mission_state.dart` (line 388):**
```dart
// CURRENT (broken stub):
final currentUserIdProvider = Provider<String?>((ref) {
  return 'current-user-id'; // ← Replace this
});

// FIX — import from profile_providers.dart instead:
// Remove local declaration and reference profile_providers.currentUserIdProvider
```

---

### `spin_earn_screen.dart` — Spin & Earn Hub  🟡

**What was wired (this session):**
- Spin statistics (daily count, weekly count, total, limit, remaining) now loaded from `spinStatisticsProvider` instead of six separate `AppSettings` calls.
- `ref.invalidate(spinStatisticsProvider)` called after returning from the wheel screen so all widgets refresh.

**Still local-only:**
| Data | Current source | Needed API |
|------|---------------|-----------|
| Spin count limits | `EnhancedSpinTracker` — local Hive | `GET /spins/config/{userId}` — server-configurable limit (e.g. premium users get more spins) |
| Spin history | `AppSettings.getSpinHistory()` — local | `GET /spins/history/{userId}` — server history for cross-device sync |
| Reward points slider | `AppSettings.getSpinRewardPoints()` — local | Part of `GET /spins/stats/{userId}` response |
| Reset logic (daily/weekly) | Handled by `EnhancedSpinTracker._validateState()` locally | Server should be source of truth for reset timestamps |

**User preference settings (animation, sound, haptic) — intentionally local.** No backend needed.

---

### `presets/reward_step_presets.dart` — Reward Step Definitions  🔴

**Status:** Entirely static constants. No provider, no backend.

**What it contains:** Hardcoded reward progression steps for spin rewards (`dailySpinRewards`), level-up rewards (`levelUpRewards`), and achievement rewards (`achievementRewards`). Each step has a fixed `pointValue`, `quantity`, and `description`.

**Impact:** Cannot A/B test or adjust reward values without a code deploy.

**Needed API:** `GET /rewards/spin-reward-steps` — returns configurable step definitions.

**Provider to create:**
```dart
final rewardStepsProvider = FutureProvider<List<RewardStep>>((ref) async {
  try {
    return await ref.read(storeServiceProvider).getSpinRewardSteps();
  } catch (_) {
    return RewardStepPresets.dailySpinRewards; // fallback to local presets
  }
});
```

---

### `widgets/reward_stepper_slider_widget.dart` — Slider UI Component  ✅

Purely presentational. Accepts `rewardSteps` as a constructor parameter from the parent screen. No hardcoding. No changes needed here — the fix is upstream (see `reward_step_presets.dart` above).

---

## Spin Wheel (`lib/ui_components/spin_wheel/`)

### `ui/screen/wheel_screen.dart`  🟡

**What works:** Wheel physics, animation, result dialog.

**Still local-only:**
| Data | Current source | Needed API |
|------|---------------|-----------|
| Spin result persistence | `AppSettings.addSpinToHistory()`, `AppSettings.updateSpinStatistics()` | `POST /spins/result` — persist outcome server-side |
| Spin count increment | `AppSettings.incrementTodaySpinCount()` (3 separate calls) | Consolidate into `POST /spins/result` response |
| Reward award | `coinBalanceProvider` updated locally | Server should award and return new balance in `POST /spins/result` response |

**Recommended refactor:**
```dart
// After wheel lands, instead of 3 separate AppSettings calls:
final response = await ref.read(storeServiceProvider).recordSpinResult(
  userId: userId,
  rewardType: result.reward.type,
  rewardAmount: result.reward.amount,
);
ref.read(coinBalanceProvider.notifier).set(response.newCoinBalance);
ref.invalidate(spinStatisticsProvider);
```

---

### `ui/widgets/spin_button.dart`  🟡

Uses `SpinTracker.canSpin()` and `SpinTracker.timeLeft()` (local cache). Should watch `spinStatisticsProvider` instead so it reacts when spin counts change elsewhere.

---

### `ui/widgets/spin_cooldown_widget.dart`  🟡

Uses `SpinTracker.timeLeft()`, `SpinTracker.getDailyCount()`, `SpinTracker.getMaxSpins()` via `Timer.periodic` polling. Should watch `spinStatisticsProvider` for reactive updates.

---

### `controllers/spin_history_notifier.dart`  🟡

Uses `AppSettings` for local history cache — appropriate for offline. Should also sync to server via `GET /spins/history/{userId}` on load and merge with local entries.

---

## New Providers Summary

All of these should be added to `lib/game/providers/` once the API endpoints exist:

| Provider | Returns | Depends on |
|----------|---------|-----------|
| `dailyRewardConfigProvider` | `DailyRewardConfig` | `GET /rewards/daily-config` |
| `weeklyStreakProvider` | `WeeklyStreakData` | `GET /rewards/weekly-streak/{userId}` |
| `weeklyRewardScheduleProvider` | `List<WeeklyRewardDay>` | `GET /rewards/weekly-schedule` |
| `rewardStepsProvider` | `List<RewardStep>` | `GET /rewards/spin-reward-steps` |
| `serverSpinStatsProvider` | `SpinStats` | `GET /spins/stats/{userId}` |

`spinStatisticsProvider` already exists in `lib/game/providers/spin_providers.dart` and wraps `EnhancedSpinTracker` locally — update its implementation to call `serverSpinStatsProvider` once the API is live.

---

## API Endpoints Needed (Priority Order)

| Priority | Method | Endpoint | Purpose |
|----------|--------|----------|---------|
| 🔴 HIGH | POST | `/rewards/daily/claim` | Server-persist daily claim; prevent multi-device double-claim |
| 🔴 HIGH | GET | `/rewards/weekly-streak/{userId}` | Sync 7-day login streak across devices |
| 🔴 HIGH | POST | `/rewards/weekly/claim` | Record weekly day claim server-side |
| 🔴 HIGH | POST | `/spins/result` | Record spin outcome + award coins in one call |
| 🟡 MED | GET | `/spins/stats/{userId}` | Authoritative spin counts + remaining |
| 🟡 MED | GET | `/rewards/daily-config` | Configurable daily reward definition |
| 🟡 MED | GET | `/rewards/weekly-schedule` | Configurable 7-day reward definitions |
| 🟡 MED | GET | `/spins/history/{userId}` | Cross-device spin history |
| 🟢 LOW | GET | `/rewards/spin-reward-steps` | Configurable reward progression steps |

---

## Files That Need No Backend Changes

| File | Reason |
|------|--------|
| `reward_stepper_slider_widget.dart` | Pure UI, data injected from parent |
| `spin_wheel/core/physics_engine.dart` | Pure physics math |
| `spin_wheel/core/wheel_painter.dart` | Pure rendering |
| `spin_wheel/services/segment_loader.dart` | Loads wheel segments from local JSON |
| `spin_wheel/ui/widgets/coin/coin_balance_display.dart` | Already watches `coinBalanceProvider` ✅ |
| `spin_wheel/ui/widgets/coin/coin_gain_animation.dart` | Pure animation |
| All `physics/`, `utils/`, `models/` files | No data fetching |
