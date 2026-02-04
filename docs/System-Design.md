# System Design

## Overview

Systems are stateless logic processors that operate on component arrays. Each system runs once per tick in a defined order.

## System Execution Order

```
1. MovementSystem           → Update positions
2. MetabolismSystem         → Deplete energy, handle starvation
3. DetectionSystem          → Find food/threats in range
4. BehaviorSystem           → Make decisions, set velocities
5. HuntingSystem            → Handle carnivore attacks
6. FeedingSystem            → Handle eating mechanics
7. PlantGrowthSystem        → Grow plants, respawn eaten ones
8. ReproductionSystem       → Create offspring
9. DeathSystem              → Handle deaths, create corpses
10. SpatialGridSystem       → Update entity positions in grid
```

**Rationale:** Movement → Sensing → Decision → Action → Consequences

---

## System Implementations

### 1. MovementSystem

**Responsibility:** Apply velocity to transform positions, enforce world boundaries.

```csharp
public static class MovementSystem
{
    public static void Update(World world, float deltaTime)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        Span<Movement> movements = world.Movements.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            if (!entities[i].Flags.HasFlag(ComponentFlags.Movement)) continue;
            
            // Apply velocity
            transforms[i].Position += movements[i].Velocity * deltaTime;
            
            // Update rotation to match velocity
            if (movements[i].Velocity.LengthSquared() > 0.01f)
            {
                transforms[i].Rotation = MathF.Atan2(
                    movements[i].Velocity.Y, 
                    movements[i].Velocity.X
                );
            }
            
            // Wrap around world boundaries
            transforms[i].Position = WrapPosition(
                transforms[i].Position, 
                world.Config.WorldSize
            );
        }
    }
    
    private static Vector2 WrapPosition(Vector2 pos, Vector2 worldSize)
    {
        float x = pos.X % worldSize.X;
        float y = pos.Y % worldSize.Y;
        
        if (x < 0) x += worldSize.X;
        if (y < 0) y += worldSize.Y;
        
        return new Vector2(x, y);
    }
}
```

**Performance:** O(n) where n = entity count. ~0.1ms for 5000 entities.

---

### 2. MetabolismSystem

**Responsibility:** Deplete energy based on hunger rate, mark starvation.

```csharp
public static class MetabolismSystem
{
    public static void Update(World world, float deltaTime)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Metabolism> metabolisms = world.Metabolisms.AsSpan(0, world.EntityCount);
        Span<Lifespan> lifespans = world.Lifespans.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            if (!entities[i].Flags.HasFlag(ComponentFlags.Metabolism)) continue;
            
            // Deplete energy
            metabolisms[i].Energy -= metabolisms[i].HungerRate * deltaTime;
            metabolisms[i].Energy = Math.Max(0f, metabolisms[i].Energy);
            
            // Age entity
            if (entities[i].Flags.HasFlag(ComponentFlags.Lifespan))
            {
                lifespans[i].Age += deltaTime;
            }
        }
    }
}
```

**Performance:** O(n), ~0.05ms for 5000 entities.

---

### 3. DetectionSystem

**Responsibility:** Find nearby food and threats using spatial grid.

