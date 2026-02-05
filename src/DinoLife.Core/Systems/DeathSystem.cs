using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// Removes or marks entities that meet death conditions (age, starvation, etc.).
/// </summary>
public class DeathSystem : ISystem
{
    /// <summary>
    /// Evaluate and process deaths on the provided <paramref name="planet"/>.
    /// </summary>
    public void Update(Planet planet, double deltatime) {}
}