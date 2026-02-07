using System;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Manages reproduction-related behavior and spawning of new entities.
/// </summary>
public class ReproductionSystem : ISystem
{
    /// <summary>
    /// Perform reproduction checks and spawn new entities on <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime)
    {
        Entity[] entityArray = planet.Entities;
        Transform[] transformArray = planet.Transforms;
        Movement[] movementArray = planet.Movements;
        Metabolism[] metabolismArray = planet.Metabolisms;
        Diet[] dietArray = planet.Diets;
        Reproduction[] reproductionArray = planet.Reproductions;

        Span<Entity> entities = entityArray.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = transformArray.AsSpan(0, planet.EntityCount);
        Span<Movement> movements = movementArray.AsSpan(0, planet.EntityCount);
        Span<Metabolism> metabolisms = metabolismArray.AsSpan(0, planet.EntityCount);
        Span<Diet> diets = dietArray.AsSpan(0, planet.EntityCount);
        Span<Reproduction> reproductions = reproductionArray.AsSpan(0, planet.EntityCount);

        float deltaTime = (float)deltatime;

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Reproduction)) { continue; }
            if (!entities[i].Has(ComponentFlags.Metabolism)) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }

            reproductions[i].Cooldown += deltaTime;

            if (entities[i].Type != EntityType.Herbivore) { continue; }
            if (!reproductions[i].CanReproduce(metabolisms[i].Energy)) { continue; }

            int childSlot;
            try
            {
                childSlot = planet.AllocateEntitySlot();
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            Vector2 childPos = SpawnNearby(transforms[i].Position, planet.WorldSize, entities[i].Id, planet.Tick);

            entityArray[childSlot] = new Entity
            {
                Id = Guid.NewGuid(),
                Type = EntityType.Herbivore,
                Flags = ComponentFlags.Transform | ComponentFlags.Movement | ComponentFlags.Metabolism | ComponentFlags.Diet | ComponentFlags.Reproduction,
                IsAlive = true
            };

            transformArray[childSlot] = new Transform(childPos);
            movementArray[childSlot] = movements[i];
            metabolismArray[childSlot] = metabolisms[i];
            dietArray[childSlot] = diets[i];
            reproductionArray[childSlot] = reproductions[i];
            reproductionArray[childSlot].Cooldown = 0f;

            metabolisms[i].Energy -= reproductions[i].ReproductionCost;
            if (metabolisms[i].Energy < 0f) { metabolisms[i].Energy = 0f; }
            reproductions[i].Cooldown = 0f;
        }
    }

    private static Vector2 SpawnNearby(Vector2 position, Vector2 worldSize, Guid seedGuid, int tick)
    {
        int hash = seedGuid.GetHashCode() ^ (tick * 997);
        float angle = ((hash & 0xFFFF) / 65535f) * (MathF.PI * 2f);
        float radius = 1.5f;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        Vector2 childPos = position + offset;
        return WrapPosition(childPos, worldSize);
    }

    private static Vector2 WrapPosition(Vector2 pos, Vector2 worldSize)
    {
        float x = pos.X % worldSize.X;
        float y = pos.Y % worldSize.Y;

        if (x < 0) { x += worldSize.X; }
        if (y < 0) { y += worldSize.Y; }

        return new Vector2(x, y);
    }
}