```csharp
public static class DetectionSystem
{
    public static void Update(World world)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        Span<Diet> diets = world.Diets.AsSpan(0, world.EntityCount);
        
        // Clear previous detections
        world.DetectedFood.Clear();
        world.DetectedThreats.Clear();
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            if (!entities[i].Flags.HasFlag(ComponentFlags.Diet)) continue;
            
            Vector2 pos = transforms[i].Position;
            float radius = diets[i].DetectionRadius;
            
            // Query spatial grid for nearby entities
            var nearbyIndices = world.SpatialGrid.QueryRadius(pos, radius);
            
            foreach (int j in nearbyIndices)
            {
                if (i == j) continue;
                if (!entities[j].IsAlive) continue;
                
                float distanceSq = Vector2.DistanceSquared(pos, transforms[j].Position);
                
                if (distanceSq <= radius * radius)
                {
                    // Check if this is food
                    if (IsFood(entities[i].Type, entities[j].Type, diets[i].FoodType))
                    {
                        world.DetectedFood.Add(i, j);
                    }
                    
                    // Check if this is a threat (carnivore for herbivore)
                    if (IsThreat(entities[i].Type, entities[j].Type))
                    {
                        world.DetectedThreats.Add(i, j);
                    }
                }
            }
        }
        
        // Detect corpses
        foreach (var corpse in world.Corpses)
        {
            var scavengers = world.SpatialGrid.QueryRadius(corpse.Position, 25f);
            
            foreach (int scavengerIdx in scavengers)
            {
                if (entities[scavengerIdx].Type == EntityType.Scavenger)
                {
                    world.DetectedCorpses.Add(scavengerIdx, corpse.Id);
                }
            }
        }
    }
    
    private static bool IsFood(EntityType hunter, EntityType prey, FoodType diet)
    {
        return (diet, prey) switch
        {
            (FoodType.Plant, EntityType.Plant) => true,
            (FoodType.Herbivore, EntityType.Herbivore) => true,
            _ => false
        };
    }
    
    private static bool IsThreat(EntityType entity, EntityType other)
    {
        return entity == EntityType.Herbivore && other == EntityType.Carnivore;
    }
}
```

**Performance:** O(n * k) where k = avg neighbors. ~2ms for 5000 entities with grid.

---

### 4. BehaviorSystem

**Responsibility:** Make decisions, set velocity directions based on detections.

```csharp
public static class BehaviorSystem
{
    public static void Update(World world, float deltaTime)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        Span<Movement> movements = world.Movements.AsSpan(0, world.EntityCount);
        Span<Metabolism> metabolisms = world.Metabolisms.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            
            switch (entities[i].Type)
            {
                case EntityType.Herbivore:
                    UpdateHerbivoreBehavior(i, world, ref movements[i], transforms, metabolisms);
                    break;
                    
                case EntityType.Carnivore:
                    UpdateCarnivoreBehavior(i, world, ref movements[i], transforms, metabolisms);
                    break;
                    
                case EntityType.Scavenger:
                    UpdateScavengerBehavior(i, world, ref movements[i], transforms);
                    break;
            }
        }
    }
    
    private static void UpdateHerbivoreBehavior(
        int index, 
        World world, 
        ref Movement movement,
        Span<Transform> transforms,
        Span<Metabolism> metabolisms)
    {
        Vector2 position = transforms[index].Position;
        
        // Priority 1: Flee from carnivores
        if (world.DetectedThreats.TryGetTargets(index, out var threats))
        {
            Vector2 fleeDirection = CalculateFleeDirection(position, threats, transforms);
            movement.Velocity = fleeDirection * movement.Speed;
            return;
        }
        
        // Priority 2: Move towards food (plants)
        if (world.DetectedFood.TryGetTargets(index, out var food))
        {
            int nearestPlant = GetNearest(position, food, transforms);
            Vector2 direction = (transforms[nearestPlant].Position - position).Normalized();
            movement.Velocity = direction * movement.Speed;
            return;
        }
        
        // Priority 3: Wander randomly
        movement.Velocity = GetWanderVelocity(index, movement.Speed, world.Random);
    }
    
    private static void UpdateCarnivoreBehavior(
        int index,
        World world,
        ref Movement movement,
        Span<Transform> transforms,
        Span<Metabolism> metabolisms)
    {
        Vector2 position = transforms[index].Position;
        
        // Chase nearest herbivore
        if (world.DetectedFood.TryGetTargets(index, out var prey))
        {
            int nearestPrey = GetNearest(position, prey, transforms);
            Vector2 direction = (transforms[nearestPrey].Position - position).Normalized();
            
            // Speed boost when starving
            float speedMultiplier = metabolisms[index].IsStarving ? 1.2f : 1.0f;
            movement.Velocity = direction * movement.Speed * speedMultiplier;
            return;
        }
        
        // Wander if no prey
        movement.Velocity = GetWanderVelocity(index, movement.Speed, world.Random);
    }
    
    private static void UpdateScavengerBehavior(
        int index,
        World world,
        ref Movement movement,
        Span<Transform> transforms)
    {
        Vector2 position = transforms[index].Position;
        
        // Move towards nearest corpse
        if (world.DetectedCorpses.TryGetTargets(index, out var corpses))
        {
            Guid nearestCorpseId = corpses.First();
            Corpse corpse = world.Corpses.First(c => c.Id == nearestCorpseId);
            Vector2 direction = (corpse.Position - position).Normalized();
            movement.Velocity = direction * movement.Speed;
            return;
        }
        
        // Wander if no corpses
        movement.Velocity = GetWanderVelocity(index, movement.Speed, world.Random);
    }
    
    private static Vector2 GetWanderVelocity(int seed, float speed, Random rng)
    {
        float angle = rng.NextSingle() * MathF.PI * 2f;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
    }
}
```

