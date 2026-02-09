using DinoLife.Core.Entities;
using DinoLife.Core.World;
using DinoLife.Core.Components;

namespace DinoLife.Core.Systems;

/// <summary>
/// Rebuilds the spatial grid from current entity positions.
/// </summary>
public class SpatialGridSystem : ISystem
{
    public void Update(Planet planet, double deltatime)
    {
        SpatialGrid grid = planet.SpatialGrid;
        grid.Clear();

        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }
            grid.Insert(i, transforms[i].Position);
        }
    }
}
