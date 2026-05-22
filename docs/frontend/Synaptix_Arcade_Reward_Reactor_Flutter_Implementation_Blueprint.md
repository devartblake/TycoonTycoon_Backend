# Synaptix Arcade Reward Reactor — Flutter Implementation Blueprint

**Version:** Alpha/Beta Production Planning Document  
**Target platform:** Flutter (iOS / Android / Web)  
**Backend compatibility:** .NET backend + FastAPI services  
**Primary goal:** Build a high-performance arcade reward machine inspired by hyper-casual casino UX while remaining skill/reward focused for the Synaptix ecosystem.

> The Reward Reactor is not a slot machine or gambling system. It is a backend-authoritative animated reward experience framework for progression, missions, XP, inventory, events, and achievement feedback.

---

## 1. Executive Summary

The reference design represents a modern social-casino / hyper-casual reward-machine interface with:

- Animated reel systems
- Neon arcade aesthetics
- Reward amplification feedback loops
- Dynamic particle effects
- Layered UI depth
- Real-time reward animation systems

For Synaptix, this concept should become a **Reward Reactor** instead of a gambling surface.

The Reward Reactor is a reusable progression mechanic across:

- XP rewards
- Rank promotions
- Loot drops
- Mission completion
- Daily rewards
- Event bonuses
- Arcade challenges
- Tournament rewards
- Skill tree bonuses

The experience should feel rewarding, exciting, responsive, premium, performant, and replayable without introducing real-money gambling mechanics.

---

## 2. Core Design Philosophy

### Primary Objectives

#### Visual Excitement

Deliver strong reward feedback through:

- Glow
- Particles
- Scaling
- Reward popups
- Dynamic typography
- Animated reels

#### Performance First

The system must maintain:

- 60 FPS minimum
- Low rebuild counts
- Isolated animations
- GPU-friendly rendering

#### Reusable Architecture

The Reward Reactor should operate as a modular component usable anywhere in the app.

#### Backend Authority

The frontend must never determine rewards.

The backend controls:

- Probabilities
- Outcomes
- Anti-cheat
- Cooldowns
- Inventory changes
- Reward validation

---

## 3. High-Level System Architecture

```text
Frontend Flutter Client
│
├── RewardReactorScreen
├── Animation Layer
│   ├── Reel Animations
│   ├── Particles
│   ├── Glow Effects
│   ├── Win Effects
│   └── Sound/Haptics
│
├── State Layer (Riverpod)
│   ├── Reward State
│   ├── Spin State
│   ├── Inventory State
│   └── Cooldown State
│
├── Rendering Layer
│   ├── CustomPainter
│   ├── Sprite Rendering
│   └── Animated Widgets
│
└── Backend Services
    ├── Reward APIs
    ├── XP APIs
    ├── Mission APIs
    ├── Inventory APIs
    └── Anti-Cheat Services
```

---

## 4. Recommended Flutter Technology Stack

| Category | Recommendation |
|---|---|
| State management | Riverpod |
| Navigation | GoRouter |
| Animation | `AnimationController` + `flutter_animate` |
| Rendering | `CustomPainter` |
| Particles | Flame particles or custom painter |
| Realtime | SignalR/WebSockets |
| Audio | `just_audio` |
| Asset handling | Sprite sheets + WebP |
| Dependency injection | Riverpod providers |
| Backend communication | Dio |
| Performance profiling | Flutter DevTools |

---

## 5. Recommended Folder Structure

```text
/lib
 ├── features
 │    └── reward_reactor
 │         ├── controllers
 │         ├── models
 │         ├── painters
 │         ├── providers
 │         ├── services
 │         ├── widgets
 │         ├── animations
 │         ├── effects
 │         ├── particles
 │         └── screens
 │
 ├── core
 │    ├── theme
 │    ├── audio
 │    ├── animations
 │    ├── networking
 │    ├── shaders
 │    └── utilities
 │
 └── shared
      ├── widgets
      ├── effects
      └── painters
```

---

## 6. Core UI Component Architecture

### Primary Widget

`ArcadeRewardMachineWidget`

This becomes the reusable machine framework.

### Internal Widget Composition

```text
ArcadeRewardMachineWidget
│
├── AnimatedBackgroundLayer
├── CoinParticleLayer
├── RewardHeaderPanel
├── ReactorMachineFrame
│
├── ReelContainer
│    ├── ReelColumn
│    ├── SymbolTile
│    ├── SymbolGlow
│    └── WinOverlay
│
├── RewardBanner
├── BottomCurrencyBar
└── ActionButtons
```

---

## 7. UI Style Guidelines

