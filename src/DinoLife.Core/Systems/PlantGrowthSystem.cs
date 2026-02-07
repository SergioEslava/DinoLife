using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Handles plant growth and respawn timers.
/// </summary>
public class PlantGrowthSystem : ISystem
{
    /// <summary>
    /// Update plant growth on the provided <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Plant> plants = planet.Plants.AsSpan(0, planet.EntityCount);
        float deltaTime = (float)deltatime;

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Plant)) { continue; }

            if (!plants[i].IsActive)
            {
                plants[i].RespawnTimer -= deltaTime;
                if (plants[i].RespawnTimer <= 0f)
                {
                    plants[i].IsActive = true;
                    plants[i].RespawnTimer = 0f;
                    plants[i].Energy = plants[i].MaxEnergy * 0.25f;
                }

                continue;
            }

            plants[i].Energy += plants[i].GrowthRate * deltaTime;
            if (plants[i].Energy > plants[i].MaxEnergy)
            {
                plants[i].Energy = plants[i].MaxEnergy;
            }
        }
    }
}
