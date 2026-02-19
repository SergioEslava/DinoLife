using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.World;

namespace DinoLife.Cli.Tuning;

internal static class WorldTuningApplier
{
    public static void Apply(Planet planet, SimulationTuningProfile profile)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Movement> movements = planet.Movements.AsSpan(0, planet.EntityCount);
        Span<Metabolism> metabolisms = planet.Metabolisms.AsSpan(0, planet.EntityCount);
        Span<Reproduction> reproductions = planet.Reproductions.AsSpan(0, planet.EntityCount);
        Span<Diet> diets = planet.Diets.AsSpan(0, planet.EntityCount);
        Span<Plant> plants = planet.Plants.AsSpan(0, planet.EntityCount);

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }

            switch (entities[i].Type)
            {
                case EntityType.Herbivore:
                    ApplyMovement(ref movements[i], profile.HerbivoreSpeed, entities[i].Has(ComponentFlags.Movement));
                    ApplyMetabolism(ref metabolisms[i], profile.HerbivoreHungerRate, entities[i].Has(ComponentFlags.Metabolism));
                    ApplyReproduction(ref reproductions[i], profile.HerbivoreReproductionThreshold, entities[i].Has(ComponentFlags.Reproduction));
                    ApplyDetection(ref diets[i], profile.HerbivoreDetectionRadius, entities[i].Has(ComponentFlags.Diet));
                    break;
                case EntityType.Carnivore:
                    ApplyMovement(ref movements[i], profile.CarnivoreSpeed, entities[i].Has(ComponentFlags.Movement));
                    ApplyMetabolism(ref metabolisms[i], profile.CarnivoreHungerRate, entities[i].Has(ComponentFlags.Metabolism));
                    ApplyReproduction(ref reproductions[i], profile.CarnivoreReproductionThreshold, entities[i].Has(ComponentFlags.Reproduction));
                    ApplyDetection(ref diets[i], profile.CarnivoreDetectionRadius, entities[i].Has(ComponentFlags.Diet));
                    break;
                case EntityType.Scavenger:
                    ApplyMovement(ref movements[i], profile.ScavengerSpeed, entities[i].Has(ComponentFlags.Movement));
                    ApplyMetabolism(ref metabolisms[i], profile.ScavengerHungerRate, entities[i].Has(ComponentFlags.Metabolism));
                    ApplyReproduction(ref reproductions[i], profile.ScavengerReproductionThreshold, entities[i].Has(ComponentFlags.Reproduction));
                    ApplyDetection(ref diets[i], profile.ScavengerDetectionRadius, entities[i].Has(ComponentFlags.Diet));
                    break;
                case EntityType.Plant:
                    if (!entities[i].Has(ComponentFlags.Plant)) { break; }
                    plants[i].GrowthRate = profile.PlantGrowthRate;
                    plants[i].RespawnTime = profile.PlantRespawnTime;
                    if (plants[i].RespawnTimer > profile.PlantRespawnTime)
                    {
                        plants[i].RespawnTimer = profile.PlantRespawnTime;
                    }

                    break;
            }
        }
    }

    private static void ApplyMovement(ref Movement movement, float speed, bool hasMovement)
    {
        if (!hasMovement) { return; }
        movement.Speed = speed;
        movement.Acceleration = speed * 2f;
    }

    private static void ApplyMetabolism(ref Metabolism metabolism, float hungerRate, bool hasMetabolism)
    {
        if (!hasMetabolism) { return; }
        metabolism.HungerRate = hungerRate;
    }

    private static void ApplyReproduction(ref Reproduction reproduction, float threshold, bool hasReproduction)
    {
        if (!hasReproduction) { return; }
        reproduction.ReproductionThreshold = threshold;
    }

    private static void ApplyDetection(ref Diet diet, float radius, bool hasDiet)
    {
        if (!hasDiet) { return; }
        diet.DetectionRadius = radius;
    }
}
