# Entity Design

## Overview

All entities follow a data-oriented design where data is stored in parallel component arrays and logic is implemented in stateless systems.

## Component Architecture

### Core Components

#### Transform Component
```csharp
/// <summary>
/// Spatial properties of an entity.
/// </summary>
public struct Transform
{
    public Vector2 Position;
    public float Rotation;        // Radians, 0 = right, π/2 = up
    
    public Transform(Vector2 position)
    {
        Position = position;
        Rotation = 0f;
    }
}
```

#### Metabolism Component
```csharp
/// <summary>
/// Energy and hunger mechanics.
/// </summary>
public struct Metabolism
{
    public float Energy;          // Current energy [0, MaxEnergy]
    public float MaxEnergy;       // Capacity
    public float HungerRate;      // Energy depleted per second
    public float EnergyGainRate;  // Multiplier when eating
    
    public bool IsStarving => Energy <= 0f;
    public bool IsSatiated => Energy >= MaxEnergy * 0.8f;
}
```

#### Movement Component
```csharp
/// <summary>
/// Movement and velocity properties.
/// </summary>
public struct Movement
{
    public Vector2 Velocity;      // Current velocity
    public float Speed;           // Max speed (units/second)
    public float Acceleration;    // Rate of speed change
    
    public void SetDirection(Vector2 direction)
    {
        Velocity = direction.Normalized() * Speed;
    }
}
```

#### Diet Component
```csharp
/// <summary>
/// What this entity can eat.
/// </summary>
public struct Diet
{
    public FoodType FoodType;     // What it consumes
    public float DetectionRadius; // How far it can detect food
    public float EatRadius;       // How close to eat
    public float EatingDuration;  // Seconds to consume
}

public enum FoodType
{
    None,
    Plant,
    Herbivore,
    Corpse
}
```

#### Reproduction Component
```csharp
/// <summary>
/// Reproduction mechanics.
/// </summary>
public struct Reproduction
{
    public float ReproductionThreshold;  // Min energy to reproduce
    public float ReproductionCost;       // Energy consumed when reproducing
    public float Cooldown;               // Time since last reproduction
    public float CooldownDuration;       // Required time between reproductions
    
    public bool CanReproduce(float currentEnergy)
    {
        return currentEnergy >= ReproductionThreshold && Cooldown >= CooldownDuration;
    }
}
```

#### Lifespan Component (Optional)
```csharp
/// <summary>
/// Age and death by old age.
/// </summary>
public struct Lifespan
{
    public float Age;             // Seconds alive
    public float MaxAge;          // Die when Age > MaxAge
    
    public bool IsDead => Age >= MaxAge;
}
```

---

## Entity Types

### 1. Herbivore

**Role:** Primary consumer, converts plant energy to biomass, prey for carnivores.

**Components:**
- Transform
- Metabolism
- Movement
- Diet (FoodType.Plant)
- Reproduction
- Lifespan

**Base Stats:**
```csharp
public static class HerbivoreStats
{
    public const float MaxEnergy = 100f;
    public const float HungerRate = 1.0f;           // Energy/second
    public const float Speed = 2.5f;                // Units/second
    public const float DetectionRadius = 15f;       // Units
    public const float EatRadius = 1.5f;
    public const float ReproductionThreshold = 80f;
    public const float ReproductionCost = 40f;
    public const float ReproductionCooldown = 20f;  // Seconds
    public const float MaxAge = 300f;               // Seconds (~5 minutes)
}
```

**Behavior:**
1. Wander randomly if no plants detected
2. Move towards nearest plant if detected
3. Eat plant when in range (gain energy)
4. Flee from carnivores if detected
5. Reproduce when energy threshold met
6. Die when energy = 0 or age > max

**Stat Variation:** ±10% on Speed, HungerRate, MaxAge

---

### 2. Carnivore

**Role:** Apex predator, population control, converts herbivore biomass.

**Components:**
- Transform
- Metabolism
- Movement
- Diet (FoodType.Herbivore)
- Reproduction
- Lifespan

