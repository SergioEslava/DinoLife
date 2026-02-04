# Technical Decisions (ADRs)

> Architecture Decision Records for DinoLife v1.0

## ADR-001: Data-Oriented Hybrid Architecture

**Status:** ✅ Accepted  
**Date:** 2026-02-04  
**Context:** Need to choose between ECS pure, OOP, or Data-Oriented Design

### Decision
Use **Data-Oriented Hybrid** approach with struct-based data and separate systems.

### Rationale
- **Performance:** Cache-friendly data layout (SoA - Structure of Arrays)
- **Simplicity:** Faster development than pure ECS for v1.0 scope
- **Maintainability:** Clear separation of data and logic
- **Migration Path:** Can refactor to ECS (Unity DOTS) in v2.0 if needed

### Implementation
```csharp
// Data: Separate arrays per component type
Entity[] entities;
Transform[] transforms;
Metabolism[] metabolisms;

// Systems: Stateless processors
MovementSystem.Update(ref transforms, ref movements, deltaTime);
```

### Alternatives Considered
1. **Pure ECS** (DefaultEcs, Arch) - Rejected: Overhead for v1.0 scope
2. **OOP** (GameObject hierarchy) - Rejected: Poor cache performance at scale

---

## ADR-002: JSON for Persistence

**Status:** ✅ Accepted  
**Date:** 2026-02-04

### Decision
Use `System.Text.Json` for save/load serialization.

### Rationale
- **Debuggability:** Human-readable saved states
- **Simplicity:** Native .NET8 support, no dependencies
- **Flexibility:** Easy schema changes during development
- **Acceptable Performance:** <1s for 5000 entity saves

### Trade-offs
- **File Size:** Larger than binary (acceptable for v1.0)
- **Speed:** Slower than MessagePack (still within budget)

### Example Format
```json
{
  "version": "1.0.0",
  "tick": 15420,
  "entities": [
    { "id": "a1b2c3", "type": "Herbivore", "energy": 75.5 }
  ]
}
```

### Migration Path
Consider MessagePack in v1.1 if performance profiling shows bottleneck.

---

## ADR-003: Direct Console Rendering

**Status:** ✅ Accepted  
**Date:** 2026-02-04

### Decision
Use direct `Console` API with double-buffering, no external TUI framework.

### Rationale
- **Control:** Full control over rendering pipeline
- **Performance:** Minimal overhead, direct buffer writes
- **Learning:** Understand rendering fundamentals
- **Unity Prep:** Renderer abstraction makes Unity port trivial

### Implementation Pattern
```csharp
interface IRenderer {
    void Render(WorldState state);
}

class TerminalRenderer : IRenderer {
    char[,] backBuffer;
    char[,] frontBuffer;
    
    void Render(WorldState state) {
        // Write to backBuffer
        // Swap buffers
        // Write diff to Console
    }
}
```

### Alternatives Considered
1. **Spectre.Console** - Rejected: Less rendering control
2. **Terminal.Gui** - Rejected: Overkill for simple visualization

---

## ADR-004: 60 TPS Fixed Timestep

**Status:** ✅ Accepted  
**Date:** 2026-02-04

### Decision
Run simulation at fixed 60 ticks per second (16.67ms per tick).

### Rationale
- **Determinism:** Same inputs = same outputs
- **Predictability:** Consistent behavior across machines
- **Standard:** Matches common game loop patterns
- **Testing:** Easier to write deterministic tests

### Implementation
```csharp
const double TARGET_TICK_TIME = 1.0 / 60.0;
double accumulator = 0.0;

while (running) {
    double frameTime = timer.Elapsed;
    accumulator += frameTime;
    
    while (accumulator >= TARGET_TICK_TIME) {
        Simulate(TARGET_TICK_TIME);
        accumulator -= TARGET_TICK_TIME;
    }
    
    Render();
}
```

### Trade-offs
- **Flexibility:** No variable timestep (not needed for v1.0)
- **Slow Machines:** May drop ticks (acceptable, show warning)

