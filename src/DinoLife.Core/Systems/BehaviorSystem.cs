using System;
using System.Collections.Generic;
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
    private const float ScavengerHuntThreshold = 0.15f;
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
        var candidates = new List<int>(64);

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Movement)) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }

            switch (entities[i].Type)
            {
                case EntityType.Carnivore:
                    HandleCarnivore(planet, i, entities, transforms, movements, metabolisms, diets, plants, candidates);
                    break;

                case EntityType.Herbivore:
                    HandleHerbivore(planet, i, entities, transforms, movements, metabolisms, diets, plants, candidates);
                    break;

                case EntityType.Scavenger:
                    HandleScavenger(planet, i, entities, transforms, movements, diets, candidates);
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
        Span<Plant> plants,
        List<int> candidates)
    {
        if (IsHungry(entities, index, metabolisms) && HasDiet(entities, index, diets, FoodType.Herbivore))
        {
            float radius = diets[index].DetectionRadius > 0f ? diets[index].DetectionRadius : ChaseRadius;
            if (TryFindNearest(planet, EntityType.Herbivore, index, entities, transforms, plants, radius, candidates, out Vector2 chaseDirection))
            {
                movements[index].SetDirection(chaseDirection);
                return;
            }

            if (IsVeryHungry(entities, index, metabolisms)
                && TryFindNearest(planet, EntityType.Scavenger, index, entities, transforms, plants, radius, candidates, out Vector2 scavengerDirection))
            {
                movements[index].SetDirection(scavengerDirection);
                return;
            }
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
        Span<Plant> plants,
        List<int> candidates)
    {
        if (IsHungry(entities, index, metabolisms) && HasDiet(entities, index, diets, FoodType.Plant))
        {
            float radius = diets[index].DetectionRadius;
            if (TryFindNearest(planet, EntityType.Plant, index, entities, transforms, plants, radius, candidates, out Vector2 foodDirection))
            {
                movements[index].SetDirection(foodDirection);
                return;
            }
        }

        if (TryFindNearest(
                planet,
                EntityType.Carnivore,
                index,
                entities,
                transforms,
                plants,
                FleeRadius,
                candidates,
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

    private void HandleScavenger(
        Planet planet,
        int index,
        Span<Entity> entities,
        Span<Transform> transforms,
        Span<Movement> movements,
        Span<Diet> diets,
        List<int> candidates)
    {
        if (TryFindNearest(
                planet,
                EntityType.Carnivore,
                index,
                entities,
                transforms,
                planet.Plants.AsSpan(0, planet.EntityCount),
                FleeRadius,
                candidates,
                out Vector2 fleeDirection))
        {
            movements[index].SetDirection(new Vector2(-fleeDirection.X, -fleeDirection.Y));
            return;
        }

        // Scavengers prioritize moving toward nearby corpses.
        if (TryFindNearestCorpse(planet, index, transforms, diets, out Vector2 corpseDirection))
        {
            movements[index].SetDirection(corpseDirection);
            return;
        }

        HandleRandomWalk(planet, index, entities, movements);
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
        Planet planet,
        EntityType targetType,
        int sourceIndex,
        Span<Entity> entities,
        Span<Transform> transforms,
        Span<Plant> plants,
        float radius,
        List<int> candidates,
        out Vector2 direction)
    {
        direction = Vector2.Zero;
        float radiusSq = radius * radius;
        float bestDistSq = float.MaxValue;

        Vector2 sourcePos = transforms[sourceIndex].Position;

        planet.SpatialGrid.QueryRadius(sourcePos, radius, candidates);

        for (int c = 0; c < candidates.Count; c++)
        {
            int i = candidates[c];
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

    private static bool TryFindNearestCorpse(
        Planet planet,
        int sourceIndex,
        Span<Transform> transforms,
        Span<Diet> diets,
        out Vector2 direction)
    {
        direction = Vector2.Zero;

        if (planet.Corpses.Count == 0) { return false; }

        float radius = 25f;
        if (sourceIndex < diets.Length && diets[sourceIndex].DetectionRadius > 0f)
        {
            radius = diets[sourceIndex].DetectionRadius;
        }

        float radiusSq = radius * radius;
        float bestDistSq = float.MaxValue;
        Vector2 sourcePos = transforms[sourceIndex].Position;

        var corpses = planet.Corpses;
        for (int i = 0; i < corpses.Count; i++)
        {
            Vector2 delta = corpses[i].Position - sourcePos;
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

    private static bool IsVeryHungry(Span<Entity> entities, int index, Span<Metabolism> metabolisms)
    {
        if (!entities[index].Has(ComponentFlags.Metabolism)) { return false; }
        float max = metabolisms[index].MaxEnergy;
        if (max <= 0f) { return false; }
        return metabolisms[index].Energy <= max * ScavengerHuntThreshold;
    }

    private static bool HasDiet(Span<Entity> entities, int index, Span<Diet> diets, FoodType foodType)
    {
        if (!entities[index].Has(ComponentFlags.Diet)) { return false; }
        return diets[index].FoodType == foodType;
    }
}
