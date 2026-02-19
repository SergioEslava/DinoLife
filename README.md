# DinoLife v1.0

Emergent life simulation system in C# (.NET 8) with terminal rendering.

## Overview

DinoLife simulates ecosystems with 4 entity types and real-time interaction rules. It supports:

- Legacy terminal renderer and TUI renderer (`--renderer=legacy|tui`)
- Save/load state
- Performance overlay
- In-game help overlay (`H`)
- In-game parameter tuning with live updates, presets, and import/export
- JSON configuration files with schema validation and hot-reload

## Quick Navigation

### Planning & Architecture
- [Project Overview](docs/Project-Overview.md)
- [Technical Decisions (ADRs)](docs/Technical-Decisions.md)
- [System Architecture](docs/Architecture.md)
- [Project Milestones](docs/Milestones.md)

### Development
- [Project Structure](docs/Project-Structure.md)
- [Entity Design](docs/Entity-Design.md)
- [System Design](docs/System-Design.md)

### Workflow
- [GitFlow Workflow](docs/GitFlow-Workflow.md)
- [Development Guidelines](docs/Development-Guidelines.md)

## Core Specifications

| Aspect | Specification |
|--------|--------------|
| Language | C# .NET8 |
| Architecture | Data-Oriented Hybrid |
| Tick Rate | 60 updates/second |
| Target Entities | 5000 simultaneous |
| Persistence | JSON serialization |
| Rendering | Terminal (legacy + TUI) |

## Entity Types

1. Herbivore: Eats plants, flees carnivores
2. Carnivore: Hunts herbivores (and scavengers when very hungry)
3. Plant: Regrows and respawns after being consumed
4. Scavenger: Consumes corpses and avoids carnivores

## Build

```bash
dotnet restore
dotnet build -c Release
```

## Run

### Legacy renderer

```bash
dotnet run --project src/DinoLife.Console -- --renderer=legacy
```

### TUI renderer

```bash
dotnet run --project src/DinoLife.Console -- --renderer=tui
```

## Main Controls

- `Space`: Play/Pause
- `RightArrow`: Step one tick (paused)
- `+/-`: Simulation speed
- `W/A/S/D` or arrows: Camera pan
- `Home`: Reset camera
- `Tab` / `[` / `]`: Entity selection
- `F`: Follow selected entity
- `G`: Toggle grid
- `P` or `O`: Performance panel
- `H`: Help screen
- `M`: Command menu (TUI)
- `S`: Save
- `L`: Load selected save
- `J/K`: Previous/next save in browser
- `R`: Reset simulation
- `Q` / `Esc`: Quit

## Parameter Tuning (TUI)

In `M` menu, open `Parameter tuning...` to:

- Edit movement/metabolism/reproduction/detection/growth values live
- Apply presets: `Balanced`, `Chaotic`, `Stable`
- Export tuning profiles to `tuning/*.json`
- Import latest tuning profile from `tuning/`

## Configuration Files

- `appsettings.json`: global app config
- `world-config.json`: world seed, initial populations, tuning profile
- `appsettings.schema.json` and `world-config.schema.json`: validation schemas

Hot-reload is enabled by default (`hotReloadEnabled: true` in `appsettings.json`).

## Tests

```bash
dotnet test -c Release
```

## Benchmarks

```bash
dotnet run --project tests/DinoLife.Benchmarks/DinoLife.Benchmarks.csproj -c Release
```
