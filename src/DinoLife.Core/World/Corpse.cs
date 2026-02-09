using System;
using DinoLife.Core.Utils;

namespace DinoLife.Core.World;

/// <summary>
/// Temporary world object created when entities die.
/// </summary>
public struct Corpse
{
    public Guid Id;
    public Vector2 Position;
    public float Energy;
    public float DecayTimer;
}

/// <summary>
/// Tunables for corpse lifecycle.
/// </summary>
public static class CorpseStats
{
    public const float DefaultDecayTime = 120f;
    public const float EnergyRetention = 0.5f;
}
