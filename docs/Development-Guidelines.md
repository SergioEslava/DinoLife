# Development Guidelines

## Code Standards

### General Principles

1. **Clarity over cleverness** - Readable code beats smart code
2. **Performance where it matters** - Profile first, optimize hot paths
3. **Test everything** - Untested code is broken code
4. **Document the why** - Code shows how, comments explain why
5. **Zero warnings** - Warnings are future bugs

---

## C# Coding Style

### Naming Conventions

```csharp
// Namespaces: PascalCase
namespace DinoLife.Core.Entities;

// Classes/Structs/Interfaces: PascalCase
public class World { }
public struct Entity { }
public interface IRenderer { }

// Methods: PascalCase
public void UpdateSystems() { }

// Properties: PascalCase
public int EntityCount { get; set; }

// Private fields: _camelCase with underscore
private int _entityCount;
private readonly Random _random;

// Public fields: PascalCase (avoid, use properties)
public const int MaxEntities = 5000;

// Parameters/Local variables: camelCase
public void CreateEntity(EntityType entityType)
{
    int newSlot = AllocateSlot();
}

// Enums: PascalCase (type and values)
public enum EntityType
{
    Herbivore,
    Carnivore,
    Plant,
    Scavenger
}
```

### File Organization

```csharp
// 1. Using statements (sorted)
using System;
using System.Collections.Generic;
using DinoLife.Core.Components;

// 2. Namespace (file-scoped)
namespace DinoLife.Core.Systems;

// 3. Class/Struct definition
/// <summary>
/// Handles entity movement and boundary enforcement.
/// </summary>
public static class MovementSystem
{
    // 4. Constants
    private const float MinSpeed = 0.1f;
    
    // 5. Public methods
    public static void Update(World world, float deltaTime)
    {
        // ...
    }
    
    // 6. Private methods
    private static Vector2 WrapPosition(Vector2 pos, Vector2 worldSize)
    {
        // ...
    }
}
```

### Struct vs Class

```csharp
// Use STRUCT for:
// - Small, immutable data (<16 bytes ideal)
// - Components (Transform, Metabolism, etc.)
// - Value semantics needed
public struct Transform
{
    public Vector2 Position;
    public float Rotation;
}

// Use CLASS for:
// - Large objects
// - Reference semantics needed
// - Inheritance required
public class World
{
    private Entity[] _entities;
    // ...
}
```

### Modern C# Features

```csharp
// File-scoped namespaces (C# 10+)
namespace DinoLife.Core.Entities;

// Record structs for immutable data
public readonly record struct Vector2(float X, float Y)
{
    public float Length() => MathF.Sqrt(X * X + Y * Y);
}

// Pattern matching
public bool IsFood(EntityType type) => type switch
{
    EntityType.Plant => true,
    EntityType.Herbivore => true,
    _ => false
};

// Span<T> for performance-critical arrays
public void ProcessEntities(Span<Entity> entities)
{
    for (int i = 0; i < entities.Length; i++)
    {
        // Direct memory access, no bounds checks
    }
}

// Null-safety with nullable reference types
public Entity? FindEntity(Guid id)
{
    // Returns null if not found
}

// Range/Index operators
var lastTen = entities[^10..];
```

---

## Performance Guidelines

### Hot Path Optimization

```csharp
// ✅ DO: Use Span<T> in systems
public static void Update(Span<Transform> transforms, float deltaTime)
{
    for (int i = 0; i < transforms.Length; i++)
    {
        transforms[i].Position += transforms[i].Velocity * deltaTime;
    }
}

// ❌ DON'T: Use foreach with ref structs (copies)
foreach (var transform in transforms) // Copies each transform!
{
    transform.Position += transform.Velocity * deltaTime;
}
```

### Allocation Reduction

```csharp
// ✅ DO: Pre-allocate collections
private readonly List<int> _detectedFood = new(capacity: 100);

public void Update()
{
    _detectedFood.Clear(); // Reuse, don't reallocate
}

// ❌ DON'T: Allocate in hot loops
for (int i = 0; i < 5000; i++)
{
    var list = new List<int>(); // 5000 allocations!
}
```

### Struct Layout

```csharp
// ✅ DO: Order fields by size (reduce padding)
public struct Transform
{
    public Vector2 Position;  // 8 bytes
    public float Rotation;    // 4 bytes
    // Total: 12 bytes (+ 4 padding = 16 bytes aligned)
}

// ❌ DON'T: Interleave small and large fields
public struct BadTransform
{
    public float Rotation;    // 4 bytes
    public Vector2 Position;  // 8 bytes
    public bool IsActive;     // 1 byte
    // Total: 13 bytes (+ 3 padding = 16 bytes with wasted space)
}
```

