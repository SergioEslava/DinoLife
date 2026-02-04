# System Architecture

## High-Level Overview

```
┌─────────────────────────────────────────────────────────┐
│                   DinoLife.Console                      │
│                    (Entry Point)                        │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
┌────────▼─────────┐    ┌───────▼────────┐
│  DinoLife.Core   │    │  DinoLife.     │
│  (Simulation)    │    │  Rendering     │
│                  │    │  (Terminal)    │
└────────┬─────────┘    └────────────────┘
         │
┌────────▼──────────┐
│  DinoLife.        │
│  Persistence      │
│  (Save/Load)      │
└───────────────────┘
```

## Project Structure

```
DinoLife/
├── src/
│   ├── DinoLife.Core/              # Simulation engine (no dependencies)
│   │   ├── Entities/               # Entity data structures
│   │   │   ├── Entity.cs
│   │   │   ├── EntityType.cs
│   │   │   └── ComponentFlags.cs
│   │   ├── Components/             # Component data (structs)
│   │   │   ├── Transform.cs
│   │   │   ├── Metabolism.cs
│   │   │   ├── Movement.cs
│   │   │   ├── Diet.cs
│   │   │   └── Reproduction.cs
│   │   ├── Systems/                # Logic processors (stateless)
│   │   │   ├── MovementSystem.cs
│   │   │   ├── MetabolismSystem.cs
│   │   │   ├── HuntingSystem.cs
│   │   │   ├── ReproductionSystem.cs
│   │   │   └── DeathSystem.cs
│   │   ├── World/                  # World state & management
│   │   │   ├── World.cs
│   │   │   ├── WorldConfig.cs
│   │   │   └── SpatialGrid.cs
│   │   ├── Simulation/             # Main loop
│   │   │   ├── SimulationEngine.cs
│   │   │   └── FixedTimeStep.cs
│   │   └── Utils/
│   │       ├── RandomProvider.cs
│   │       └── MathUtils.cs
│   │
│   ├── DinoLife.Rendering/         # Abstract + Terminal impl
│   │   ├── IRenderer.cs
│   │   ├── RenderData.cs
│   │   └── Terminal/
│   │       ├── TerminalRenderer.cs
│   │       ├── DoubleBuffer.cs
│   │       └── ColorScheme.cs
│   │
│   ├── DinoLife.Persistence/       # Save/Load system
│   │   ├── IWorldSerializer.cs
│   │   ├── JsonWorldSerializer.cs
│   │   └── SaveFile.cs
│   │
│   └── DinoLife.Console/           # Main application
│       ├── Program.cs
│       ├── InputHandler.cs
│       └── UI/
│           ├── HUD.cs
│           └── PerformanceOverlay.cs
│
├── tests/
│   ├── DinoLife.Core.Tests/
│   │   ├── Systems/
│   │   ├── World/
│   │   └── Integration/
│   ├── DinoLife.Rendering.Tests/
│   └── DinoLife.Benchmarks/
│
└── docs/                           # This documentation
```

## Core Architecture Patterns

### 1. Data-Oriented Design

**Component Storage:**
```csharp
// World.cs - Structure of Arrays (SoA)
public class World
{
    // Parallel arrays indexed by entity slot
    private Entity[] _entities;             // [5000]
    private Transform[] _transforms;        // [5000]
    private Metabolism[] _metabolisms;      // [5000]
    private Movement[] _movements;          // [5000]
    private Diet[] _diets;                  // [5000]
    
    private int _entityCount;
    private SpatialGrid _spatialGrid;
}
```

**Benefits:**
- Cache-friendly iteration (systems process arrays sequentially)
- SIMD-ready for future optimization
- Predictable memory layout

### 2. System Architecture

**System Contract:**
```csharp
public interface ISystem
{
    void Update(World world, float deltaTime);
}
```

**System Example:**
```csharp
public static class MovementSystem
{
    public static void Update(
        Span<Transform> transforms,
        Span<Movement> movements,
        float deltaTime)
    {
        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].Position += movements[i].Velocity * deltaTime;
        }
    }
}
```

**Systems are:**
- ✅ Stateless (pure functions)
- ✅ Operate on component arrays
- ✅ Independent (minimal coupling)
- ✅ Easily testable

### 3. Simulation Loop

```csharp
public class SimulationEngine
{
    private const double TARGET_TICK_TIME = 1.0 / 60.0;
    private World _world;
    private double _accumulator = 0.0;
    
    public void Run()
    {
        while (_running)
        {
            double frameTime = _timer.Elapsed;
            _accumulator += frameTime;
            
            // Fixed timestep updates
            while (_accumulator >= TARGET_TICK_TIME)
            {
                UpdateSystems(TARGET_TICK_TIME);
                _world.Tick++;
                _accumulator -= TARGET_TICK_TIME;
            }
            
            // Render at display rate (decoupled)
            _renderer.Render(_world);
        }
    }
    
    private void UpdateSystems(double deltaTime)
    {
        MovementSystem.Update(_world, deltaTime);
        MetabolismSystem.Update(_world, deltaTime);
        HuntingSystem.Update(_world, deltaTime);
        ReproductionSystem.Update(_world, deltaTime);
        DeathSystem.Update(_world, deltaTime);
    }
}
```

