using System;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Sets movement directions based on simple behaviors (random walk, chase, flee).
/// </summary>
public class BehaviorSystem : ISystem
{
    private const int DirectionChangeIntervalTicks = 30;
    private const float FleeRadius = 12f;
    private const float ChaseRadius = 20f;
    private const float HungerThreshold = 0.3f;
    private const float MinDirectionLengthSquared = 0.0001f;

    /// <summary>
    /// Update entity velocities on the <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);
        Span<Movement> movements = planet.Movements.AsSpan(0, planet.EntityCount);
        Span<Metabolism> metabolisms = planet.Metabolisms.AsSpan(0, planet.EntityCount);
        Span<Diet> diets = planet.Diets.AsSpan(0, planet.EntityCount);
        Span<Plant> plants = planet.Plants.AsSpan(0, planet.EntityCount);

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Movement)) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }

            switch (entities[i].Type)
            {
                case EntityType.Carnivore:
                    HandleCarnivore(planet, i, entities, transforms, movements, metabolisms, diets, plants);
                    break;

                case EntityType.Herbivore:
                    HandleHerbivore(planet, i, entities, transforms, movements, metabolisms, diets, plants);
                    break;

                case EntityType.Scavenger:
                    HandleRandomWalk(planet, i, entities, movements);
                    break;
            }
        }
    }

    private void HandleCarnivore(
        Planet planet,
        int index,
        Span<Entity> entities,
        Span<Transform> transforms,
        Span<Movement> movements,
        Span<Metabolism> metabolisms,
        Span<Diet> diets,
        Span<Plant> plants)
    {
        if (IsHungry(entities, index, metabolisms) && HasDiet(entities, index, diets, FoodType.Herbivore))
        {
            float radius = diets[index].DetectionRadius > 0f ? diets[index].DetectionRadius : ChaseRadius;
            if (TryFindNearest(EntityType.Herbivore, index, entities, transforms, plants, radius, out Vector2 chaseDirection))
            {
                movements[index].SetDirection(chaseDirection);
                return;
            }
        }

        if (TryFindNearest(
                EntityType.Herbivore,
                index,
                entities,
                transforms,
                plants,
                ChaseRadius,
                out Vector2 fallbackDirection))
        {
            movements[index].SetDirection(fallbackDirection);
            return;
        }

        HandleRandomWalk(planet, index, entities, movements);
    }

    private void HandleHerbivore(
        Planet planet,
        int index,
        Span<Entity> entities,
        Span<Transform> transforms,
        Span<Movement> movements,
        Span<Metabolism> metabolisms,
        Span<Diet> diets,
        Span<Plant> plants)
    {
        if (IsHungry(entities, index, metabolisms) && HasDiet(entities, index, diets, FoodType.Plant))
        {
            float radius = diets[index].DetectionRadius;
            if (TryFindNearest(EntityType.Plant, index, entities, transforms, plants, radius, out Vector2 foodDirection))
            {
                movements[index].SetDirection(foodDirection);
                return;
            }
        }

        if (TryFindNearest(
                EntityType.Carnivore,
                index,
                entities,
                transforms,
                plants,
                FleeRadius,
                out Vector2 fleeDirection))
        {
            movements[index].SetDirection(new Vector2(-fleeDirection.X, -fleeDirection.Y));
            return;
        }

        HandleRandomWalk(planet, index, entities, movements);
    }

    private void HandleRandomWalk(
        Planet planet,
        int index,
        Span<Entity> entities,
        Span<Movement> movements)
    {
        bool shouldChange = movements[index].Velocity.LengthSquared() < MinDirectionLengthSquared
            || (planet.Tick % DirectionChangeIntervalTicks) == 0;

        if (!shouldChange) { return; }

        uint seed = (uint)(entities[index].Id.GetHashCode() ^ (planet.Tick * 397));
        Vector2 direction = RandomDirection(seed);
        movements[index].SetDirection(direction);
    }

    private static Vector2 RandomDirection(uint seed)
    {
        uint hash = XorShift(seed == 0 ? 1u : seed);
        float t = (hash % 10000) / 10000f;
        float angle = t * (MathF.PI * 2f);
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    private static uint XorShift(uint value)
    {
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        return value;
    }

    private static bool TryFindNearest(
        EntityType targetType,
        int sourceIndex,
        Span<Entity> entities,
        Span<Transform> transforms,
        Span<Plant> plants,
        float radius,
        out Vector2 direction)
    {
        direction = Vector2.Zero;
        float radiusSq = radius * radius;
        float bestDistSq = float.MaxValue;

        Vector2 sourcePos = transforms[sourceIndex].Position;

        for (int i = 0; i < entities.Length; i++)
        {
            if (i == sourceIndex) { continue; }
            if (!entities[i].IsAlive) { continue; }
            if (entities[i].Type != targetType) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }
            if (targetType == EntityType.Plant && entities[i].Has(ComponentFlags.Plant) && !plants[i].IsActive)
            {
                continue;
            }

            Vector2 delta = transforms[i].Position - sourcePos;
            float distSq = delta.LengthSquared();
            if (distSq > radiusSq) { continue; }

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                direction = delta;
            }
        }

        return bestDistSq < float.MaxValue;
    }

    private static bool IsHungry(Span<Entity> entities, int index, Span<Metabolism> metabolisms)
    {
        if (!entities[index].Has(ComponentFlags.Metabolism)) { return false; }
        float max = metabolisms[index].MaxEnergy;
        if (max <= 0f) { return false; }
        return metabolisms[index].Energy <= max * HungerThreshold;
    }

    private static bool HasDiet(Span<Entity> entities, int index, Span<Diet> diets, FoodType foodType)
    {
        if (!entities[index].Has(ComponentFlags.Diet)) { return false; }
        return diets[index].FoodType == foodType;
    }
}
