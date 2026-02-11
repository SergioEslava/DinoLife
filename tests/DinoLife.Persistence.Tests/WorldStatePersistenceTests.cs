using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using DinoLife.Persistence;
using FluentAssertions;
using System.Text.Json.Nodes;
using Xunit;

namespace DinoLife.Persistence.Tests;

public sealed class JsonWorldSerializerTests
{
    [Fact]
    public void SaveAndLoad_Roundtrip_ShouldPreserveRenderableEntityData()
    {
        var serializer = new JsonWorldSerializer();
        string path = Path.GetTempFileName();
        try
        {
            Planet source = BuildSampleWorld();
            source.Tick = 42;
            source.DebugDrawGrid = true;
            source.AddCorpse(new Vector2(8f, 9f), energy: 12.5f);

            serializer.Save(path, source);

            bool loadedOk = serializer.TryLoad(path, out Planet loaded, out string error);

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
        var serializer = new JsonWorldSerializer();
        string path = Path.Combine(Path.GetTempPath(), $"dino-life-missing-{Guid.NewGuid():N}.json");

        bool loadedOk = serializer.TryLoad(path, out Planet loaded, out string error);

        loadedOk.Should().BeFalse();
        error.Should().Be("No save file found");
        loaded.Should().BeNull();
    }

    [Fact]
    public void TryLoad_ShouldFail_WhenChecksumDoesNotMatch()
    {
        var serializer = new JsonWorldSerializer();
        string path = Path.GetTempFileName();
        try
        {
            Planet source = BuildSampleWorld();
            serializer.Save(path, source);

            JsonNode root = JsonNode.Parse(File.ReadAllText(path))!;
            root["Tick"] = ((int?)root["Tick"] ?? 0) + 1; // Tamper content
            File.WriteAllText(path, root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            bool loadedOk = serializer.TryLoad(path, out Planet loaded, out string error);

            loadedOk.Should().BeFalse();
            error.ToLowerInvariant().Should().Contain("checksum");
            loaded.Should().BeNull();
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
    public void TryLoad_ShouldFail_WhenSchemaVersionIsUnsupported()
    {
        var serializer = new JsonWorldSerializer();
        string path = Path.GetTempFileName();
        try
        {
            Planet source = BuildSampleWorld();
            serializer.Save(path, source);

            JsonNode root = JsonNode.Parse(File.ReadAllText(path))!;
            root["SchemaVersion"] = "2.0";
            File.WriteAllText(path, root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            bool loadedOk = serializer.TryLoad(path, out Planet loaded, out string error);

            loadedOk.Should().BeFalse();
            error.Should().Contain("Unsupported save version");
            loaded.Should().BeNull();
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
    public void SaveBrowser_ShouldListSavesFromDirectory_OrderedByMostRecent()
    {
        var serializer = new JsonWorldSerializer();
        var browser = new SaveBrowser(serializer);
        string dir = Path.Combine(Path.GetTempPath(), $"dino-life-saves-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            Planet source = BuildSampleWorld();
            string first = Path.Combine(dir, "first.json");
            serializer.Save(first, source);
            Thread.Sleep(20);
            string second = Path.Combine(dir, "second.json");
            serializer.Save(second, source);

            IReadOnlyList<SaveMetadata> saves = browser.ListSaves(dir);

            saves.Should().HaveCount(2);
            saves[0].Name.Should().Be("second.json");
            saves[1].Name.Should().Be("first.json");
            saves[0].Tick.Should().Be(source.Tick);
            saves[0].IsValid.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
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
