using System;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Core.World;

namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Simple terminal renderer using a double-buffered character grid.
/// </summary>
public sealed class TerminalRenderer : IRenderer
{
    private char[]? _backBuffer;
    private char[]? _frontBuffer;
    private int _width;
    private int _height;
    private int _drawableHeight;
    private bool _initialized;

    public void Initialize()
    {
        EnsureBuffers();
        Console.CursorVisible = false;
        Console.Clear();
        _initialized = true;
    }

    public void Render(Planet planet)
    {
        if (!_initialized) { Initialize(); }

        EnsureBuffers();
        ClearBackBuffer();

        DrawEntities(planet);
        DrawHud(planet);

        FlushDiffToConsole();
        SwapBuffers();
    }

    public void Shutdown()
    {
        if (!_initialized) { return; }

        Console.CursorVisible = true;
        _initialized = false;
    }

    private void EnsureBuffers()
    {
        int width = Math.Max(20, Console.WindowWidth);
        int height = Math.Max(5, Console.WindowHeight);

        if (_backBuffer is not null && _width == width && _height == height)
        {
            return;
        }

        _width = width;
        _height = height;
        _drawableHeight = Math.Max(1, _height - 1); // Reserve last line for HUD

        int size = _width * _height;
        _backBuffer = new char[size];
        _frontBuffer = new char[size];

        Array.Fill(_backBuffer, ' ');
        Array.Fill(_frontBuffer, ' ');
        Console.Clear();
    }

    private void ClearBackBuffer()
    {
        if (_backBuffer is null) { return; }
        Array.Fill(_backBuffer, ' ');
    }

    private void DrawEntities(Planet planet)
    {
        Span<Entity> entities = planet.Entities.AsSpan(0, planet.EntityCount);
        Span<Transform> transforms = planet.Transforms.AsSpan(0, planet.EntityCount);

        int herbivores = 0;
        int carnivores = 0;
        int plants = 0;
        int scavengers = 0;

        for (int i = 0; i < entities.Length; i++)
        {
            if (!entities[i].IsAlive) { continue; }
            if (!entities[i].Has(ComponentFlags.Transform)) { continue; }

            Vector2 pos = transforms[i].Position;
            int x = ToScreenX(pos.X, planet.WorldSize.X);
            int y = ToScreenY(pos.Y, planet.WorldSize.Y);

            char symbol = GetSymbol(entities[i].Type);
            SetCell(x, y, symbol);

            switch (entities[i].Type)
            {
                case EntityType.Herbivore:
                    herbivores++;
                    break;
                case EntityType.Carnivore:
                    carnivores++;
                    break;
                case EntityType.Plant:
                    plants++;
                    break;
                case EntityType.Scavenger:
                    scavengers++;
                    break;
            }
        }

        _lastCounts = (herbivores, carnivores, plants, scavengers);
    }

    private (int herbivores, int carnivores, int plants, int scavengers) _lastCounts;

    private void DrawHud(Planet planet)
    {
        string hud = $"Tick {planet.Tick}  H:{_lastCounts.herbivores} C:{_lastCounts.carnivores} P:{_lastCounts.plants} S:{_lastCounts.scavengers}";
        int y = _height - 1;
        for (int i = 0; i < _width; i++)
        {
            char c = i < hud.Length ? hud[i] : ' ';
            SetCell(i, y, c);
        }
    }

    private void FlushDiffToConsole()
    {
        if (_backBuffer is null || _frontBuffer is null) { return; }

        for (int y = 0; y < _height; y++)
        {
            int rowStart = y * _width;
            bool rowChanged = false;

            for (int x = 0; x < _width; x++)
            {
                int idx = rowStart + x;
                if (_backBuffer[idx] != _frontBuffer[idx])
                {
                    rowChanged = true;
                    break;
                }
            }

            if (!rowChanged) { continue; }

            Console.SetCursorPosition(0, y);
            Console.Write(_backBuffer, rowStart, _width);
        }
    }

    private void SwapBuffers()
    {
        if (_backBuffer is null || _frontBuffer is null) { return; }
        (_frontBuffer, _backBuffer) = (_backBuffer, _frontBuffer);
    }

    private void SetCell(int x, int y, char c)
    {
        if (_backBuffer is null) { return; }
        if (x < 0 || x >= _width) { return; }
        if (y < 0 || y >= _height) { return; }
        _backBuffer[(y * _width) + x] = c;
    }

    private int ToScreenX(float x, float worldWidth)
    {
        if (worldWidth <= 0f) { return 0; }
        float t = x / worldWidth;
        int ix = (int)(t * (_width - 1));
        return Math.Clamp(ix, 0, _width - 1);
    }

    private int ToScreenY(float y, float worldHeight)
    {
        if (worldHeight <= 0f) { return 0; }
        float t = y / worldHeight;
        int iy = (int)(t * (_drawableHeight - 1));
        return Math.Clamp(iy, 0, _drawableHeight - 1);
    }

    private static char GetSymbol(EntityType type)
    {
        return type switch
        {
            EntityType.Herbivore => 'H',
            EntityType.Carnivore => 'C',
            EntityType.Plant => 'P',
            EntityType.Scavenger => 'S',
            _ => '?'
        };
    }
}