**Base Stats:**
```csharp
public static class CarnivoreStats
{
    public const float MaxEnergy = 150f;
    public const float HungerRate = 1.5f;           // Higher metabolism
    public const float Speed = 3.5f;                // Faster than herbivores
    public const float DetectionRadius = 20f;       // Better detection
    public const float CatchRadius = 1.0f;          // Must catch prey
    public const float ReproductionThreshold = 120f;
    public const float ReproductionCost = 60f;
    public const float ReproductionCooldown = 30f;
    public const float MaxAge = 400f;
}
```

**Behavior:**
1. Wander if no prey detected
2. Chase nearest herbivore if detected
3. Kill herbivore when in catch radius (instant)
4. Gain energy from kill
5. Reproduce when energy threshold met
6. Die when starved or aged

**Stat Variation:** ±10% on Speed, HungerRate, DetectionRadius

---

### 3. Plant

**Role:** Primary producer, energy source for herbivores.

**Components:**
- Transform
- Metabolism (simplified: no hunger, only growth)

**Base Stats:**
```csharp
public static class PlantStats
{
    public const float MaxEnergy = 50f;
    public const float GrowthRate = 0.5f;           // Energy/second when growing
    public const float EnergyProvided = 30f;        // Energy given when eaten
    public const float RespawnTime = 60f;           // Seconds to regrow after eaten
}
```

**Behavior:**
1. Grow over time (energy increases)
2. Provide energy when eaten
3. Mark as "eaten" (invisible, no collision)
4. Respawn after timer expires
5. No death, no reproduction (static count)

**Stat Variation:** ±10% on GrowthRate, RespawnTime

**Implementation Note:** Plants don't die, they cycle between active/inactive states.

---

### 4. Scavenger

**Role:** Cleanup crew, consumes corpses, reduces clutter.

**Components:**
- Transform
- Metabolism
- Movement
- Diet (FoodType.Corpse)
- Reproduction
- Lifespan

**Base Stats:**
```csharp
public static class ScavengerStats
{
    public const float MaxEnergy = 80f;
    public const float HungerRate = 0.8f;           // Lower metabolism
    public const float Speed = 2.0f;                // Slower than herbivores
    public const float DetectionRadius = 25f;       // Best detection
    public const float EatRadius = 1.5f;
    public const float ReproductionThreshold = 60f;
    public const float ReproductionCost = 30f;
    public const float ReproductionCooldown = 25f;
    public const float MaxAge = 350f;
}
```

**Behavior:**
1. Wander if no corpses detected
2. Move towards nearest corpse
3. Eat corpse (remove from world)
4. Gain energy from corpse
5. Reproduce when threshold met
6. Die when starved or aged

**Stat Variation:** ±10% on Speed, HungerRate, DetectionRadius

---

## Corpse System

Corpses are not entities but temporary world objects.

```csharp
public struct Corpse
{
    public Guid Id;
    public Vector2 Position;
    public float Energy;          // Energy available to scavengers
    public float DecayTimer;      // Seconds until auto-removal
}

public static class CorpseStats
{
    public const float DecayTime = 120f;      // 2 minutes
    public const float EnergyRetention = 0.5f; // 50% of entity's energy at death
}
```

**Lifecycle:**
1. Created when entity dies
2. Provides energy = 50% of dead entity's energy
3. Decays over time
4. Removed when eaten or decay timer expires

---

## Entity Creation

### Factory Pattern

