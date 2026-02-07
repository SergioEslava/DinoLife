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
    private const float HerbivoreEnergy = 40f;
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
                if (TryFindFoodTarget(
                        i,
                        entities,
                        transforms,
                        diets,
                        out int targetIndex))
                {
                    float gain = GetEnergyGain(diets[i].FoodType);
                    metabolisms[i].Energy += gain * metabolisms[i].EnergyGainRate;
                    if (metabolisms[i].Energy > metabolisms[i].MaxEnergy)
                    {
                        metabolisms[i].Energy = metabolisms[i].MaxEnergy;
                    }

                    entities[targetIndex].Kill();
                }
            }

            // Starvation
            if (metabolisms[i].Energy <= 0f)
            {
                entities[i].Kill();
            }
        }
    }

    private static float GetEnergyGain(FoodType foodType)
        => foodType switch
        {
            FoodType.Plant => PlantEnergy,
            FoodType.Herbivore => HerbivoreEnergy,
            FoodType.Corpse => CorpseEnergy,
            _ => 0f
        };

    private static bool TryFindFoodTarget(
        int sourceIndex,
        Span<Entity> entities,
        Span<Transform> transforms,
        Span<Diet> diets,
        out int targetIndex)
    {
        targetIndex = -1;

        FoodType foodType = diets[sourceIndex].FoodType;
        if (foodType == FoodType.None) { return false; }

        float radius = diets[sourceIndex].EatRadius;
        float radiusSq = radius * radius;
        Vector2 sourcePos = transforms[sourceIndex].Position;

        for (int i = 0; i < entities.Length; i++)
        {
            if (i == sourceIndex) { continue; }
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }

            if (!IsFoodMatch(foodType, entities[i].Type)) { continue; }

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
            FoodType.Herbivore => targetType == EntityType.Herbivore,
            FoodType.Corpse => false,
            _ => false
        };
    }
}
