using System;
using System.Collections.Generic;

namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Double buffer for terminal rendering with diff support.
/// </summary>
public sealed class DoubleBuffer
{
    private readonly ConsoleColor _defaultColor;
    private char[,] _backChars;
    private char[,] _frontChars;
    private ConsoleColor[,] _backColors;
    private ConsoleColor[,] _frontColors;

    public DoubleBuffer(int width, int height, ConsoleColor defaultColor)
    {
        if (width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
        if (height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }

        Width = width;
        Height = height;
        _defaultColor = defaultColor;

        _backChars = new char[height, width];
        _frontChars = new char[height, width];
        _backColors = new ConsoleColor[height, width];
        _frontColors = new ConsoleColor[height, width];

        FillAll(' ', _defaultColor);
    }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public void Resize(int width, int height)
    {
        if (width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
        if (height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
        if (width == Width && height == Height) { return; }

        Width = width;
        Height = height;
        _backChars = new char[height, width];
        _frontChars = new char[height, width];
        _backColors = new ConsoleColor[height, width];
        _frontColors = new ConsoleColor[height, width];
        FillAll(' ', _defaultColor);
    }

    public void ClearBack(char fillChar = ' ')
    {
        FillGrid(_backChars, fillChar);
        FillGrid(_backColors, _defaultColor);
    }

    public void SetBackCell(int x, int y, char c, ConsoleColor color)
    {
        if (x < 0 || x >= Width) { return; }
        if (y < 0 || y >= Height) { return; }

        _backChars[y, x] = c;
        _backColors[y, x] = color;
    }

    public IEnumerable<CellDiff> GetDiff()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_backChars[y, x] == _frontChars[y, x] && _backColors[y, x] == _frontColors[y, x])
                {
                    continue;
                }

                yield return new CellDiff(x, y, _backChars[y, x], _backColors[y, x]);
            }
        }
    }

    public void Swap()
    {
        (_frontChars, _backChars) = (_backChars, _frontChars);
        (_frontColors, _backColors) = (_backColors, _frontColors);
    }

    private void FillAll(char fillChar, ConsoleColor fillColor)
    {
        FillGrid(_backChars, fillChar);
        FillGrid(_frontChars, fillChar);
        FillGrid(_backColors, fillColor);
        FillGrid(_frontColors, fillColor);
    }

    private static void FillGrid(char[,] grid, char value)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = value;
            }
        }
    }

    private static void FillGrid(ConsoleColor[,] grid, ConsoleColor value)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = value;
            }
        }
    }
}

public readonly record struct CellDiff(int X, int Y, char Character, ConsoleColor Color);