### 4. Entity Management

**Entity Structure:**
```csharp
public struct Entity
{
    public Guid Id;
    public EntityType Type;        // Herbivore, Carnivore, Plant, Scavenger
    public ComponentFlags Flags;    // Which components this entity has
    public bool IsAlive;
}

[Flags]
public enum ComponentFlags
{
    None = 0,
    Transform = 1 << 0,
    Metabolism = 1 << 1,
    Movement = 1 << 2,
    Diet = 1 << 3,
    Reproduction = 1 << 4
}
```

**Entity Factory:**
```csharp
public static class EntityFactory
{
    public static int CreateHerbivore(World world, Vector2 position)
    {
        int slot = world.AllocateEntitySlot();
        
        world.Entities[slot] = new Entity 
        { 
            Type = EntityType.Herbivore,
            Flags = ComponentFlags.Transform | 
                    ComponentFlags.Metabolism | 
                    ComponentFlags.Movement |
                    ComponentFlags.Diet |
                    ComponentFlags.Reproduction
        };
        
        world.Transforms[slot] = new Transform { Position = position };
        world.Metabolisms[slot] = new Metabolism { Energy = 100f, HungerRate = 1.0f };
        world.Movements[slot] = new Movement { Speed = 2.5f };
        world.Diets[slot] = new Diet { FoodType = FoodType.Plant };
        
        return slot;
    }
}
```

### 5. Spatial Partitioning

```csharp
public class SpatialGrid
{
    private const int CELL_SIZE = 10;
    private readonly List<int>[] _cells;  // Entity indices per cell
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    
    public IEnumerable<int> QueryRadius(Vector2 position, float radius)
    {
        int minX = (int)((position.X - radius) / CELL_SIZE);
        int maxX = (int)((position.X + radius) / CELL_SIZE);
        int minY = (int)((position.Y - radius) / CELL_SIZE);
        int maxY = (int)((position.Y + radius) / CELL_SIZE);
        
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            int cellIndex = y * _gridWidth + x;
            foreach (int entityIndex in _cells[cellIndex])
            {
                yield return entityIndex;
            }
        }
    }
}
```

### 6. Renderer Abstraction

```csharp
public interface IRenderer
{
    void Initialize();
    void Render(WorldSnapshot snapshot);
    void Shutdown();
}

public class TerminalRenderer : IRenderer
{
    private char[,] _backBuffer;
    private char[,] _frontBuffer;
    
    public void Render(WorldSnapshot snapshot)
    {
        Clear(_backBuffer);
        
        // Draw entities
        foreach (var entity in snapshot.Entities)
        {
            char symbol = GetSymbol(entity.Type);
            _backBuffer[entity.X, entity.Y] = symbol;
        }
        
        // Draw HUD
        DrawHUD(snapshot.Stats);
        
        // Swap and flush
        SwapBuffers();
        FlushToConsole();
    }
}
```

## Data Flow

```
Input → SimulationEngine → Systems → World State → Renderer → Display
  ↓                                        ↓
  └────────── InputHandler ────────────────┘
                                           ↓
                                    Persistence ──→ Disk
```

1. **Input Phase:** User commands processed (pause, save, speed change)
2. **Update Phase:** Systems modify world state in fixed timestep
3. **Render Phase:** Renderer reads world state (non-blocking)
4. **Persistence Phase:** Optional save on command or interval

## Key Interfaces

### World State Access
```csharp
public interface IWorldState
{
    int EntityCount { get; }
    long CurrentTick { get; }
    
    ReadOnlySpan<Entity> GetEntities();
    ReadOnlySpan<Transform> GetTransforms();
    
    IEnumerable<int> QueryEntitiesInRadius(Vector2 position, float radius);
}
```

### Serialization
```csharp
public interface IWorldSerializer
{
    void Save(World world, string filePath);
    World Load(string filePath);
}
```

## Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| Tick Time | <16ms | 60 TPS sustained |
| Entity Count | 5000 | No degradation |
| Memory | <500MB | Working set |
| Save Time | <1s | Full world state |
| Load Time | <1s | Full world state |

## Threading Model

**v1.0 - Single Threaded:**
- Main thread handles input, simulation, rendering
- Simpler debugging and determinism
- Sufficient for 5000 entities

**v1.1+ - Multi-threaded (future):**
- System parallelization (Jobs pattern)
- Async rendering thread
- Requires thread-safe spatial grid

## Future Architecture Considerations

1. **ECS Migration:** Current design supports gradual refactor to pure ECS
2. **Unity Integration:** `IRenderer` interface enables Unity renderer drop-in
3. **Networking:** World state serialization ready for netcode
4. **Scripting:** Consider Roslyn scripting for runtime behavior modification

---

*Last updated: 2026-02-04*
