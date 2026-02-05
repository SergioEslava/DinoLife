using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace DinoLife.Core.Utils;

/// <summary>
/// Simple 2D vector struct used across the engine for positions and velocities.
/// Small and mutable by design for performance in systems.
/// </summary>
public struct Vector2 : IEquatable<Vector2>
{
    private float _x;
    private float _y;

    /// <summary>
    /// Initialize a zero vector.
    /// </summary>
    public Vector2()
    {
        this._x = 0;
        this._y = 0;
    }

    /// <summary>
    /// Initialize a vector with the given components.
    /// </summary>
    public Vector2(float x, float y)
    {
        this._x = x;
        this._y = y;
    }

    public static Vector2 operator *(Vector2 v, float scalar) => new Vector2(v.X * scalar, v.Y * scalar);
    public static Vector2 operator *(float scalar, Vector2 v) =>  v * scalar;

    /// <summary>
    /// True when both components are equal.
    /// </summary>
    public bool Equals(Vector2 other)
        => X == other.X && Y == other.Y;

    public override bool Equals(object? obj)
        => obj is Vector2 other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(X, Y);

    public static bool operator ==(Vector2 left, Vector2 right)
        => left.Equals(right);

    public static bool operator !=(Vector2 left, Vector2 right)
        => !left.Equals(right);

    /// <summary>
    /// X component.
    /// </summary>
    public float X {get => _x; set => _x = value;}

    /// <summary>
    /// Y component.
    /// </summary>
    public float Y {get => _y; set => _y = value;}

    /// <summary>
    /// Returns a unit vector in the same direction, or zero if this vector is zero.
    /// </summary>
    public Vector2 Normalized() { float mag = Magnitude(); return mag == 0 ? new Vector2() : this * (1f/mag); }

    /// <summary>
    /// Euclidean magnitude (length) of the vector.
    /// </summary>
    public float Magnitude() => MathF.Sqrt(X*X + Y*Y);
}