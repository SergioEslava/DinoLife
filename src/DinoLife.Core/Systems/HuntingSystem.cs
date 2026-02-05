using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Handles detection and consumption of prey/food by entities with a diet.
/// </summary>
public class HuntingSystem : ISystem
{
    /// <summary>
    /// Execute hunting/detection logic for entities on the <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime) {}
}