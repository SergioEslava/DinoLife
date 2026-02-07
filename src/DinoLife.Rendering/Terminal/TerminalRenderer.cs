using System;
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

    public void Render(WorldSnapshot snapshot)
    {
        if (!_initialized) { Initialize(); }

        EnsureBuffers();
        ClearBackBuffer();

        DrawEntities(snapshot);
        DrawHud(snapshot);

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

    private void DrawEntities(WorldSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.Entities.Length; i++)
        {
            SnapshotEntity entity = snapshot.Entities[i];
            if (!entity.IsAlive) { continue; }

            int x = ToScreenX(entity.Position.X, snapshot.WorldSize.X);
            int y = ToScreenY(entity.Position.Y, snapshot.WorldSize.Y);

            char symbol = GetSymbol(entity.Type);
            SetCell(x, y, symbol);
        }
    }

    private void DrawHud(WorldSnapshot snapshot)
    {
        string hud = $"Tick {snapshot.Tick}  H:{snapshot.Stats.Herbivores} C:{snapshot.Stats.Carnivores} P:{snapshot.Stats.Plants} S:{snapshot.Stats.Scavengers}";
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

    private static char GetSymbol(DinoLife.Core.Entities.EntityType type)
    {
        return type switch
        {
            DinoLife.Core.Entities.EntityType.Herbivore => 'H',
            DinoLife.Core.Entities.EntityType.Carnivore => 'C',
            DinoLife.Core.Entities.EntityType.Plant => 'P',
            DinoLife.Core.Entities.EntityType.Scavenger => 'S',
            _ => '?'
        };
    }
}
