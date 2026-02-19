# Project Overview

## Vision

DinoLife is a terminal-based ecosystem simulation where simple local rules produce emergent global behavior.

## Current Scope (v1.0)

### Core Goals

1. Stable fixed-step simulation (60 TPS).
2. Four interacting entity types (Plant, Herbivore, Carnivore, Scavenger).
3. Real-time terminal visualization.
4. Save/load world state.
5. Runtime parameter tuning without restart.
6. In-game help and diagnostics overlays.
7. Config-driven startup with schema validation and hot-reload.

### Implemented Highlights

- Legacy and TUI renderer modes.
- Camera pan/zoom/reset/follow behavior.
- Performance overlay and stats HUD.
- Command menu and help overlay in TUI.
- Parameter tuning categories:
  - movement
  - metabolism
  - reproduction
  - detection
  - growth
- Presets: Balanced, Chaotic, Stable.
- Tuning profile import/export.
- `appsettings.json` and `world-config.json` with schema validation.
- Runtime config hot-reload for app/world settings.

## Non-Goals (v1.0)

- 3D graphics
- networking/multiplayer
- advanced genetics/evolution model
- audio systems

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Runtime instability from bad config edits | High | Schema validation + reject invalid hot-reloads |
| UI complexity growth in terminal | Medium | Overlay architecture (`IOverlay`, `OverlayHost`) |
| Performance regression with larger populations | High | Snapshot rendering, spatial grid, benchmarks |
| Parameter tuning breaking balance | Medium | Presets + import/export profiles |

## Roadmap Reference

See `docs/Milestones.md` for detailed progress and remaining scope.

---

Last updated: 2026-02-19
