using System.Collections.Generic;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Handles energy consumption/recovery and starvation mechanics.
/// </summary>
public class MetabolismSystem : ISystem
{
    private const float PlantEnergy = 25f;
    private const float CorpseEnergy = 20f;

    /// <summary>
    /// Update metabolisms for entities on the provided <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);
        Span<Metabolism> metabolisms = planet.Metabolisms.AsSpan(0, planet.EntityCount);
        Span<Diet> diets = planet.Diets.AsSpan(0, planet.EntityCount);
        Span<Plant> plants = planet.Plants.AsSpan(0, planet.EntityCount);
        var candidates = new List<int>(32);

        float deltaTime = (float)deltatime;

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Metabolism)) { continue; }

            // Deplete energy over time
            metabolisms[i].Energy -= metabolisms[i].HungerRate * deltaTime;
            if (metabolisms[i].Energy < 0f) { metabolisms[i].Energy = 0f; }

            // Eat if possible
            if (entities[i].Has(ComponentFlags.Diet) && entities[i].Has(ComponentFlags.Transform))
            {
                if (diets[i].FoodType == FoodType.Corpse)
                {
                    // Scavengers consume corpses directly (not entities).
                    if (TryFindCorpseTarget(
                            i,
                            transforms,
                            diets,
                            planet.Corpses,
                            out int corpseIndex))
                    {
                        float gain = planet.Corpses[corpseIndex].Energy;
                        metabolisms[i].Energy += gain * metabolisms[i].EnergyGainRate;
                        if (metabolisms[i].Energy > metabolisms[i].MaxEnergy)
                        {
                            metabolisms[i].Energy = metabolisms[i].MaxEnergy;
                        }

                        planet.Corpses.RemoveAt(corpseIndex);
                    }
                }
                else if (TryFindFoodTarget(
                             i,
                             entities,
                             transforms,
                             diets,
                             plants,
                             planet.SpatialGrid,
                             candidates,
                             out int targetIndex))
                {
                    float gain = GetEnergyGain(diets[i].FoodType, targetIndex, entities, plants);
                    metabolisms[i].Energy += gain * metabolisms[i].EnergyGainRate;
                    if (metabolisms[i].Energy > metabolisms[i].MaxEnergy)
                    {
                        metabolisms[i].Energy = metabolisms[i].MaxEnergy;
                    }

                    if (diets[i].FoodType == FoodType.Plant && entities[targetIndex].Has(ComponentFlags.Plant))
                    {
                        plants[targetIndex].IsActive = false;
                        plants[targetIndex].RespawnTimer = plants[targetIndex].RespawnTime;
                        plants[targetIndex].Energy = 0f;
                    }
                    else
                    {
                        entities[targetIndex].Kill();
                    }
                }
            }

        }
    }

    private static float GetEnergyGain(
        FoodType foodType,
        int targetIndex,
        Span<Entity> entities,
        Span<Plant> plants)
        => foodType switch
        {
            FoodType.Plant => GetPlantEnergy(targetIndex, entities, plants),
            FoodType.Herbivore => 0f,
            FoodType.Corpse => CorpseEnergy,
            _ => 0f
        };

    private static float GetPlantEnergy(int targetIndex, Span<Entity> entities, Span<Plant> plants)
    {
        if (!entities[targetIndex].Has(ComponentFlags.Plant)) { return PlantEnergy; }
        return plants[targetIndex].Energy;
    }

    private static bool TryFindFoodTarget(
        int sourceIndex,
        Span<Entity> entities,
        Span<Transform> transforms,
        Span<Diet> diets,
        Span<Plant> plants,
        SpatialGrid grid,
        List<int> candidates,
        out int targetIndex)
    {
        targetIndex = -1;

        FoodType foodType = diets[sourceIndex].FoodType;
        if (foodType == FoodType.None) { return false; }

        float radius = diets[sourceIndex].EatRadius;
        float radiusSq = radius * radius;
        Vector2 sourcePos = transforms[sourceIndex].Position;

        grid.QueryRadius(sourcePos, radius, candidates);

        for (int c = 0; c < candidates.Count; c++)
        {
            int i = candidates[c];
            if (i == sourceIndex) { continue; }
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }

            if (!IsFoodMatch(foodType, entities[i].Type)) { continue; }
            if (foodType == FoodType.Plant && entities[i].Has(ComponentFlags.Plant) && !plants[i].IsActive)
            {
                continue;
            }

            Vector2 delta = transforms[i].Position - sourcePos;
            if (delta.LengthSquared() > radiusSq) { continue; }

            targetIndex = i;
            return true;
        }

        return false;
    }

    private static bool IsFoodMatch(FoodType foodType, EntityType targetType)
    {
        return foodType switch
        {
            FoodType.Plant => targetType == EntityType.Plant,
            FoodType.Herbivore => false,
            FoodType.Corpse => false,
            _ => false
        };
    }

    /// <summary>
    /// Finds a corpse within the entity's eat radius.
    /// </summary>
    private static bool TryFindCorpseTarget(
        int sourceIndex,
        Span<Transform> transforms,
        Span<Diet> diets,
        List<Corpse> corpses,
        out int corpseIndex)
    {
        corpseIndex = -1;
        if (corpses.Count == 0) { return false; }

        float radius = diets[sourceIndex].EatRadius;
        float radiusSq = radius * radius;
        Vector2 sourcePos = transforms[sourceIndex].Position;

        for (int i = 0; i < corpses.Count; i++)
        {
            Vector2 delta = corpses[i].Position - sourcePos;
            if (delta.LengthSquared() > radiusSq) { continue; }

            corpseIndex = i;
            return true;
        }

        return false;
    }
}
