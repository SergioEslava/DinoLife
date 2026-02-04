namespace DinoLife.Core.Components;

/// <summary>
/// Reproduction mechanics.
/// </summary>
public struct Reproduction
{
    public float ReproductionThreshold;  // Min energy to reproduce
    public float ReproductionCost;       // Energy consumed when reproducing
    public float Cooldown;               // Time since last reproduction
    public float CooldownDuration;       // Required time between reproductions
    
    public bool CanReproduce(float currentEnergy)
    {
        return currentEnergy >= ReproductionThreshold && Cooldown >= CooldownDuration;
    }
}