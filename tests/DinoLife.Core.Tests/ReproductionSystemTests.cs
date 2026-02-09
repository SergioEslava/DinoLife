using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class ReproductionSystemTests
{
    [Fact]
    public void ReproductionSystem_IncreasesPopulation_WhenEnergyHighAndCooldownZero()
    {
        var world = new Planet();
        CreateParent(world, new Vector2(5f, 5f));

        int before = world.EntityCount;
        var system = new ReproductionSystem();

        system.Update(world, 1.0 / 60.0);

        world.EntityCount.Should().Be(before + 1);
    }

    [Fact]
    public void ReproductionSystem_VariesChildStats_WithinTenPercent()
    {
        var world = new Planet();
        int parent = CreateParent(world, new Vector2(10f, 10f), speed: 10f, hungerRate: 2f, maxAge: 100f);
        world.Reproductions[parent].Cooldown = world.Reproductions[parent].CooldownDuration;

        int before = world.EntityCount;
        var system = new ReproductionSystem();
        system.Update(world, 1.0 / 60.0);

        int child = before;
        world.Movements[child].Speed.Should().BeInRange(9f, 11f);
        world.Metabolisms[child].HungerRate.Should().BeInRange(1.8f, 2.2f);
        world.Lifespans[child].MaxAge.Should().BeInRange(90f, 110f);
    }

    private static int CreateParent(
        Planet world,
        Vector2 position,
        float speed = 3f,
        float hungerRate = 1f,
        float maxAge = 50f)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Herbivore,
            Flags = ComponentFlags.Transform
                | ComponentFlags.Movement
                | ComponentFlags.Metabolism
                | ComponentFlags.Diet
                | ComponentFlags.Reproduction
                | ComponentFlags.Lifespan,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);
        world.Movements[slot] = new Movement
        {
            Velocity = Vector2.Zero,
            Speed = speed,
            Acceleration = speed * 2f
        };
        world.Metabolisms[slot] = new Metabolism
        {
            Energy = 100f,
            MaxEnergy = 200f,
            HungerRate = hungerRate,
            EnergyGainRate = 1.0f
        };
        world.Diets[slot] = new Diet
        {
            FoodType = FoodType.Plant,
            DetectionRadius = 20f,
            EatRadius = 2f,
            EatingDuration = 1f
        };
        world.Reproductions[slot] = new Reproduction
        {
            ReproductionThreshold = 10f,
            ReproductionCost = 1f,
            Cooldown = 0f,
            CooldownDuration = 0f
        };
        world.Lifespans[slot] = new Lifespan
        {
            Age = 0f,
            MaxAge = maxAge
        };

        return slot;
    }
}
