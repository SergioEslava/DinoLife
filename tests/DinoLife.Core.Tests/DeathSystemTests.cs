using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class DeathSystemTests
{
    [Fact]
    public void DeathSystem_KillsEntity_WhenStarving()
    {
        var world = new Planet();
        int slot = CreateEntity(world, EntityType.Herbivore, new Vector2(1f, 1f));

        world.Metabolisms[slot].Energy = 0f;

        var system = new DeathSystem();
        system.Update(world, 1.0);

        world.Entities[slot].IsAlive.Should().BeFalse();
        world.Corpses.Should().HaveCount(1);
    }

    [Fact]
    public void DeathSystem_KillsEntity_WhenOldAgeReached()
    {
        var world = new Planet();
        int slot = CreateEntity(world, EntityType.Herbivore, new Vector2(2f, 2f), maxAge: 2f);

        var system = new DeathSystem();
        system.Update(world, 2.5);

        world.Entities[slot].IsAlive.Should().BeFalse();
        world.Corpses.Should().HaveCount(1);
    }

    [Fact]
    public void DeathSystem_DecaysCorpses_OverTime()
    {
        var world = new Planet();
        world.AddCorpse(new Vector2(3f, 3f), energy: 10f);
        world.Corpses[0] = new Corpse
        {
            Id = world.Corpses[0].Id,
            Position = world.Corpses[0].Position,
            Energy = world.Corpses[0].Energy,
            DecayTimer = 0.5f
        };

        var system = new DeathSystem();
        system.Update(world, 1.0);

        world.Corpses.Should().BeEmpty();
    }

    private static int CreateEntity(Planet world, EntityType type, Vector2 position, float maxAge = 5f)
    {
        int slot = world.AllocateEntitySlot();

        world.Entities[slot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = type,
            Flags = ComponentFlags.Transform | ComponentFlags.Metabolism | ComponentFlags.Lifespan,
            IsAlive = true
        };

        world.Transforms[slot] = new Transform(position);
        world.Metabolisms[slot] = new Metabolism
        {
            Energy = 10f,
            MaxEnergy = 100f,
            HungerRate = 1.0f,
            EnergyGainRate = 1.0f
        };
        world.Lifespans[slot] = new Lifespan
        {
            Age = 0f,
            MaxAge = maxAge
        };

        return slot;
    }
}
