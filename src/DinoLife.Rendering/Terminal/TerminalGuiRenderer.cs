using System;
using System.Collections.Generic;

namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Transitional TUI renderer that reuses the world renderer and adds a simple
/// in-screen command window for menu-based interactions.
/// </summary>
public sealed class TerminalGuiRenderer : IInteractiveRenderer
{
    private readonly TerminalRenderer _inner;

    public TerminalGuiRenderer(ColorScheme? scheme = null)
    {
        _inner = new TerminalRenderer(scheme);
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

    public bool ShowMenuOverlay { get; set; }

    public string MenuTitle { get; set; } = "COMMAND MENU";

    public IReadOnlyList<string> MenuItems { get; set; } = Array.Empty<string>();

    public int SelectedMenuIndex { get; set; } = -1;

    public void Initialize() => _inner.Initialize();

    public void Render(WorldSnapshot snapshot)
    {
        _inner.Render(snapshot);
        DrawMenuOverlay();
    }

    public void Shutdown() => _inner.Shutdown();

    public void Pan(float normalizedX, float normalizedY) => _inner.Pan(normalizedX, normalizedY);

    public void ZoomIn() => _inner.ZoomIn();

    public void ZoomOut() => _inner.ZoomOut();

    public void ResetCamera() => _inner.ResetCamera();

    private void DrawMenuOverlay()
    {
        if (!ShowMenuOverlay) { return; }

        int windowWidth = Math.Clamp(Console.WindowWidth, 40, 80);
        int maxItems = Math.Max(1, Console.WindowHeight - 8);
        int itemCount = Math.Min(MenuItems.Count, maxItems);
        int windowHeight = itemCount + 4;
        int left = Math.Max(0, (Console.WindowWidth - windowWidth) / 2);
        int top = Math.Max(1, (Console.WindowHeight - windowHeight) / 2);

        int selected = SelectedMenuIndex;
        if (itemCount == 0) { selected = -1; }
        else { selected = Math.Clamp(selected, 0, itemCount - 1); }

        ConsoleColor previousForeground = Console.ForegroundColor;
        ConsoleColor previousBackground = Console.BackgroundColor;

        DrawHorizontalBorder(left, top, windowWidth, '+', '-', '+');
        DrawHorizontalBorder(left, top + windowHeight - 1, windowWidth, '+', '-', '+');
        for (int row = 1; row < windowHeight - 1; row++)
        {
            WriteAt(left, top + row, "|");
            WriteAt(left + windowWidth - 1, top + row, "|");
            WriteAt(left + 1, top + row, new string(' ', windowWidth - 2));
        }

        string title = $"{MenuTitle} [M close]";
        WriteAt(left + 2, top, title[..Math.Min(title.Length, windowWidth - 4)], ConsoleColor.White);

        for (int i = 0; i < itemCount; i++)
        {
            bool isSelected = i == selected;
            ConsoleColor fg = isSelected ? ConsoleColor.Black : ConsoleColor.White;
            ConsoleColor bg = isSelected ? ConsoleColor.Gray : ConsoleColor.Black;
            string line = $"{(isSelected ? '>' : ' ')} {MenuItems[i]}";
            if (line.Length > windowWidth - 4) { line = line[..(windowWidth - 4)]; }
            line = line.PadRight(windowWidth - 4);
            WriteAt(left + 2, top + 2 + i, line, fg, bg);
        }

        Console.ForegroundColor = previousForeground;
        Console.BackgroundColor = previousBackground;
    }

    private static void DrawHorizontalBorder(int left, int y, int width, char start, char fill, char end)
    {
        if (width <= 1) { return; }
        WriteAt(left, y, start.ToString());
        WriteAt(left + 1, y, new string(fill, width - 2));
        WriteAt(left + width - 1, y, end.ToString());
    }

    private static void WriteAt(int x, int y, string text, ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (x < 0 || y < 0 || x >= Console.WindowWidth || y >= Console.WindowHeight) { return; }

        int maxChars = Console.WindowWidth - x;
        if (maxChars <= 0) { return; }
        string writeText = text.Length > maxChars ? text[..maxChars] : text;

        if (foreground.HasValue) { Console.ForegroundColor = foreground.Value; }
        if (background.HasValue) { Console.BackgroundColor = background.Value; }
        Console.SetCursorPosition(x, y);
        Console.Write(writeText);
    }
}
