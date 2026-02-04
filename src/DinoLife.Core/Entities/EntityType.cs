namespace DinoLife.Core.Entities;

/// <summary>
/// Type of entity
/// </summary>
[Flags]
public enum EntityType
{
    Herbivore = 0, 
    Carnivore = 1 << 0, 
    Plant = 1 << 1, 
    Scavenger = 1 << 2
}