using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class HerbivoreEntityTests
{
    [Fact]
    public void Herbivore_MovesTowardsNearestPlant_WhenHungry()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int herbivore = CreateHerbivore(world, new Vector2(10f, 10f), energy: 10f, threshold: 50f);
        CreatePlant(world, new Vector2(20f, 10f));

        var behavior = new BehaviorSystem();
        behavior.Update(world, 1.0 / 60.0);

        world.Movements[herbivore].Velocity.X.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void Herbivore_Reproduces_WhenEnergyAboveThreshold_AndCooldownReady()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int parent = CreateHerbivore(world, new Vector2(10f, 10f), energy: 100f, threshold: 80f);
        world.Reproductions[parent].Cooldown = world.Reproductions[parent].CooldownDuration;

        int beforeCount = world.EntityCount;
        var reproduction = new ReproductionSystem();
        reproduction.Update(world, 1.0 / 60.0);

        world.EntityCount.Should().Be(beforeCount + 1);
        world.Metabolisms[parent].Energy.Should().BeLessThan(100f);
        world.Entities[beforeCount].Type.Should().Be(EntityType.Herbivore);
        world.Entities[beforeCount].IsAlive.Should().BeTrue();
    }

    [Fact]
    public void Herbivore_DoesNotReproduce_WhenEnergyBelowThreshold()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int parent = CreateHerbivore(world, new Vector2(10f, 10f), energy: 20f, threshold: 80f);
        world.Reproductions[parent].Cooldown = world.Reproductions[parent].CooldownDuration;

        int beforeCount = world.EntityCount;
        var reproduction = new ReproductionSystem();
        reproduction.Update(world, 1.0 / 60.0);

        world.EntityCount.Should().Be(beforeCount);
    }

    private static int CreateHerbivore(Planet world, Vector2 position, float energy, float threshold)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Herbivore,
            Flags = ComponentFlags.Transform | ComponentFlags.Movement | ComponentFlags.Metabolism | ComponentFlags.Diet | ComponentFlags.Reproduction,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);
        world.Movements[slot] = new Movement
        {
            Velocity = Vector2.Zero,
            Speed = 2.5f,
            Acceleration = 5f
        };
        world.Metabolisms[slot] = new Metabolism
        {
            Energy = energy,
            MaxEnergy = 100f,
            HungerRate = 1.0f,
            EnergyGainRate = 1.0f
        };
        world.Diets[slot] = new Diet
        {
            FoodType = FoodType.Plant,
            DetectionRadius = 30f,
            EatRadius = 2f,
            EatingDuration = 1f
        };
        world.Reproductions[slot] = new Reproduction
        {
            ReproductionThreshold = threshold,
            ReproductionCost = 20f,
            Cooldown = 0f,
            CooldownDuration = 10f
        };

        return slot;
    }

    private static int CreatePlant(Planet world, Vector2 position)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Plant,
            Flags = ComponentFlags.Transform | ComponentFlags.Plant,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);
        world.Plants[slot] = new Plant
        {
            Energy = 20f,
            MaxEnergy = 50f,
            GrowthRate = 0.5f,
            RespawnTime = 10f,
            RespawnTimer = 0f,
            IsActive = true
        };

        return slot;
    }
}
