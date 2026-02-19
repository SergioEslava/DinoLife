using System;

namespace DinoLife.Rendering;

/// <summary>
/// Renderer contract plus interactive camera/UI controls used by the console host.
/// </summary>
public interface IInteractiveRenderer : IRenderer
{
    bool ShowPerformanceOverlay { get; set; }
    bool ShowHelpOverlay { get; set; }
    string? StatusText { get; set; }
    Guid? SelectedEntityId { get; set; }
    Guid? FollowEntityId { get; set; }

    void Pan(float normalizedX, float normalizedY);
    void ZoomIn();
    void ZoomOut();
    void ResetCamera();
}
