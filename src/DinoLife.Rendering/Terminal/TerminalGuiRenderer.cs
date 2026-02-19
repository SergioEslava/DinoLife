using System;
using System.Collections.Generic;
using DinoLife.Rendering.Terminal.UI;

namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Transitional TUI renderer that reuses the world renderer and adds a simple
/// in-screen command window for menu-based interactions.
/// </summary>
public sealed class TerminalGuiRenderer : IInteractiveRenderer
{
    private readonly TerminalRenderer _inner;
    private readonly OverlayHost _overlayHost = new();
    private readonly CommandMenuOverlay _menuOverlay = new();

    public TerminalGuiRenderer(ColorScheme? scheme = null)
    {
        _inner = new TerminalRenderer(scheme);
        _overlayHost.Add(_menuOverlay);
    }

    public bool ShowPerformanceOverlay
    {
        get => _inner.ShowPerformanceOverlay;
        set => _inner.ShowPerformanceOverlay = value;
    }

    public bool ShowHelpOverlay
    {
        get => _inner.ShowHelpOverlay;
        set => _inner.ShowHelpOverlay = value;
    }

    public string? StatusText
    {
        get => _inner.StatusText;
        set => _inner.StatusText = value;
    }

    public Guid? SelectedEntityId
    {
        get => _inner.SelectedEntityId;
        set => _inner.SelectedEntityId = value;
    }

    public Guid? FollowEntityId
    {
        get => _inner.FollowEntityId;
        set => _inner.FollowEntityId = value;
    }

    public bool ShowMenuOverlay
    {
        get => _menuOverlay.IsVisible;
        set
        {
            bool wasVisible = _menuOverlay.IsVisible;
            _menuOverlay.IsVisible = value;
            if (wasVisible && !value)
            {
                _inner.RequestFullRedraw();
            }
        }
    }

    public string MenuTitle { get; set; } = "COMMAND MENU";

    public IReadOnlyList<string> MenuItems { get; set; } = Array.Empty<string>();

    public int SelectedMenuIndex { get; set; } = -1;

    public void Initialize() => _inner.Initialize();

    public void Render(WorldSnapshot snapshot)
    {
        _inner.Render(snapshot);
        _menuOverlay.Title = MenuTitle;
        _menuOverlay.Items = MenuItems;
        _menuOverlay.SelectedIndex = SelectedMenuIndex;
        _overlayHost.Render(UiCanvas.CreateForConsole());
        TryHideCursor();
    }

    public void Shutdown() => _inner.Shutdown();

    public void Pan(float normalizedX, float normalizedY) => _inner.Pan(normalizedX, normalizedY);

    public void ZoomIn() => _inner.ZoomIn();

    public void ZoomOut() => _inner.ZoomOut();

    public void ResetCamera() => _inner.ResetCamera();

    public void RequestFullRedraw() => _inner.RequestFullRedraw();

    private static void TryHideCursor()
    {
        try
        {
            Console.CursorVisible = false;
        }
        catch (Exception)
        {
            // Ignore unsupported cursor APIs on some hosts.
        }
    }

}