### Visual Direction

The design language should combine:

- Arcade
- Neon
- Sci-fi
- Casino polish
- Glassmorphism
- 2.5D depth

### Color Palette

| Type | Color |
|---|---|
| Neon Purple | `#8B5CF6` |
| Gold | `#FBBF24` |
| Cyan | `#22D3EE` |
| Pink Accent | `#EC4899` |
| Reactor Orange | `#FB923C` |
| Dark Background | `#0F172A` |

### Lighting Style

Use:

- Bloom
- Glow
- Emissive gradients
- Layered shadows
- Soft blur

Avoid:

- Flat UI
- Material-only design
- Plain cards

---

## 8. Reel Machine Design

### Important

Do **not** use `GridView`.

The reference UI uses:

- Asymmetrical reel layouts
- Stepped positioning
- Layered animation regions

Use:

- `Stack` + `Positioned`
- `CustomMultiChildLayout`

### Recommended Reel Structure

```text
ReelContainer
 ├── ReelColumn
 │    ├── SymbolTile
 │    ├── SymbolTile
 │    └── SymbolTile
```

### Reel Animation Strategy

Each reel should:

- Spin independently
- Stagger stop timing
- Overshoot slightly
- Bounce on completion

### Animation Timing

| Phase | Duration |
|---|---:|
| Initial Spin | 1200ms |
| Acceleration | 300ms |
| Deceleration | 500ms |
| Final Bounce | 150ms |

---

## 9. Symbol System

### Synaptix Symbol Mapping

| Casino Symbol | Synaptix Equivalent |
|---|---|
| Wild | XP Multiplier |
| Coins | SynCoins |
| Diamonds | Knowledge Gems |
| Bell | Reward Trigger |
| Jackpot | Mega Rank Bonus |
| Clover | Luck Booster |
| Free Spins | Bonus Round |
| Piggy Bank | XP Vault |

### Symbol Requirements

Each symbol supports:

- Glow state
- Pulse animation
- Rarity tier
- Animated entrance
- Unlock effects

---

## 10. Particle System

Do **not** use GIFs.

Use:

- `CustomPainter` particles
- Flame particles
- Sprite sheets

### Particle Types

| Particle | Usage |
|---|---|
| Coins | Win effects |
| Sparks | Reel impact |
| Energy Rings | Jackpot |
| Floating XP | Reward gain |
| Confetti | Milestones |

### Particle Layer Structure

```text
ParticleLayer
 ├── CoinEmitter
 ├── SparkEmitter
 ├── XPEmitter
 └── GlowField
```

---

## 11. Typography System

Typography should be:

- Bold
- Arcade-inspired
- Beveled
- Glowing
- Oversized

### Recommended Font Types

| Usage | Style |
|---|---|
| Reward Headers | Bold Display |
| Counters | Digital/LED |
| Buttons | Rounded Arcade |
| Stats | Condensed Sans |

### Reward Typography Examples

- MEGA BONUS
- XP MULTIPLIER
- TRIVIA COMBO
- RANK BOOST
- MISSION COMPLETE

---

## 12. Animation System

### Core Animation Types

| Animation | Usage |
|---|---|
| Pulse | Active symbols |
| Shake | Near jackpot |
| Scale Burst | Win events |
| Glow Sweep | Reward banner |
| Floating | Idle motion |
| Bounce | Reel stop |

### Animation Architecture

```text
animations/
 ├── reel_spin_animation.dart
 ├── symbol_pulse_animation.dart
 ├── jackpot_animation.dart
 ├── glow_sweep_animation.dart
 └── reward_burst_animation.dart
```

---

## 13. Audio System

### Audio Categories

| Type | Example |
|---|---|
| Reel Spin | Mechanical arcade spin |
| Reel Stop | Impact click |
| Reward Win | Synth reward |
| Jackpot | Large orchestral hit |
| UI Hover | Soft arcade tick |

### Audio Guidelines

Do:

- Preload sounds
- Pool audio players
- Sync sounds to animations

Do not:

- Instantiate players repeatedly
- Stream small UI sounds

---

## 14. Haptic Feedback

Use haptics for:

- Reel stop
- Major rewards
- Button taps
- Jackpot impact

---

## 15. Backend Architecture

### Backend Responsibility

The backend determines:

- Reward results
- RNG
- Cooldowns
- Anti-cheat validation
- Inventory changes

The frontend only renders outcomes.

### Recommended API Flow

```text
Client Requests Spin
        │
        ▼
Backend Generates Reward
        │
        ▼
Backend Returns Spin Result
        │
        ▼
Frontend Animates Result
        │
        ▼
Frontend Applies Reward State
```