```csharp
public static class EntityFactory
{
    public static int CreateHerbivore(World world, Vector2 position, HerbivoreStats? stats = null)
    {
        stats ??= HerbivoreStats.Default;
        
        int slot = world.AllocateEntitySlot();
        
        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Herbivore,
            Flags = ComponentFlags.Transform | 
                    ComponentFlags.Metabolism | 
                    ComponentFlags.Movement |
                    ComponentFlags.Diet |
                    ComponentFlags.Reproduction |
                    ComponentFlags.Lifespan,
            IsAlive = true
        };
        
        world.Transforms[slot] = new Transform(position);
        
        world.Metabolisms[slot] = new Metabolism
        {
            Energy = stats.MaxEnergy * 0.5f, // Start at 50%
            MaxEnergy = stats.MaxEnergy,
            HungerRate = stats.HungerRate,
            EnergyGainRate = 1.0f
        };
        
        world.Movements[slot] = new Movement
        {
            Velocity = Vector2.Zero,
            Speed = stats.Speed,
            Acceleration = stats.Speed * 2f
        };
        
        world.Diets[slot] = new Diet
        {
            FoodType = FoodType.Plant,
            DetectionRadius = stats.DetectionRadius,
            EatRadius = stats.EatRadius,
            EatingDuration = 2f
        };
        
        world.Reproductions[slot] = new Reproduction
        {
            ReproductionThreshold = stats.ReproductionThreshold,
            ReproductionCost = stats.ReproductionCost,
            Cooldown = stats.ReproductionCooldown, // Start ready
            CooldownDuration = stats.ReproductionCooldown
        };
        
        world.Lifespans[slot] = new Lifespan
        {
            Age = 0f,
            MaxAge = stats.MaxAge
        };
        
        return slot;
    }
    
    // Similar methods for Carnivore, Plant, Scavenger
}
```

### Stat Variation (Reproduction)

```csharp
public static HerbivoreStats ApplyVariation(HerbivoreStats parent, Random rng)
{
    return new HerbivoreStats
    {
        Speed = Vary(parent.Speed, 0.1f, rng),
        HungerRate = Vary(parent.HungerRate, 0.1f, rng),
        MaxAge = Vary(parent.MaxAge, 0.1f, rng),
        // Other stats remain same
    };
}

private static float Vary(float value, float variance, Random rng)
{
    float multiplier = 1f + (rng.NextSingle() * 2f - 1f) * variance; // [0.9, 1.1]
    return value * multiplier;
}
```

---

## Initial Population

**World Start Configuration:**
```csharp
public class InitialPopulation
{
    public int Herbivores = 100;
    public int Carnivores = 20;
    public int Plants = 500;
    public int Scavengers = 30;
    
    public Vector2 WorldSize = new Vector2(1000, 1000);
}
```

**Distribution:**
- Random scatter across world
- Minimum distance between entities (5 units)
- Plants clustered in patches (future: biome support)

---

## Behavioral Priorities

### Herbivore Decision Tree
```
1. Is starving (<20% energy)?
   YES → Find nearest plant, ignore carnivores
   NO  → Continue
2. Carnivore in detection range?
   YES → Flee from carnivore
   NO  → Continue
3. Plant in detection range?
   YES → Move towards plant
   NO  → Continue
4. Can reproduce?
   YES → Reproduce
   NO  → Continue
5. Wander randomly
```

### Carnivore Decision Tree
```
1. Is starving (<30% energy)?
   YES → Aggressively chase nearest herbivore
   NO  → Continue
2. Herbivore in detection range?
   YES → Chase herbivore
   NO  → Continue
3. Can reproduce?
   YES → Reproduce
   NO  → Continue
4. Wander randomly
```

### Scavenger Decision Tree
```
1. Corpse in detection range?
   YES → Move towards corpse
   NO  → Continue
2. Can reproduce?
   YES → Reproduce
   NO  → Continue
3. Wander randomly
```

---

## Performance Considerations

**Component Array Allocation:**
- Pre-allocate arrays for max entities (5000)
- Use sparse indexing (some slots empty)
- Free list for slot reuse

**Spatial Queries:**
- All detection uses SpatialGrid
- Never O(n²) iteration
- Grid cell size = max detection radius / 2

**Entity Slot Management:**
```csharp
public class World
{
    private const int MAX_ENTITIES = 5000;
    
    private Entity[] _entities = new Entity[MAX_ENTITIES];
    private Stack<int> _freeSlots = new Stack<int>();
    private int _entityCount = 0;
    
    public int AllocateEntitySlot()
    {
        if (_freeSlots.Count > 0)
            return _freeSlots.Pop();
        
        if (_entityCount >= MAX_ENTITIES)
            throw new InvalidOperationException("World full");
        
        return _entityCount++;
    }
    
    public void FreeEntitySlot(int slot)
    {
        _entities[slot].IsAlive = false;
        _freeSlots.Push(slot);
    }
}
```

---

*Last updated: 2026-02-19*
