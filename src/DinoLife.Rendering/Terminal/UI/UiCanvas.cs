using System;

namespace DinoLife.Rendering.Terminal.UI;

/// <summary>
/// Lightweight write-through canvas for console overlays.
/// </summary>
public sealed class UiCanvas
{
    private readonly int _width;
    private readonly int _height;

    private UiCanvas(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public int Width => _width;

    public int Height => _height;

    public static UiCanvas CreateForConsole()
    {
        int width = Math.Max(1, Console.WindowWidth);
        int height = Math.Max(1, Console.WindowHeight);
        return new UiCanvas(width, height);
    }

    public void FillRect(int x, int y, int width, int height, char fill, ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (width <= 0 || height <= 0) { return; }
        for (int row = 0; row < height; row++)
        {
            DrawText(x, y + row, new string(fill, width), foreground, background);
        }
    }

    public void DrawBox(int x, int y, int width, int height, ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (width < 2 || height < 2) { return; }

        DrawText(x, y, "+" + new string('-', width - 2) + "+", foreground, background);
        for (int row = 1; row < height - 1; row++)
        {
            DrawText(x, y + row, "|" + new string(' ', width - 2) + "|", foreground, background);
        }

        DrawText(x, y + height - 1, "+" + new string('-', width - 2) + "+", foreground, background);
    }

    public void DrawText(int x, int y, string text, ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (string.IsNullOrEmpty(text)) { return; }
        if (y < 0 || y >= _height) { return; }
        if (x >= _width) { return; }

        int writeX = Math.Max(0, x);
        int sourceStart = x < 0 ? -x : 0;
        int maxLength = _width - writeX;
        int length = Math.Min(maxLength, text.Length - sourceStart);
        if (length <= 0) { return; }

        string slice = text.Substring(sourceStart, length);

        if (foreground.HasValue) { Console.ForegroundColor = foreground.Value; }
        if (background.HasValue) { Console.BackgroundColor = background.Value; }
        Console.SetCursorPosition(writeX, y);
        Console.Write(slice);
    }
}
