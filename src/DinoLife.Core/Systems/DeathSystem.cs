using System;
using DinoLife.Core.World;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;

namespace DinoLife.Core.Systems;

/// <summary>
/// Removes or marks entities that meet death conditions (age, starvation, etc.).
/// </summary>
public class DeathSystem : ISystem
{
    /// <summary>
    /// Evaluate and process deaths on the provided <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);
        Span<Metabolism> metabolisms = planet.Metabolisms.AsSpan(0, planet.EntityCount);
        Span<Lifespan> lifespans = planet.Lifespans.AsSpan(0, planet.EntityCount);

        float deltaTime = (float)deltatime;

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive)
            {
                if (entities[i].Id == Guid.Empty) { continue; }
                planet.FreeEntitySlot(i);
                continue;
            }

            bool shouldDie = false;

            if (entities[i].Has(ComponentFlags.Lifespan))
            {
                lifespans[i].Age += deltaTime;
                if (lifespans[i].IsDead) { shouldDie = true; }
            }

            if (entities[i].Has(ComponentFlags.Metabolism) && metabolisms[i].Energy <= 0f)
            {
                shouldDie = true;
            }

            if (!shouldDie) { continue; }

            if (entities[i].Type != EntityType.Plant && entities[i].Has(ComponentFlags.Transform))
            {
                float corpseEnergy = 0f;
                if (entities[i].Has(ComponentFlags.Metabolism))
                {
                    corpseEnergy = metabolisms[i].Energy * CorpseStats.EnergyRetention;
                }

                planet.AddCorpse(transforms[i].Position, corpseEnergy);
            }

            entities[i].Kill();
            planet.FreeEntitySlot(i);
        }

        DecayCorpses(planet, deltaTime);
    }

    private static void DecayCorpses(Planet planet, float deltaTime)
    {
        if (planet.Corpses.Count == 0) { return; }

        for (int i = planet.Corpses.Count - 1; i >= 0; i--)
        {
            Corpse corpse = planet.Corpses[i];
            corpse.DecayTimer -= deltaTime;
            if (corpse.DecayTimer <= 0f)
            {
                planet.Corpses.RemoveAt(i);
                continue;
            }

            planet.Corpses[i] = corpse;
        }
    }
}