### Avoid LINQ in Hot Paths

```csharp
// ✅ DO: Manual loops for systems (performance)
int herbivoreCount = 0;
for (int i = 0; i < entities.Length; i++)
{
    if (entities[i].Type == EntityType.Herbivore)
        herbivoreCount++;
}

// ❌ DON'T: LINQ in 60Hz systems (allocates)
int herbivoreCount = entities.Count(e => e.Type == EntityType.Herbivore);

// ✅ OK: LINQ in setup/UI code (readability)
var initialHerbivores = config.Entities
    .Where(e => e.Type == EntityType.Herbivore)
    .ToList();
```

---

## Testing Standards

### Unit Test Structure

```csharp
[Fact]
public void MovementSystem_UpdatesPosition_WhenVelocityNonZero()
{
    // Arrange
    var world = new WorldBuilder()
        .WithEntity(EntityType.Herbivore, position: Vector2.Zero, velocity: new Vector2(10, 0))
        .Build();
    
    // Act
    MovementSystem.Update(world, deltaTime: 1.0f);
    
    // Assert
    world.GetTransform(0).Position.Should().Be(new Vector2(10, 0));
}

[Theory]
[InlineData(0, 0)]
[InlineData(10, 10)]
[InlineData(-5, 5)]
public void MovementSystem_WrapsPosition_AtWorldBoundaries(float x, float y)
{
    // Arrange
    var world = new WorldBuilder()
        .WithWorldSize(100, 100)
        .WithEntity(position: new Vector2(x, y))
        .Build();
    
    // Act
    MovementSystem.Update(world, deltaTime: 1.0f);
    
    // Assert
    var pos = world.GetTransform(0).Position;
    pos.X.Should().BeInRange(0, 100);
    pos.Y.Should().BeInRange(0, 100);
}
```

### Test Naming

```
[ClassName]_[MethodName]_[ExpectedBehavior]_[StateUnderTest]

Examples:
- MovementSystem_Update_IncreasesPosition_WhenVelocityPositive
- HerbivoreFactory_Create_SetsCorrectStats_WithDefaultConfig
- World_FreeEntitySlot_ReusesSlot_AfterEntityDeath
```

### Test Builders

```csharp
public class WorldBuilder
{
    private int _maxEntities = 100;
    private Vector2 _worldSize = new(1000, 1000);
    private List<EntityConfig> _entities = new();
    
    public WorldBuilder WithMaxEntities(int max)
    {
        _maxEntities = max;
        return this;
    }
    
    public WorldBuilder WithEntity(
        EntityType type, 
        Vector2? position = null,
        Vector2? velocity = null)
    {
        _entities.Add(new EntityConfig(type, position, velocity));
        return this;
    }
    
    public World Build()
    {
        var world = new World(_maxEntities, _worldSize);
        
        foreach (var config in _entities)
        {
            EntityFactory.Create(world, config.Type, config.Position);
            // Set velocity if provided
        }
        
        return world;
    }
}
```

### Integration Tests

```csharp
[Fact]
public void Simulation_ProducesPredatorPreyCycles_Over10000Ticks()
{
    // Arrange
    var world = new WorldBuilder()
        .WithEntity(EntityType.Herbivore, count: 100)
        .WithEntity(EntityType.Carnivore, count: 20)
        .WithEntity(EntityType.Plant, count: 500)
        .Build();
    
    var simulation = new SimulationEngine(world);
    var tracker = new PopulationTracker();
    
    // Act
    for (int tick = 0; tick < 10000; tick++)
    {
        simulation.Tick(1.0f / 60.0f);
        tracker.Record(world);
    }
    
    // Assert
    tracker.HerbivorePopulation.Should().HaveOscillations(minPeriod: 1000);
    tracker.CarnivorePopulation.Should().LagBehind(tracker.HerbivorePopulation);
    tracker.Should().NotHaveExtinctions();
}
```

---

## Documentation Standards

### XML Documentation

```csharp
/// <summary>
/// Processes entity movement and applies world boundary wrapping.
/// </summary>
/// <param name="world">World containing entities to update.</param>
/// <param name="deltaTime">Time elapsed since last update in seconds.</param>
/// <remarks>
/// Movement is applied as: position += velocity * deltaTime.
/// Entities exceeding world boundaries wrap to opposite side.
/// </remarks>
public static void Update(World world, float deltaTime)
{
    // Implementation
}
```

