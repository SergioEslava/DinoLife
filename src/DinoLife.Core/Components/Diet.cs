namespace DinoLife.Core.Components;

/// <summary>
/// What this entity can eat.
/// </summary>
public struct Diet
{
    public FoodType FoodType;     // What it consumes
    public float DetectionRadius; // How far it can detect food
    public float EatRadius;       // How close to eat
    public float EatingDuration;  // Seconds to consume
}

/// <summary>
/// Types of food an entity can consume.
/// </summary>
public enum FoodType
{
    None,
    Plant,
    Herbivore,
    Corpse
}