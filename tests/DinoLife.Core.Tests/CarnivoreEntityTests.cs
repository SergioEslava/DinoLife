using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class CarnivoreEntityTests
{
    [Fact]
    public void Carnivore_KillsHerbivore_WhenInCatchRadius()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int carnivore = CreateCarnivore(world, new Vector2(10f, 10f), energy: 10f);
        int herbivore = CreateHerbivore(world, new Vector2(11f, 10f));

        var hunting = new HuntingSystem();
        hunting.Update(world, 1.0 / 60.0);

        world.Entities[herbivore].IsAlive.Should().BeFalse();
        world.Metabolisms[carnivore].Energy.Should().BeGreaterThan(10f);
    }

    [Fact]
    public void Carnivore_Reproduces_WhenEnergyAboveThreshold_AndCooldownReady()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int parent = CreateCarnivore(world, new Vector2(10f, 10f), energy: 150f);
        world.Reproductions[parent].Cooldown = world.Reproductions[parent].CooldownDuration;

        int before = world.EntityCount;
        var reproduction = new ReproductionSystem();
        reproduction.Update(world, 1.0 / 60.0);

        world.EntityCount.Should().Be(before + 1);
        world.Entities[before].Type.Should().Be(EntityType.Carnivore);
    }

    private static int CreateCarnivore(Planet world, Vector2 position, float energy)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Carnivore,
            Flags = ComponentFlags.Transform | ComponentFlags.Movement | ComponentFlags.Metabolism | ComponentFlags.Diet | ComponentFlags.Reproduction,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);
        world.Movements[slot] = new Movement
        {
            Velocity = Vector2.Zero,
            Speed = 3.5f,
            Acceleration = 6f
        };
        world.Metabolisms[slot] = new Metabolism
        {
            Energy = energy,
            MaxEnergy = 150f,
            HungerRate = 1.5f,
            EnergyGainRate = 1.0f
        };
        world.Diets[slot] = new Diet
        {
            FoodType = FoodType.Herbivore,
            DetectionRadius = 20f,
            EatRadius = 2f,
            EatingDuration = 1f
        };
        world.Reproductions[slot] = new Reproduction
        {
            ReproductionThreshold = 120f,
            ReproductionCost = 50f,
            Cooldown = 0f,
            CooldownDuration = 30f
        };

        return slot;
    }

    private static int CreateHerbivore(Planet world, Vector2 position)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Herbivore,
            Flags = ComponentFlags.Transform | ComponentFlags.Movement | ComponentFlags.Metabolism | ComponentFlags.Diet,
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
            Energy = 50f,
            MaxEnergy = 100f,
            HungerRate = 1.0f,
            EnergyGainRate = 1.0f
        };
        world.Diets[slot] = new Diet
        {
            FoodType = FoodType.Plant,
            DetectionRadius = 20f,
            EatRadius = 2f,
            EatingDuration = 1f
        };

        return slot;
    }
}