### Inline Comments

```csharp
// ✅ DO: Explain WHY, not WHAT
// Multiply by 0.7 because carnivores only extract 70% of prey energy
float energyGained = preyEnergy * 0.7f;

// ❌ DON'T: State the obvious
// Set energy to the energy gained
float energyGained = preyEnergy * 0.7f;
```

### Architecture Decision Records

Document significant decisions in `docs/` folder:

```markdown
# ADR-XXX: Decision Title

**Status:** Accepted | Rejected | Superseded  
**Date:** 2026-02-04

## Context
What problem are we solving?

## Decision
What did we decide to do?

## Rationale
Why this solution over alternatives?

## Consequences
What are the trade-offs?

## Alternatives Considered
What else did we evaluate?
```

---

## Error Handling

### Exceptions

```csharp
// ✅ DO: Throw for exceptional cases
public int AllocateEntitySlot()
{
    if (_entityCount >= MaxEntities)
        throw new InvalidOperationException(
            $"World full: {_entityCount}/{MaxEntities} entities"
        );
    
    return _entityCount++;
}

// ✅ DO: Use specific exception types
public Entity GetEntity(int index)
{
    if (index < 0 || index >= _entityCount)
        throw new ArgumentOutOfRangeException(
            nameof(index),
            index,
            $"Entity index must be between 0 and {_entityCount - 1}"
        );
    
    return _entities[index];
}

// ❌ DON'T: Swallow exceptions silently
try
{
    SaveWorld(path);
}
catch (IOException)
{
    // Silent failure - user never knows!
}

// ✅ DO: Log and re-throw or handle gracefully
try
{
    SaveWorld(path);
}
catch (IOException ex)
{
    _logger.Error(ex, "Failed to save world to {Path}", path);
    // Show error to user
    throw;
}
```

### Defensive Programming

```csharp
// ✅ DO: Validate inputs
public void CreateEntity(EntityType type, Vector2 position)
{
    ArgumentNullException.ThrowIfNull(type);
    
    if (!Enum.IsDefined(type))
        throw new ArgumentException($"Invalid entity type: {type}", nameof(type));
    
    // Continue with valid inputs
}

// ✅ DO: Use Debug.Assert for internal invariants
Debug.Assert(_entityCount <= MaxEntities, "Entity count exceeded max");
```

---

## Git Practices

### Commits

```bash
# ✅ DO: Atomic commits (one logical change)
git commit -m "feat(entities): Add Herbivore reproduction logic"

# ✅ DO: Descriptive messages
git commit -m "fix(renderer): Prevent buffer overflow in terminal rendering

Terminal buffer was not resizing when window changed,
causing array index out of bounds. Now dynamically
resizes on window size change events."

# ❌ DON'T: Vague messages
git commit -m "fix stuff"
git commit -m "wip"
```

### Branching

```bash
# ✅ DO: Descriptive branch names
git checkout -b feature/milestone-2-spatial-grid-optimization

# ❌ DON'T: Generic names
git checkout -b fix-bug
git checkout -b temp-work
```

---

## Code Review Checklist

### For Authors
- [ ] Code compiles without warnings
- [ ] All tests pass locally
- [ ] New tests added for new features
- [ ] Documentation updated
- [ ] No commented-out code
- [ ] No debug print statements
- [ ] Performance impact considered

### For Reviewers
- [ ] Logic is correct and clear
- [ ] Edge cases handled
- [ ] Error handling appropriate
- [ ] Tests are meaningful
- [ ] Performance acceptable
- [ ] Follows coding standards
- [ ] Documentation adequate

---

## Performance Profiling

### BenchmarkDotNet Usage

```csharp
[MemoryDiagnoser]
public class SystemBenchmarks
{
    private World _world;
    
    [GlobalSetup]
    public void Setup()
    {
        _world = new WorldBuilder()
            .WithEntity(EntityType.Herbivore, count: 5000)
            .Build();
    }
    
    [Benchmark]
    public void MovementSystem_5000Entities()
    {
        MovementSystem.Update(_world, 0.016f);
    }
}

// Run: dotnet run -c Release --project DinoLife.Benchmarks
```

### Profiling Tools
- **dotnet-trace:** CPU sampling
- **dotnet-counters:** Real-time metrics
- **BenchmarkDotNet:** Micro-benchmarks
- **Visual Studio Profiler:** Detailed analysis

---

*Last updated: 2026-02-19*
