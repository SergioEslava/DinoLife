namespace DinoLife.Rendering.Terminal.UI;

/// <summary>
/// Renderable terminal overlay component.
/// </summary>
public interface IOverlay
{
    bool IsVisible { get; }

    void Render(UiCanvas canvas);
}
