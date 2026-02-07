using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Systems;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Systems;

public class PlantGrowthSystemTests
{
    [Fact]
    public void PlantGrowthSystem_IncreasesEnergy_OverTime()
    {
        var world = new Planet();
        int plant = CreatePlant(world, new Vector2(5f, 5f), energy: 0f, maxEnergy: 10f, growthRate: 2f);

        var system = new PlantGrowthSystem();
        system.Update(world, 1.0);

        world.Plants[plant].Energy.Should().BeApproximately(2f, 0.001f);
    }

    [Fact]
    public void PlantGrowthSystem_CapsEnergy_AtMax()
    {
        var world = new Planet();
        int plant = CreatePlant(world, new Vector2(5f, 5f), energy: 9f, maxEnergy: 10f, growthRate: 5f);

        var system = new PlantGrowthSystem();
        system.Update(world, 1.0);

        world.Plants[plant].Energy.Should().Be(10f);
    }

    [Fact]
    public void PlantGrowthSystem_Respawns_WhenTimerExpires()
    {
        var world = new Planet();
        int plant = CreatePlant(world, new Vector2(5f, 5f), energy: 0f, maxEnergy: 20f, growthRate: 1f);
        world.Plants[plant].IsActive = false;
        world.Plants[plant].RespawnTimer = 0.5f;
        world.Plants[plant].RespawnTime = 0.5f;

        var system = new PlantGrowthSystem();
        system.Update(world, 1.0);

        world.Plants[plant].IsActive.Should().BeTrue();
        world.Plants[plant].Energy.Should().BeApproximately(5f, 0.001f);
    }

    private static int CreatePlant(
        Planet world,
        Vector2 position,
        float energy,
        float maxEnergy,
        float growthRate)
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
            MaxEnergy = maxEnergy,
            GrowthRate = growthRate,
            RespawnTime = 1f,
            RespawnTimer = 0f,
            IsActive = true
        };

        return slot;
    }
}