---

### 5. HuntingSystem

**Responsibility:** Handle carnivore kills when catching herbivores.

```csharp
public static class HuntingSystem
{
    public static void Update(World world)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        Span<Diet> diets = world.Diets.AsSpan(0, world.EntityCount);
        Span<Metabolism> metabolisms = world.Metabolisms.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            if (entities[i].Type != EntityType.Carnivore) continue;
            
            if (world.DetectedFood.TryGetTargets(i, out var prey))
            {
                foreach (int preyIdx in prey)
                {
                    float distance = Vector2.Distance(
                        transforms[i].Position,
                        transforms[preyIdx].Position
                    );
                    
                    if (distance <= diets[i].EatRadius)
                    {
                        // Kill prey
                        entities[preyIdx].IsAlive = false;
                        
                        // Gain energy
                        float energyGained = metabolisms[preyIdx].Energy * 0.7f;
                        metabolisms[i].Energy = Math.Min(
                            metabolisms[i].Energy + energyGained,
                            metabolisms[i].MaxEnergy
                        );
                        
                        // Create corpse (handled by DeathSystem)
                        world.PendingDeaths.Add(preyIdx);
                        
                        break; // One kill per tick
                    }
                }
            }
        }
    }
}
```

---

### 6. FeedingSystem

**Responsibility:** Handle herbivores eating plants, scavengers eating corpses.

```csharp
public static class FeedingSystem
{
    public static void Update(World world, float deltaTime)
    {
        HandlePlantFeeding(world, deltaTime);
        HandleCorpseFeeding(world, deltaTime);
    }
    
    private static void HandlePlantFeeding(World world, float deltaTime)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        Span<Diet> diets = world.Diets.AsSpan(0, world.EntityCount);
        Span<Metabolism> metabolisms = world.Metabolisms.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            if (entities[i].Type != EntityType.Herbivore) continue;
            
            if (world.DetectedFood.TryGetTargets(i, out var plants))
            {
                foreach (int plantIdx in plants)
                {
                    if (entities[plantIdx].Type != EntityType.Plant) continue;
                    
                    float distance = Vector2.Distance(
                        transforms[i].Position,
                        transforms[plantIdx].Position
                    );
                    
                    if (distance <= diets[i].EatRadius)
                    {
                        // Eat plant
                        float energyGained = PlantStats.EnergyProvided;
                        metabolisms[i].Energy = Math.Min(
                            metabolisms[i].Energy + energyGained,
                            metabolisms[i].MaxEnergy
                        );
                        
                        // Mark plant as eaten
                        world.MarkPlantEaten(plantIdx);
                        
                        break; // One plant per tick
                    }
                }
            }
        }
    }
    
    private static void HandleCorpseFeeding(World world, float deltaTime)
    {
        // Similar to plant feeding but for scavengers and corpses
        // Removes corpse when consumed
    }
}
```

---

### 7. PlantGrowthSystem

**Responsibility:** Grow plant energy, respawn eaten plants.

```csharp
public static class PlantGrowthSystem
{
    public static void Update(World world, float deltaTime)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Metabolism> metabolisms = world.Metabolisms.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i].Type != EntityType.Plant) continue;
            
            if (world.IsPlantEaten(i))
            {
                // Respawn timer
                world.PlantRespawnTimers[i] -= deltaTime;
                
                if (world.PlantRespawnTimers[i] <= 0f)
                {
                    world.RespawnPlant(i);
                }
            }
            else
            {
                // Grow energy
                metabolisms[i].Energy = Math.Min(
                    metabolisms[i].Energy + PlantStats.GrowthRate * deltaTime,
                    metabolisms[i].MaxEnergy
                );
            }
        }
    }
}
```

