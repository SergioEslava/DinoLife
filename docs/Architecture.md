# System Architecture

## High-Level Overview

DinoLife is organized in 4 runtime layers:

1. `DinoLife.Console`: app host, input loop, UI flow, configuration hot-reload.
2. `DinoLife.Core`: simulation data model and systems.
3. `DinoLife.Rendering`: rendering abstractions and terminal implementations.
4. `DinoLife.Persistence`: save/load and save browser.

Main loop flow:

`Input -> Command Handling -> Simulation Tick(s) -> WorldSnapshot -> Renderer -> Console`

## Runtime Modules

### DinoLife.Core

- Stores world state in parallel arrays (`Planet`).
- Runs systems (`BehaviorSystem`, `MovementSystem`, `MetabolismSystem`, `HuntingSystem`, `ReproductionSystem`, `PlantGrowthSystem`, `DeathSystem`, `SpatialGridSystem`).
- Uses fixed tick rate (`SimulationEngine.TickRate = 60`).

### DinoLife.Rendering

- `IRenderer` provides `Initialize`, `Render`, `Shutdown`.
- `IInteractiveRenderer` extends renderer with camera and overlay controls.
- `WorldSnapshotBuilder` converts mutable world state to immutable render snapshot.
- `TerminalRenderer` handles legacy rendering using a double-buffer diff.
- `TerminalGuiRenderer` composes overlays (`OverlayHost`) on top of terminal world rendering.

### DinoLife.Console

- Owns main application loop and command processing.
- `InputHandler` maps key events to semantic commands.
- `InputRouter` enforces focus-aware behavior (world vs menu).
- Supports renderer selection via CLI (`--renderer=legacy|tui`) and config default.
- Implements parameter tuning UI, help UI, save browser flow, and camera controls.

### DinoLife.Persistence

- `JsonWorldSerializer` for world save/load.
- `SaveBrowser` for listing and selecting saved files.
- Save operations are triggered from input/menu commands and autosave cadence.

## UI Architecture (TUI Mode)

TUI mode is overlay-driven:

- `TerminalGuiRenderer` delegates world drawing to `TerminalRenderer`.
- `UiCanvas` offers drawing primitives.
- `IOverlay` defines pluggable overlay components.
- `OverlayHost` renders active overlays in order.
- Implemented overlays:
  - `CommandMenuOverlay`
  - `HelpOverlay`

This allows incremental UI feature growth without rewriting the world renderer.

## Configuration Architecture

Configuration is file-based and runtime-reloadable:

- `appsettings.json` (global app behavior)
- `world-config.json` (world seed, initial counts, tuning profile)
- `appsettings.schema.json` and `world-config.schema.json` (validation)

`ConfigManager` responsibilities:

- Ensure config files exist (write defaults if missing).
- Validate JSON against schema subset (`SchemaSubsetValidator`).
- Provide hot-reload polling and only apply valid updates.

## Data Boundaries

### Simulation -> Rendering

- Renderer never mutates `Planet`.
- Rendering only consumes `WorldSnapshot`.
- Snapshot includes entities, corpses, world size, grid settings, and aggregate stats.

### UI -> Simulation

- UI commands mutate simulation state through explicit handlers in `Program`.
- Parameter tuning applies to live entities through `WorldTuningApplier`.

### Persistence -> Simulation

- Load replaces current world instance and recreates simulation engine.
- Save serializes current world state to JSON.

## Threading Model

Current model is single-threaded:

- Input polling, simulation updates, rendering, and hot-reload checks run on main thread.
- This preserves deterministic update order and simplifies debugging.

## Extension Points

- Add new renderer implementations via `IRenderer` / `IInteractiveRenderer`.
- Add new overlays by implementing `IOverlay`.
- Add new runtime parameters by extending `SimulationTuningProfile` and `WorldTuningApplier`.
- Add new config keys by extending config models and schemas.

---

Last updated: 2026-02-19
