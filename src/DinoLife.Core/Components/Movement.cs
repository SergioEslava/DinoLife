using DinoLife.Core.Utils;

namespace DinoLife.Core.Components;
/// <summary>
/// Movement and velocity properties.
/// </summary>
public struct Movement
{
    public Vector2 Velocity;      // Current velocity
    public float Speed;           // Max speed (units/second)
    public float Acceleration;    // Rate of speed change
    
    public void SetDirection(Vector2 direction)
    {
        Velocity = direction.Normalized() * Speed;
    }
}