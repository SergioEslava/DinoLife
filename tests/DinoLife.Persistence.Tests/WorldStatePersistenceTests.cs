using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using DinoLife.Persistence;
using FluentAssertions;
using Xunit;

namespace DinoLife.Persistence.Tests;

public sealed class WorldStatePersistenceTests
{
    [Fact]
    public void SaveAndLoad_Roundtrip_ShouldPreserveRenderableEntityData()
    {
        string path = Path.GetTempFileName();
        try
        {
            Planet source = BuildSampleWorld();
            source.Tick = 42;
            source.DebugDrawGrid = true;
            source.AddCorpse(new Vector2(8f, 9f), energy: 12.5f);

            WorldStatePersistence.Save(path, source);

            bool loadedOk = WorldStatePersistence.TryLoad(path, out Planet loaded, out string error);

            loadedOk.Should().BeTrue(error);
            loaded.Tick.Should().Be(42);
            loaded.DebugDrawGrid.Should().BeTrue();
            loaded.WorldSize.Should().Be(source.WorldSize);
            loaded.EntityCount.Should().Be(source.EntityCount);
            loaded.Corpses.Should().HaveCount(1);
            loaded.Corpses[0].Position.Should().Be(new Vector2(8f, 9f));
            loaded.Corpses[0].Energy.Should().BeApproximately(12.5f, 0.001f);

            loaded.Entities[0].IsAlive.Should().BeTrue();
            loaded.Entities[0].Type.Should().Be(EntityType.Herbivore);
            loaded.Transforms[0].Position.Should().Be(new Vector2(2f, 3f));
            loaded.Metabolisms[0].Energy.Should().BeApproximately(77f, 0.001f);
            loaded.Metabolisms[0].MaxEnergy.Should().BeApproximately(100f, 0.001f);
            loaded.Lifespans[0].Age.Should().BeApproximately(12f, 0.001f);
            loaded.Lifespans[0].MaxAge.Should().BeApproximately(300f, 0.001f);

            loaded.Entities[1].IsAlive.Should().BeTrue();
            loaded.Entities[1].Type.Should().Be(EntityType.Plant);
            loaded.Plants[1].IsActive.Should().BeTrue();
            loaded.Plants[1].Energy.Should().BeApproximately(20f, 0.001f);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void TryLoad_ShouldReturnFalse_ForMissingFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"dino-life-missing-{Guid.NewGuid():N}.json");

        bool loadedOk = WorldStatePersistence.TryLoad(path, out Planet loaded, out string error);

        loadedOk.Should().BeFalse();
        error.Should().Be("No save file found");
        loaded.Should().BeNull();
    }

    private static Planet BuildSampleWorld()
    {
        Planet planet = new Planet
        {
            WorldSize = new Vector2(120f, 40f)
        };

        int herbivoreSlot = planet.AllocateEntitySlot();
        planet.Entities[herbivoreSlot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Herbivore,
            Flags = ComponentFlags.Transform | ComponentFlags.Metabolism | ComponentFlags.Lifespan,
            IsAlive = true
        };
        planet.Transforms[herbivoreSlot] = new Transform(new Vector2(2f, 3f));
        planet.Metabolisms[herbivoreSlot] = new Metabolism
        {
            Energy = 77f,
            MaxEnergy = 100f,
            HungerRate = 1.2f,
            EnergyGainRate = 1f
        };
        planet.Lifespans[herbivoreSlot] = new Lifespan
        {
            Age = 12f,
            MaxAge = 300f
        };

        int plantSlot = planet.AllocateEntitySlot();
        planet.Entities[plantSlot] = new Entity
        {
            Id = Guid.NewGuid(),
            Type = EntityType.Plant,
            Flags = ComponentFlags.Transform | ComponentFlags.Plant,
            IsAlive = true
        };
        planet.Transforms[plantSlot] = new Transform(new Vector2(5f, 6f));
        planet.Plants[plantSlot] = new Plant
        {
            Energy = 20f,
            MaxEnergy = 50f,
            GrowthRate = 0.5f,
            RespawnTime = 30f,
            RespawnTimer = 0f,
            IsActive = true
        };

        return planet;
    }
}
