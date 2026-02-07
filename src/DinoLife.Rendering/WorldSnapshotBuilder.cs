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

        SnapshotEntity[] snapshotEntities = new SnapshotEntity[entities.Length];

        int herbivores = 0;
        int carnivores = 0;
        int plants = 0;
        int scavengers = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            Vector2 position = transforms[i].Position;
            snapshotEntities[i] = new SnapshotEntity(entity.Type, position, entity.IsAlive);

            if (!entity.IsAlive) { continue; }

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
        }

        return new WorldSnapshot
        {
            Tick = planet.Tick,
            WorldSize = planet.WorldSize,
            Entities = snapshotEntities,
            Stats = new SnapshotStats(herbivores, carnivores, plants, scavengers)
        };
    }
}