---

### 8. ReproductionSystem

**Responsibility:** Create offspring when conditions met.

```csharp
public static class ReproductionSystem
{
    public static void Update(World world, float deltaTime)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        Span<Metabolism> metabolisms = world.Metabolisms.AsSpan(0, world.EntityCount);
        Span<Reproduction> reproductions = world.Reproductions.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            if (!entities[i].Flags.HasFlag(ComponentFlags.Reproduction)) continue;
            
            // Update cooldown
            reproductions[i].Cooldown += deltaTime;
            
            // Check if can reproduce
            if (reproductions[i].CanReproduce(metabolisms[i].Energy))
            {
                // Create offspring
                Vector2 spawnPos = transforms[i].Position + GetRandomOffset(world.Random);
                
                int offspringIdx = entities[i].Type switch
                {
                    EntityType.Herbivore => EntityFactory.CreateHerbivore(world, spawnPos, parent: i),
                    EntityType.Carnivore => EntityFactory.CreateCarnivore(world, spawnPos, parent: i),
                    EntityType.Scavenger => EntityFactory.CreateScavenger(world, spawnPos, parent: i),
                    _ => -1
                };
                
                if (offspringIdx >= 0)
                {
                    // Cost energy
                    metabolisms[i].Energy -= reproductions[i].ReproductionCost;
                    
                    // Reset cooldown
                    reproductions[i].Cooldown = 0f;
                }
            }
        }
    }
}
```

---

### 9. DeathSystem

**Responsibility:** Remove dead entities, create corpses.

```csharp
public static class DeathSystem
{
    public static void Update(World world)
    {
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Metabolism> metabolisms = world.Metabolisms.AsSpan(0, world.EntityCount);
        Span<Lifespan> lifespans = world.Lifespans.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            
            bool shouldDie = false;
            
            // Death by starvation
            if (entities[i].Flags.HasFlag(ComponentFlags.Metabolism))
            {
                if (metabolisms[i].IsStarving)
                    shouldDie = true;
            }
            
            // Death by old age
            if (entities[i].Flags.HasFlag(ComponentFlags.Lifespan))
            {
                if (lifespans[i].IsDead)
                    shouldDie = true;
            }
            
            if (shouldDie)
            {
                // Create corpse (except plants)
                if (entities[i].Type != EntityType.Plant)
                {
                    world.CreateCorpse(transforms[i].Position, metabolisms[i].Energy);
                }
                
                // Free entity slot
                world.FreeEntitySlot(i);
            }
        }
    }
}
```

---

### 10. SpatialGridSystem

**Responsibility:** Update spatial grid with current entity positions.

```csharp
public static class SpatialGridSystem
{
    public static void Update(World world)
    {
        world.SpatialGrid.Clear();
        
        Span<Entity> entities = world.Entities.AsSpan(0, world.EntityCount);
        Span<Transform> transforms = world.Transforms.AsSpan(0, world.EntityCount);
        
        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) continue;
            
            world.SpatialGrid.Insert(i, transforms[i].Position);
        }
    }
}
```

---

## Performance Budget

| System | Target Time (5000 entities) |
|--------|----------------------------|
| MovementSystem | 0.1ms |
| MetabolismSystem | 0.05ms |
| DetectionSystem | 2.0ms |
| BehaviorSystem | 1.0ms |
| HuntingSystem | 0.5ms |
| FeedingSystem | 0.5ms |
| PlantGrowthSystem | 0.1ms |
| ReproductionSystem | 0.3ms |
| DeathSystem | 0.2ms |
| SpatialGridSystem | 0.5ms |
| **Total** | **~5.25ms** |

**Budget:** 16.67ms per tick @ 60 TPS  
**Headroom:** ~11ms for overhead, rendering, input

---

*Last updated: 2026-02-04*
