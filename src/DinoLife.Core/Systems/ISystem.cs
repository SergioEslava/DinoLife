using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

/// <summary>
/// System contract implemented by subsystems that modify the <see cref="Planet"/> per tick.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Update the provided <paramref name="planet"/> by <paramref name="deltatime"/> seconds.
    /// </summary>
    /// <param name="planet">World instance containing component storage.</param>
    /// <param name="deltatime">Fixed timestep in seconds.</param>
    void Update(Planet planet, double deltatime);
}