# Project Structure

## Directory Layout

```text
DinoLife/
  src/
    DinoLife.Core/
      Components/
      Entities/
      Simulation/
      Systems/
      Utils/
      World/
    DinoLife.Rendering/
      Terminal/
        UI/
      IRenderer.cs
      IInteractiveRenderer.cs
      WorldSnapshot.cs
      WorldSnapshotBuilder.cs
    DinoLife.Persistence/
      IWorldSerializer.cs
      JsonWorldSerializer.cs
      SaveBrowser.cs
      WorldStatePersistence.cs
    DinoLife.Console/
      Configuration/
      Tuning/
      InputHandler.cs
      InputRouter.cs
      Program.cs
      RendererMode.cs
  tests/
    DinoLife.Core.Tests/
    DinoLife.Rendering.Tests/
    DinoLife.Persistence.Tests/
    DinoLife.Benchmarks/
  docs/
  appsettings.json
  appsettings.schema.json
  world-config.json
  world-config.schema.json
  DinoLife.sln
```

## Key Files By Responsibility

### Simulation Core

- `src/DinoLife.Core/World/Planet.cs`: central world state storage.
- `src/DinoLife.Core/Simulation/SimulationEngine.cs`: fixed timestep execution.
- `src/DinoLife.Core/Systems/*.cs`: simulation behavior systems.

### Rendering

- `src/DinoLife.Rendering/IRenderer.cs`: base rendering contract.
- `src/DinoLife.Rendering/IInteractiveRenderer.cs`: interactive renderer contract.
- `src/DinoLife.Rendering/Terminal/TerminalRenderer.cs`: legacy terminal renderer with double-buffer diff.
- `src/DinoLife.Rendering/Terminal/TerminalGuiRenderer.cs`: TUI renderer with overlays.
- `src/DinoLife.Rendering/Terminal/UI/*.cs`: overlay primitives and components.

### Console Host and UX

- `src/DinoLife.Console/Program.cs`: app orchestration and loop.
- `src/DinoLife.Console/InputHandler.cs`: key mapping.
- `src/DinoLife.Console/InputRouter.cs`: focus-aware routing.
- `src/DinoLife.Console/RendererMode.cs`: renderer CLI parsing.

### Runtime Tuning

- `src/DinoLife.Console/Tuning/SimulationTuningProfile.cs`: editable parameters.
- `src/DinoLife.Console/Tuning/TuningPresets.cs`: Balanced/Chaotic/Stable.
- `src/DinoLife.Console/Tuning/WorldTuningApplier.cs`: live application to world.
- `src/DinoLife.Console/Tuning/TuningFileStore.cs`: import/export JSON profiles.

### Configuration

- `src/DinoLife.Console/Configuration/ConfigManager.cs`: loading + validation + hot-reload.
- `src/DinoLife.Console/Configuration/SchemaSubsetValidator.cs`: schema validation engine.
- `src/DinoLife.Console/Configuration/AppSettingsConfig.cs`: global config model.
- `src/DinoLife.Console/Configuration/WorldConfig.cs`: world config model.

## Build and Run

### Build all

```bash
dotnet build
```

### Run console app

```bash
dotnet run --project src/DinoLife.Console
```

### Run TUI explicitly

```bash
dotnet run --project src/DinoLife.Console -- --renderer=tui
```

### Run tests

```bash
dotnet test
```

## Dependency Graph

```text
DinoLife.Console
  -> DinoLife.Core
  -> DinoLife.Rendering
  -> DinoLife.Persistence

DinoLife.Rendering -> DinoLife.Core
DinoLife.Persistence -> DinoLife.Core
```

---

Last updated: 2026-02-19
