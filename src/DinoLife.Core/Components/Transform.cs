using DinoLife.Core.Utils;

namespace DinoLife.Core.Components;

/// <summary>
/// Spatial properties of an entity.
/// </summary>
public struct Transform
{
    public Vector2 Position;
    public float Rotation;        // Radians, 0 = right, π/2 = up
    
    public Transform(Vector2 position)
    {
        Position = position;
        Rotation = 0f;
    }
}