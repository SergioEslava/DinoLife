namespace DinoLife.Core.Components;

/// <summary>
/// Age and death by old age.
/// </summary>
public struct Lifespan
{
    public float Age;             // Seconds alive
    public float MaxAge;          // Die when Age > MaxAge
    
    public bool IsDead => Age >= MaxAge;
}