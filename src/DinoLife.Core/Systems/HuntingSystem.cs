using System.Collections.Generic;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Handles detection and consumption of prey/food by entities with a diet.
/// </summary>
public class HuntingSystem : ISystem
{
    private const float HerbivoreEnergy = 40f;

    /// <summary>
    /// Execute hunting/detection logic for entities on the <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);
        Span<Metabolism> metabolisms = planet.Metabolisms.AsSpan(0, planet.EntityCount);
        Span<Diet> diets = planet.Diets.AsSpan(0, planet.EntityCount);
        var candidates = new List<int>(16);

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Diet)) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }
            if (!entities[i].Has(ComponentFlags.Metabolism)) { continue; }

            if (diets[i].FoodType != FoodType.Herbivore) { continue; }

            if (TryFindPrey(
                    i,
                    entities,
                    transforms,
                    diets[i].EatRadius,
                    planet.SpatialGrid,
                    candidates,
                    out int preyIndex))
            {
                // Create a corpse before removing the prey to preserve its remaining energy.
                if (entities[preyIndex].Has(ComponentFlags.Transform))
                {
                    float corpseEnergy = 0f;
                    if (entities[preyIndex].Has(ComponentFlags.Metabolism))
                    {
                        corpseEnergy = metabolisms[preyIndex].Energy * CorpseStats.EnergyRetention;
                    }

                    planet.AddCorpse(transforms[preyIndex].Position, corpseEnergy);
                }

                entities[preyIndex].Kill();

                metabolisms[i].Energy += HerbivoreEnergy * metabolisms[i].EnergyGainRate;
                if (metabolisms[i].Energy > metabolisms[i].MaxEnergy)
                {
                    metabolisms[i].Energy = metabolisms[i].MaxEnergy;
                }
            }
        }
    }

    private static bool TryFindPrey(
        int hunterIndex,
        Span<Entity> entities,
        Span<Transform> transforms,
        float radius,
        SpatialGrid grid,
        List<int> candidates,
        out int preyIndex)
    {
        preyIndex = -1;
        float radiusSq = radius * radius;
        Vector2 hunterPos = transforms[hunterIndex].Position;

        grid.QueryRadius(hunterPos, radius, candidates);

        for (int c = 0; c < candidates.Count; c++)
        {
            int i = candidates[c];
            if (i == hunterIndex) { continue; }
            if (!entities[i].IsAlive) { continue; }
            if (entities[i].Type != EntityType.Herbivore) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }

            Vector2 delta = transforms[i].Position - hunterPos;
            if (delta.LengthSquared() > radiusSq) { continue; }

            preyIndex = i;
            return true;
        }

        return false;
    }
}
