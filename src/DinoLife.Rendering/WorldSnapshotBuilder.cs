using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Rendering;

/// <summary>
/// Builds render snapshots from the simulation world.
/// </summary>
public static class WorldSnapshotBuilder
{
    public static WorldSnapshot Build(Planet planet)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);
        Span<DinoLife.Core.Components.Plant> plantComponents = planet.Plants.AsSpan(0, planet.EntityCount);

        SnapshotEntity[] snapshotEntities = new SnapshotEntity[entities.Length];
        SnapshotCorpse[] snapshotCorpses = new SnapshotCorpse[planet.Corpses.Count];

        int herbivores = 0;
        int carnivores = 0;
        int plants = 0;
        int scavengers = 0;
        float totalEnergy = 0f;
        float totalAge = 0f;
        int lifespanCount = 0;
        int healthLow = 0;
        int healthMedium = 0;
        int healthHigh = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            Vector2 position = transforms[i].Position;
            bool isAlive = entity.IsAlive;
            if (entity.Type == EntityType.Plant && entity.Has(ComponentFlags.Plant) && !plantComponents[i].IsActive)
            {
                isAlive = false;
            }

            snapshotEntities[i] = new SnapshotEntity(entity.Id, entity.Type, position, isAlive);

            if (!isAlive) { continue; }

            switch (entity.Type)
            {
                case EntityType.Herbivore:
                    herbivores++;
                    break;
                case EntityType.Carnivore:
                    carnivores++;
                    break;
                case EntityType.Plant:
                    plants++;
                    break;
                case EntityType.Scavenger:
                    scavengers++;
                    break;
            }

            if (entity.Has(ComponentFlags.Metabolism))
            {
                Metabolism metabolism = planet.Metabolisms[i];
                totalEnergy += metabolism.Energy;

                float healthRatio = metabolism.MaxEnergy > 0f
                    ? metabolism.Energy / metabolism.MaxEnergy
                    : 0f;
                if (healthRatio < 0.33f) { healthLow++; }
                else if (healthRatio < 0.66f) { healthMedium++; }
                else { healthHigh++; }
            }
            else if (entity.Has(ComponentFlags.Plant))
            {
                totalEnergy += plantComponents[i].Energy;
            }

            if (entity.Has(ComponentFlags.Lifespan))
            {
                totalAge += planet.Lifespans[i].Age;
                lifespanCount++;
            }
        }

        for (int i = 0; i < planet.Corpses.Count; i++)
        {
            totalEnergy += planet.Corpses[i].Energy;
        }

        for (int i = 0; i < planet.Corpses.Count; i++)
        {
            snapshotCorpses[i] = new SnapshotCorpse(planet.Corpses[i].Position);
        }

        float averageLifespan = lifespanCount > 0 ? totalAge / lifespanCount : 0f;

        return new WorldSnapshot
        {
            Tick = planet.Tick,
            WorldSize = planet.WorldSize,
            GridCellSize = planet.SpatialGrid.CellSize,
            ShowGrid = planet.DebugDrawGrid,
            Entities = snapshotEntities,
            Corpses = snapshotCorpses,
            Stats = new SnapshotStats(
                herbivores,
                carnivores,
                plants,
                scavengers,
                totalEnergy,
                averageLifespan,
                healthLow,
                healthMedium,
                healthHigh)
        };
    }
}
