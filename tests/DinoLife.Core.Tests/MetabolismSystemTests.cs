using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class MetabolismSystemTests
{
    [Fact]
    public void MetabolismSystem_DepletesEnergy_WhenHungerRatePositive()
    {
        var world = new Planet();
        int slot = CreateEntity(world, EntityType.Herbivore, new Vector2(0f, 0f));

        world.Metabolisms[slot].Energy = 10f;
        world.Metabolisms[slot].HungerRate = 2f;

        var system = new MetabolismSystem();
        system.Update(world, 1.0);

        world.Metabolisms[slot].Energy.Should().BeApproximately(8f, 0.001f);
    }

    [Fact]
    public void MetabolismSystem_GainsEnergy_WhenEatingPlantInRange()
    {
        var world = new Planet();
        int herbivore = CreateEntity(world, EntityType.Herbivore, new Vector2(5f, 5f));
        int plant = CreatePlant(world, new Vector2(6f, 5f), energy: 30f);

        world.Metabolisms[herbivore].Energy = 10f;
        world.Metabolisms[herbivore].MaxEnergy = 100f;

        world.Diets[herbivore] = new Diet
        {
            FoodType = FoodType.Plant,
            DetectionRadius = 10f,
            EatRadius = 2f,
            EatingDuration = 1f
        };

        var system = new MetabolismSystem();
        system.Update(world, 1.0 / 60.0);

        world.Metabolisms[herbivore].Energy.Should().BeGreaterThan(10f);
        world.Plants[plant].IsActive.Should().BeFalse();
    }

    [Fact]
    public void MetabolismSystem_StarvesEntity_WhenEnergyDepletes()
    {
        var world = new Planet();
        int slot = CreateEntity(world, EntityType.Herbivore, new Vector2(0f, 0f));

        world.Metabolisms[slot].Energy = 0.5f;
        world.Metabolisms[slot].HungerRate = 1.0f;

        var system = new MetabolismSystem();
        system.Update(world, 1.0);

        world.Entities[slot].IsAlive.Should().BeFalse();
    }

    [Fact]
    public void MetabolismSystem_CapsEnergy_AtMax()
    {
        var world = new Planet();
        int herbivore = CreateEntity(world, EntityType.Herbivore, new Vector2(1f, 1f));
        CreatePlant(world, new Vector2(2f, 1f), energy: 30f);

        world.Metabolisms[herbivore].Energy = 99f;
        world.Metabolisms[herbivore].MaxEnergy = 100f;

        world.Diets[herbivore] = new Diet
        {
            FoodType = FoodType.Plant,
            DetectionRadius = 10f,
            EatRadius = 3f,
            EatingDuration = 1f
        };

        var system = new MetabolismSystem();
        system.Update(world, 1.0 / 60.0);

        world.Metabolisms[herbivore].Energy.Should().Be(100f);
    }

    private static int CreateEntity(
        Planet world,
        EntityType type,
        Vector2 position,
        bool hasDiet = true,
        bool hasMetabolism = true)
    {
        int slot = world.AllocateEntitySlot();

        var flags = ComponentFlags.Transform;
        if (hasMetabolism) { flags |= ComponentFlags.Metabolism; }
        if (hasDiet) { flags |= ComponentFlags.Diet; }

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = type,
            Flags = flags,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);

        if (hasMetabolism)
        {
            world.Metabolisms[slot] = new Metabolism
            {
                Energy = 50f,
                MaxEnergy = 100f,
                HungerRate = 1.0f,
                EnergyGainRate = 1.0f
            };
        }

        return slot;
    }

    private static int CreatePlant(Planet world, Vector2 position, float energy)
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
            Energy = energy,
            MaxEnergy = energy,
            GrowthRate = 0f,
            RespawnTime = 10f,
            RespawnTimer = 0f,
            IsActive = true
        };

        return slot;
    }
}
