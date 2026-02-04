namespace DinoLife.Core.Components;

/// <summary>
/// Energy and hunger mechanics.
/// </summary>
public struct Metabolism
{
    public float Energy;          // Current energy [0, MaxEnergy]
    public float MaxEnergy;       // Capacity
    public float HungerRate;      // Energy depleted per second
    public float EnergyGainRate;  // Multiplier when eating
    
    public bool IsStarving => Energy <= 0f;
    public bool IsSatiated => Energy >= MaxEnergy * 0.8f;
}