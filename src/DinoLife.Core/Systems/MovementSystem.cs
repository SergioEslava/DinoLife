using System;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Applies movement updates to entities with movement/transform components.
/// Responsible for advancing positions and handling wrapping or bounds.
/// </summary>
public class MovementSystem : ISystem
{
    /// <summary>
    /// Update entity positions on the <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);
        Span<Movement> movements = planet.Movements.AsSpan(0, planet.EntityCount);
        float deltaTime = (float)deltatime;

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Flags.HasFlag(ComponentFlags.Movement)) { continue; }
            if (!entities[i].Flags.HasFlag(ComponentFlags.Transform)) { continue; }

            // Apply velocity
            transforms[i].Position += movements[i].Velocity * deltaTime;

            // Update rotation to match velocity
            if (movements[i].Velocity.LengthSquared() > 0.01f)
            {
                transforms[i].Rotation = MathF.Atan2(
                    movements[i].Velocity.Y,
                    movements[i].Velocity.X
                );
            }

            // Wrap around world boundaries
            transforms[i].Position = WrapPosition(
                transforms[i].Position,
                planet.WorldSize
            );
        }
    }

    private static Vector2 WrapPosition(Vector2 pos, Vector2 worldSize)
    {
        float x = pos.X % worldSize.X;
        float y = pos.Y % worldSize.Y;

        if (x < 0) { x += worldSize.X; }
        if (y < 0) { y += worldSize.Y; }

        return new Vector2(x, y);
    }
}
