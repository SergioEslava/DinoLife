namespace DinoLife.Rendering;

/// <summary>
/// Rendering contract for visualizing the simulation state.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Initialize renderer resources.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Render the current <paramref name="snapshot"/> state.
    /// </summary>
    void Render(WorldSnapshot snapshot);

    /// <summary>
    /// Release renderer resources.
    /// </summary>
    void Shutdown();
}
