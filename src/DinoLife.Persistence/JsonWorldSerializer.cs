using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Persistence;

public sealed class JsonWorldSerializer : IWorldSerializer
{
    public const string CurrentSchemaVersion = "1.0";

    private static readonly JsonSerializerOptions FileJsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        WriteIndented = false,
        IncludeFields = true
    };

    public string SchemaVersion => CurrentSchemaVersion;

    public void Save(string path, Planet world)
    {
        SaveFileV1 model = CaptureState(world);
        model.ChecksumSha256 = ComputeChecksum(model);
        string json = JsonSerializer.Serialize(model, FileJsonOptions);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    public bool TryLoad(string path, out Planet world, out string error)
    {
        world = default!;
        error = string.Empty;

        if (!File.Exists(path))
        {
            error = "No save file found";
            return false;
        }

        SaveFileV1? model;
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            model = JsonSerializer.Deserialize<SaveFileV1>(json, FileJsonOptions);
        }
        catch (Exception)
        {
            error = "Corrupted or unreadable save file";
            return false;
        }

        if (model is null)
        {
            error = "Invalid save file";
            return false;
        }

        if (model.SchemaVersion != CurrentSchemaVersion)
        {
            error = $"Unsupported save version: {model.SchemaVersion}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(model.ChecksumSha256))
        {
            error = "Save file checksum missing";
            return false;
        }

        string expectedChecksum = ComputeChecksum(model);
        if (!string.Equals(expectedChecksum, model.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
        {
            error = "Save file corruption detected (checksum mismatch)";
            return false;
        }

        world = BuildPlanetFromSave(model);
        return true;
    }

    internal bool TryReadMetadata(string path, out SaveMetadata metadata)
    {
        metadata = default!;
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            SaveFileV1? model = JsonSerializer.Deserialize<SaveFileV1>(json, FileJsonOptions);
            if (model is null || model.SchemaVersion != CurrentSchemaVersion)
            {
                return false;
            }

            FileInfo fi = new FileInfo(path);
            metadata = new SaveMetadata(
                Path: path,
                Name: fi.Name,
                CreatedUtc: model.CreatedUtc,
                Tick: model.Tick,
                SizeBytes: fi.Length,
                IsValid: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeChecksum(SaveFileV1 model)
    {
        string original = model.ChecksumSha256;
        model.ChecksumSha256 = string.Empty;
        string canonicalJson = JsonSerializer.Serialize(model, CanonicalJsonOptions);
        model.ChecksumSha256 = original;

        byte[] bytes = Encoding.UTF8.GetBytes(canonicalJson);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static SaveFileV1 CaptureState(Planet world)
    {
        EntitySlotSave[] slots = new EntitySlotSave[world.EntityCount];
        for (int i = 0; i < world.EntityCount; i++)
        {
            slots[i] = new EntitySlotSave
            {
                Entity = world.Entities[i],
                Transform = world.Transforms[i],
                Movement = world.Movements[i],
                Metabolism = world.Metabolisms[i],
                Diet = world.Diets[i],
                Plant = world.Plants[i],
                Reproduction = world.Reproductions[i],
                Lifespan = world.Lifespans[i]
            };
        }

        return new SaveFileV1
        {
            SchemaVersion = CurrentSchemaVersion,
            CreatedUtc = DateTimeOffset.UtcNow,
            Tick = world.Tick,
            WorldSize = world.WorldSize,
            DebugDrawGrid = world.DebugDrawGrid,
            EntitySlots = slots,
            Corpses = world.Corpses.ToArray(),
            ChecksumSha256 = string.Empty
        };
    }

    private static Planet BuildPlanetFromSave(SaveFileV1 model)
    {
        Planet planet = new Planet
        {
            WorldSize = model.WorldSize,
            DebugDrawGrid = model.DebugDrawGrid,
            Tick = model.Tick
        };

        for (int i = 0; i < model.EntitySlots.Length; i++)
        {
            planet.AllocateEntitySlot();
        }

        for (int i = 0; i < model.EntitySlots.Length; i++)
        {
            EntitySlotSave slot = model.EntitySlots[i];
            planet.Entities[i] = slot.Entity;
            planet.Transforms[i] = slot.Transform;
            planet.Movements[i] = slot.Movement;
            planet.Metabolisms[i] = slot.Metabolism;
            planet.Diets[i] = slot.Diet;
            planet.Plants[i] = slot.Plant;
            planet.Reproductions[i] = slot.Reproduction;
            planet.Lifespans[i] = slot.Lifespan;
        }

        for (int i = 0; i < model.EntitySlots.Length; i++)
        {
            if (!planet.Entities[i].IsAlive)
            {
                planet.FreeEntitySlot(i);
            }
        }

        planet.Corpses.Clear();
        if (model.Corpses is not null)
        {
            planet.Corpses.AddRange(model.Corpses);
        }

        return planet;
    }
}

public sealed class SaveFileV1
{
    public string SchemaVersion { get; set; } = JsonWorldSerializer.CurrentSchemaVersion;
    public DateTimeOffset CreatedUtc { get; set; }
    public int Tick { get; set; }
    public Vector2 WorldSize { get; set; }
    public bool DebugDrawGrid { get; set; }
    public EntitySlotSave[] EntitySlots { get; set; } = [];
    public Corpse[] Corpses { get; set; } = [];
    public string ChecksumSha256 { get; set; } = string.Empty;
}

public sealed class EntitySlotSave
{
    public Entity Entity { get; set; }
    public Transform Transform { get; set; }
    public Movement Movement { get; set; }
    public Metabolism Metabolism { get; set; }
    public Diet Diet { get; set; }
    public Plant Plant { get; set; }
    public Reproduction Reproduction { get; set; }
    public Lifespan Lifespan { get; set; }
}
