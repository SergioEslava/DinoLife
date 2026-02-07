using DinoLife.Core.Utils;
using DinoLife.Core.Entities;

namespace DinoLife.Rendering;

/// <summary>
/// Lightweight render snapshot of the world state.
/// </summary>
public sealed class WorldSnapshot
{
    public required int Tick { get; init; }
    public required Vector2 WorldSize { get; init; }
    public required SnapshotEntity[] Entities { get; init; }
    public required SnapshotStats Stats { get; init; }
}

public readonly record struct SnapshotEntity(EntityType Type, Vector2 Position, bool IsAlive);

public readonly record struct SnapshotStats(int Herbivores, int Carnivores, int Plants, int Scavengers);
