using System.Collections.Generic;

namespace DinoLife.Rendering.Terminal.UI;

/// <summary>
/// Manages overlay components and renders them in insertion order.
/// </summary>
public sealed class OverlayHost
{
    private readonly List<IOverlay> _overlays = [];

    public void Add(IOverlay overlay)
    {
        _overlays.Add(overlay);
    }

    public void Render(UiCanvas canvas)
    {
        for (int i = 0; i < _overlays.Count; i++)
        {
            IOverlay overlay = _overlays[i];
            if (!overlay.IsVisible) { continue; }
            overlay.Render(canvas);
        }
    }
}
