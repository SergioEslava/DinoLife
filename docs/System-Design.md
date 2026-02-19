# System Design

## Overview

DinoLife uses stateless systems that operate over data stored in `Planet` parallel arrays.
Each simulation tick runs systems in a deterministic fixed order.

## Tick Pipeline

Execution order is defined in `FactorySimulation.GenerateDefaultSimulation`:

1. `SpatialGridSystem`
2. `BehaviorSystem`
3. `MovementSystem`
4. `MetabolismSystem`
5. `PlantGrowthSystem`
6. `HuntingSystem`
7. `ReproductionSystem`
8. `DeathSystem`

This order intentionally updates spatial queries first, then intent/velocity, then movement,
resource exchange, reproduction, and final cleanup.

## Core Data Model

`Planet` stores components in arrays indexed by entity slot:

- `Entities`
- `Transforms`
- `Movements`
- `Metabolisms`
- `Diets`
- `Plants`
- `Reproductions`
- `Lifespans`
- `Corpses` (list)

Common access pattern:

```csharp
Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
for (int i = 0; i < entities.Length; i++)
{
    if (!entities[i].IsAlive) { continue; }
    // component checks + logic
}
```

## System Responsibilities

### SpatialGridSystem

- Clears and rebuilds spatial index each tick from active transforms.
- Enables neighborhood queries for behavior/hunting/metabolism.

### BehaviorSystem

- Computes velocity direction intent by entity type:
  - herbivores: seek plants, flee carnivores
  - carnivores: seek herbivores/scavengers under hunger pressure
  - scavengers: seek corpses, avoid carnivores
- Falls back to deterministic random walk when needed.

### MovementSystem

- Applies velocity integration to transform positions.
- Updates facing rotation from velocity.
- Wraps positions around world bounds.

### MetabolismSystem

- Depletes energy by hunger rate.
- Handles direct eating interactions:
  - plants become inactive and respawn later
  - corpse consumption removes corpse entries
  - consumed prey can be marked dead

### PlantGrowthSystem

- Grows active plants toward `MaxEnergy`.
- Advances respawn timer for inactive plants.
- Reactivates plants after respawn delay.

### HuntingSystem

- Resolves predator kills in eat radius (carnivore diet flow).
- Converts prey death into corpses with retained energy.
- Grants predator energy gain.

### ReproductionSystem

- Advances reproduction cooldown.
- Spawns offspring when thresholds and component requirements are met.
- Applies lightweight stat variation to offspring.

### DeathSystem

- Kills entities due to starvation or lifespan expiry.
- Spawns corpses for non-plant deaths.
- Decays and removes expired corpses.

## Simulation Timing

- Fixed update rate: `60 TPS` (`SimulationEngine.TickRate`).
- `SimulationEngine.TickTime = 1.0 / TickRate`.
- Console host can run multiple ticks per frame based on accumulated real time and speed multiplier.

## Integration With Rendering

Renderers do not read `Planet` directly. They consume `WorldSnapshot` built by `WorldSnapshotBuilder`.
This keeps simulation mutation separate from display concerns.

## Integration With Tuning and Config

- Runtime tuning edits values in `SimulationTuningProfile`.
- `WorldTuningApplier` pushes those values into live component arrays.
- `world-config.json` can hot-reload and reapply profile/world size.

---

Last updated: 2026-02-19
