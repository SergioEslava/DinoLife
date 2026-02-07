namespace DinoLife.Core.Entities;

/// <summary>
/// Flags identifying which component data slots are populated for an entity.
/// Used by the world to quickly test component membership.
/// </summary>
[Flags]
public enum ComponentFlags
{
    None = 0,
    Transform = 1 << 0,
    Metabolism = 1 << 1,
    Movement = 1 << 2,
    Diet = 1 << 3,
    Reproduction = 1 << 4,
    Plant = 1 << 5
}