---

## ADR-005: Simple Stat Variation (No Genetics)

**Status:** ✅ Accepted  
**Date:** 2026-02-04

### Decision
Entities have fixed base stats with ±10% random variation on reproduction.

### Rationale
- **Scope:** Genetic systems are v1.1+ feature
- **Emergence:** Sufficient for observable population dynamics
- **Simplicity:** Faster implementation and testing
- **Baseline:** Establishes behavior patterns before adding complexity

### Example
```csharp
struct HerbivoreStats {
    float Speed;        // Base: 2.5, Range: 2.25 - 2.75
    float Metabolism;   // Base: 1.0, Range: 0.9 - 1.1
}

HerbivoreStats Reproduce(HerbivoreStats parent) {
    return new HerbivoreStats {
        Speed = parent.Speed * Random.Range(0.9f, 1.1f),
        Metabolism = parent.Metabolism * Random.Range(0.9f, 1.1f)
    };
}
```

### Future Evolution
v1.1: Add genome system with gene-stat mapping and mutations.

---

## ADR-006: Entity Type Enum (Not Inheritance)

**Status:** ✅ Accepted  
**Date:** 2026-02-04

### Decision
Use `EntityType` enum with component flags, not class inheritance.

### Rationale
- **DOD Alignment:** Supports data-oriented array layouts
- **Performance:** No virtual dispatch, better cache locality
- **Flexibility:** Easy to add/remove components per entity
- **Serialization:** Simple enum serialization

### Implementation
```csharp
enum EntityType { Herbivore, Carnivore, Plant, Scavenger }

struct Entity {
    Guid Id;
    EntityType Type;
    ComponentFlags Components; // Bitflags
}
```

### Alternatives Considered
1. **Class Inheritance** - Rejected: OOP overhead, poor cache
2. **Interface Components** - Rejected: Virtual calls, complexity

---

## ADR-007: Spatial Partitioning - Uniform Grid

**Status:** ✅ Accepted  
**Date:** 2026-02-04

### Decision
Use uniform grid (not quadtree) for spatial queries.

### Rationale
- **Simplicity:** Easier to implement and debug
- **Predictable:** O(1) insertion, O(k) query where k = cells checked
- **Sufficient:** For uniform entity distribution, grid is optimal
- **Memory:** Fixed allocation, no tree node overhead

### Configuration
```csharp
const int GRID_CELL_SIZE = 10; // Units
const int WORLD_SIZE = 1000;   // 100x100 grid
```

### Trade-offs
- **Non-uniform Distribution:** Quadtree would be better (defer to v1.1)
- **Dynamic Sizing:** Grid is fixed (acceptable for v1.0)

---

## ADR-008: No External Dependencies (Core)

**Status:** ✅ Accepted  
**Date:** 2026-02-04

### Decision
Core simulation has zero NuGet dependencies (except testing libs).

### Rationale
- **Control:** No surprises, full code ownership
- **Learning:** Implement fundamentals from scratch
- **Portability:** Easy Unity integration later
- **Stability:** No breaking changes from external libs

### Exceptions
- ✅ `xUnit` - Testing only
- ✅ `FluentAssertions` - Testing only
- ✅ `BenchmarkDotNet` - Performance testing only

### Future Considerations
May add MessagePack (v1.1) or Serilog (v1.2) if justified.

---

## Summary Table

| ADR | Decision | Status |
|-----|----------|--------|
| 001 | Data-Oriented Hybrid | ✅ Accepted |
| 002 | JSON Persistence | ✅ Accepted |
| 003 | Direct Console Rendering | ✅ Accepted |
| 004 | 60 TPS Fixed Timestep | ✅ Accepted |
| 005 | Simple Stat Variation | ✅ Accepted |
| 006 | Entity Type Enum | ✅ Accepted |
| 007 | Uniform Grid Partitioning | ✅ Accepted |
| 008 | No Core Dependencies | ✅ Accepted |

---

*Last updated: 2026-02-04*