### Proposed API Endpoints

These are proposed future contracts, not existing routes.

| Purpose | Endpoint |
|---|---|
| Spin Request | `POST /arcade/reactor/spin` |
| Reward Claim | `POST /arcade/reactor/claim` |
| Reward Inventory | `GET /users/me/rewards` |

---

## 16. Anti-Cheat Recommendations

The backend should validate:

- Impossible spin frequency
- Modified client values
- Packet replay
- Invalid reward claims
- Speed hacks

### Recommended Protection

| Protection | Purpose |
|---|---|
| JWT Validation | Session integrity |
| Spin Cooldowns | Prevent abuse |
| Idempotency Tokens | Duplicate prevention |
| Signed Reward Responses | Tamper prevention |
| Server-side RNG | Fairness |

---

## 17. Performance Requirements

### Mandatory Targets

| Target | Goal |
|---|---|
| FPS | 60 FPS |
| Frame Build | < 8ms |
| Frame Render | < 8ms |
| Memory Stability | No leaks |
| Asset Size | Optimized |

### Required Optimizations

Do:

- Isolate repaint boundaries
- Use sprite sheets
- Use WebP assets
- Cache images
- Use `const` widgets
- Reduce rebuilds

Avoid:

- Animating `Opacity` heavily
- Massive GIFs
- Rebuilding entire screens
- Stacking many `BackdropFilter`s
- Oversized PNGs

---

## 18. Mobile Responsiveness

| Device | Support |
|---|---|
| Phones | Primary |
| Tablets | Enhanced |
| Foldables | Optional |
| Web | Adaptive |

### Layout Strategy

Use:

- `LayoutBuilder`
- `MediaQuery`
- Adaptive sizing

---

## 19. Suggested Synaptix Integrations

| System | Reward Reactor Usage |
|---|---|
| Missions | Mission rewards |
| Skill Tree | Skill shard drops |
| Arcade | Reward events |
| Leaderboards | Rank bonuses |
| XP System | XP multipliers |
| Seasonal Events | Event loot |
| Daily Login | Daily spins |

---

## 20. Production Rollout Plan

### Phase 1 — Alpha

Focus:

- Core reel system
- Backend spin API
- Reward animations
- Basic particles

### Phase 2 — Beta

Add:

- Advanced particles
- Shader effects
- Reward chains
- Mission integration
- Haptics/audio polish

### Phase 3 — Production

Add:

- Dynamic themes
- Live events
- Realtime tournaments
- Progression campaigns
- Seasonal reward reactors

---

## 21. Recommended Claude/Codex Development Order

### Priority Order

1. Build `ArcadeRewardMachineWidget`.
2. Implement `ReelColumn`, `SymbolTile`, and `SpinController`.
3. Implement particle layer, glow system, and reward banner.
4. Wire Riverpod state, backend APIs, and cooldown system.
5. Add audio, haptics, and shader effects.

---

## 22. Recommended Deliverables for Claude/Codex

### Flutter

- Production folder structure
- Reusable widgets
- Riverpod providers
- Animation controllers
- `CustomPainter` systems
- Responsive layouts

### Backend

- Reward API contracts
- DTOs
- Anti-cheat validation
- RNG services
- Cooldown systems

### Art Pipeline

- Sprite atlas support
- WebP conversion pipeline
- Glow overlays
- Symbol rarity effects

---

## 23. Related Existing Docs

- [`docs/store/spin_and_earn_backend_handoff_net10.md`](../store/spin_and_earn_backend_handoff_net10.md) — existing arcade spin backend handoff and reward claim concepts.
- [`docs/releases/ALPHA_ENABLED_FEATURES.md`](../releases/ALPHA_ENABLED_FEATURES.md) — current Alpha arcade spin endpoints and enabled feature list.
- [`docs/frontend/synaptix_frontend_plan.md`](synaptix_frontend_plan.md) — frontend rebrand and Labs/Arcade positioning guidance.

---

## 24. Final Recommendation

This system should not be treated as a slot machine.

It should become a **Synaptix Reward Experience Framework**: a reusable animated progression system that powers reward feedback, progression excitement, event engagement, mission systems, rank promotions, and XP milestones.

The strongest implementation strategy is:

```text
Backend-authoritative rewards
+
Flutter-native rendering
+
CustomPainter effects
+
Riverpod architecture
+
Modular reusable components
```

This architecture aligns with the existing Synaptix ecosystem, arcade mission systems, progression mechanics, tier/rank systems, and skill tree infrastructure, and can scale from Alpha/Beta into a full production reward ecosystem.
