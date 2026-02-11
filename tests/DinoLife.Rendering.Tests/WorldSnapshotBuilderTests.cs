using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Rendering.Tests;

public sealed class WorldSnapshotBuilderTests
{
    [Fact]
    public void Build_ShouldAggregatePopulationEnergyLifespanAndHealthBuckets()
    {
        Planet planet = new Planet();

        int herbivore = planet.AllocateEntitySlot();
        planet.Entities[herbivore] = new Entity(EntityType.Herbivore, ComponentFlags.Metabolism | ComponentFlags.Lifespan) { IsAlive = true };
        planet.Transforms[herbivore] = new Transform { Position = new Vector2(10f, 10f) };
        planet.Metabolisms[herbivore] = new Metabolism { Energy = 10f, MaxEnergy = 100f };
        planet.Lifespans[herbivore] = new Lifespan { Age = 10f, MaxAge = 100f };

        int carnivore = planet.AllocateEntitySlot();
        planet.Entities[carnivore] = new Entity(EntityType.Carnivore, ComponentFlags.Metabolism | ComponentFlags.Lifespan) { IsAlive = true };
        planet.Transforms[carnivore] = new Transform { Position = new Vector2(20f, 20f) };
        planet.Metabolisms[carnivore] = new Metabolism { Energy = 50f, MaxEnergy = 100f };
        planet.Lifespans[carnivore] = new Lifespan { Age = 20f, MaxAge = 100f };

        int scavenger = planet.AllocateEntitySlot();
        planet.Entities[scavenger] = new Entity(EntityType.Scavenger, ComponentFlags.Metabolism) { IsAlive = true };
        planet.Transforms[scavenger] = new Transform { Position = new Vector2(30f, 30f) };
        planet.Metabolisms[scavenger] = new Metabolism { Energy = 90f, MaxEnergy = 100f };

        int plant = planet.AllocateEntitySlot();
        planet.Entities[plant] = new Entity(EntityType.Plant, ComponentFlags.Plant) { IsAlive = true };
        planet.Transforms[plant] = new Transform { Position = new Vector2(40f, 40f) };
        planet.Plants[plant] = new Plant { IsActive = true, Energy = 30f, MaxEnergy = 50f };

        planet.AddCorpse(new Vector2(50f, 50f), energy: 5f);

        var snapshot = WorldSnapshotBuilder.Build(planet);

        snapshot.Stats.Herbivores.Should().Be(1);
        snapshot.Stats.Carnivores.Should().Be(1);
        snapshot.Stats.Scavengers.Should().Be(1);
        snapshot.Stats.Plants.Should().Be(1);
        snapshot.Stats.TotalEnergy.Should().BeApproximately(185f, 0.001f);
        snapshot.Stats.AverageLifespan.Should().BeApproximately(15f, 0.001f);
        snapshot.Stats.HealthLow.Should().Be(1);
        snapshot.Stats.HealthMedium.Should().Be(1);
        snapshot.Stats.HealthHigh.Should().Be(1);
    }
}
