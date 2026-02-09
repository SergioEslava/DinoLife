using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class ScavengerEntityTests
{
    [Fact]
    public void Scavenger_MovesTowardsNearestCorpse_WhenDetected()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int scavenger = CreateScavenger(world, new Vector2(10f, 10f), energy: 20f);

        world.AddCorpse(new Vector2(20f, 10f), energy: 15f);

        var behavior = new BehaviorSystem();
        behavior.Update(world, 1.0 / 60.0);

        world.Movements[scavenger].Velocity.X.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void Scavenger_EatsCorpse_WhenInRange()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int scavenger = CreateScavenger(world, new Vector2(10f, 10f), energy: 10f);

        world.AddCorpse(new Vector2(11f, 10f), energy: 20f);

        var metabolism = new MetabolismSystem();
        metabolism.Update(world, 1.0 / 60.0);

        world.Corpses.Should().BeEmpty();
        world.Metabolisms[scavenger].Energy.Should().BeGreaterThan(10f);
    }

    [Fact]
    public void Scavenger_Reproduces_WhenEnergyAboveThreshold_AndCooldownReady()
    {
        var world = new Planet { WorldSize = new Vector2(100f, 100f) };
        int parent = CreateScavenger(world, new Vector2(10f, 10f), energy: 90f);
        world.Reproductions[parent].Cooldown = world.Reproductions[parent].CooldownDuration;

        int before = world.EntityCount;
        var reproduction = new ReproductionSystem();
        reproduction.Update(world, 1.0 / 60.0);

        world.EntityCount.Should().Be(before + 1);
        world.Entities[before].Type.Should().Be(EntityType.Scavenger);
    }

    private static int CreateScavenger(Planet world, Vector2 position, float energy)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Scavenger,
            Flags = ComponentFlags.Transform | ComponentFlags.Movement | ComponentFlags.Metabolism | ComponentFlags.Diet | ComponentFlags.Reproduction,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);
        world.Movements[slot] = new Movement
        {
            Velocity = Vector2.Zero,
            Speed = 2.0f,
            Acceleration = 4f
        };
        world.Metabolisms[slot] = new Metabolism
        {
            Energy = energy,
            MaxEnergy = 80f,
            HungerRate = 0.8f,
            EnergyGainRate = 1.0f
        };
        world.Diets[slot] = new Diet
        {
            FoodType = FoodType.Corpse,
            DetectionRadius = 25f,
            EatRadius = 2f,
            EatingDuration = 1f
        };
        world.Reproductions[slot] = new Reproduction
        {
            ReproductionThreshold = 60f,
            ReproductionCost = 30f,
            Cooldown = 0f,
            CooldownDuration = 25f
        };

        return slot;
    }
}
