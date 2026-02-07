namespace DinoLife.Core.Components;

/// <summary>
/// Plant growth and respawn mechanics.
/// </summary>
public struct Plant
{
    public float Energy;
    public float MaxEnergy;
    public float GrowthRate;
    public float RespawnTime;
    public float RespawnTimer;
    public bool IsActive;
}
