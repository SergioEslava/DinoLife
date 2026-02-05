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
    public void Update(Planet planet, double deltatime) {}
}