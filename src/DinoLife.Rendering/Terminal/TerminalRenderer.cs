using System;
namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Simple terminal renderer using a double-buffered character grid.
/// </summary>
public sealed class TerminalRenderer : IRenderer
{
    private char[]? _backBuffer;
    private char[]? _frontBuffer;
    private ConsoleColor[]? _backColors;
    private ConsoleColor[]? _frontColors;
    private int _width;
    private int _height;
    private int _drawableHeight;
    private bool _initialized;
    private readonly ColorScheme _scheme;

    public TerminalRenderer(ColorScheme? scheme = null)
    {
        _scheme = scheme ?? new ColorScheme();
    }

    public void Initialize()
    {
        EnsureBuffers();
        Console.CursorVisible = false;
        Console.ForegroundColor = _scheme.Default;
        Console.Clear();
        _initialized = true;
    }

    public void Render(WorldSnapshot snapshot)
    {
        if (!_initialized) { Initialize(); }

        EnsureBuffers();
        ClearBackBuffer();

        if (snapshot.ShowGrid)
        {
            DrawGrid(snapshot);
        }

        DrawCorpses(snapshot);
        DrawEntities(snapshot);
        DrawHud(snapshot);

        FlushDiffToConsole();
        SwapBuffers();
    }

    public void Shutdown()
    {
        if (!_initialized) { return; }

        Console.CursorVisible = true;
        Console.ForegroundColor = _scheme.Default;
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
        _backColors = new ConsoleColor[size];
        _frontColors = new ConsoleColor[size];

        Array.Fill(_backBuffer, ' ');
        Array.Fill(_frontBuffer, ' ');
        Array.Fill(_backColors, _scheme.Default);
        Array.Fill(_frontColors, _scheme.Default);
        Console.Clear();
    }

    private void ClearBackBuffer()
    {
        if (_backBuffer is null) { return; }
        Array.Fill(_backBuffer, ' ');
        if (_backColors is not null)
        {
            Array.Fill(_backColors, _scheme.Default);
        }
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
            SetCell(x, y, symbol, GetColor(entity.Type));
        }
    }

    private void DrawCorpses(WorldSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.Corpses.Length; i++)
        {
            SnapshotCorpse corpse = snapshot.Corpses[i];
            int x = ToScreenX(corpse.Position.X, snapshot.WorldSize.X);
            int y = ToScreenY(corpse.Position.Y, snapshot.WorldSize.Y);
            SetCell(x, y, 'X', _scheme.Corpse);
        }
    }

    private void DrawGrid(WorldSnapshot snapshot)
    {
        float cellSize = snapshot.GridCellSize;
        if (cellSize <= 0f) { return; }

        float worldWidth = snapshot.WorldSize.X;
        float worldHeight = snapshot.WorldSize.Y;

        int cellsX = (int)MathF.Ceiling(worldWidth / cellSize);
        int cellsY = (int)MathF.Ceiling(worldHeight / cellSize);

        for (int gx = 1; gx < cellsX; gx++)
        {
            float wx = gx * cellSize;
            int sx = ToScreenX(wx, worldWidth);
            for (int sy = 0; sy < _drawableHeight; sy++)
            {
                SetCell(sx, sy, '|', _scheme.Grid);
            }
        }

        for (int gy = 1; gy < cellsY; gy++)
        {
            float wy = gy * cellSize;
            int sy = ToScreenY(wy, worldHeight);
            for (int sx = 0; sx < _width; sx++)
            {
                SetCell(sx, sy, '-', _scheme.Grid);
            }
        }

        for (int gx = 1; gx < cellsX; gx++)
        {
            float wx = gx * cellSize;
            int sx = ToScreenX(wx, worldWidth);
            for (int gy = 1; gy < cellsY; gy++)
            {
                float wy = gy * cellSize;
                int sy = ToScreenY(wy, worldHeight);
                SetCell(sx, sy, '+', _scheme.Grid);
            }
        }
    }

    private void DrawHud(WorldSnapshot snapshot)
    {
        string hud = $"Tick {snapshot.Tick}  H:{snapshot.Stats.Herbivores} C:{snapshot.Stats.Carnivores} P:{snapshot.Stats.Plants} S:{snapshot.Stats.Scavengers}";
        int y = _height - 1;
        for (int i = 0; i < _width; i++)
        {
            char c = i < hud.Length ? hud[i] : ' ';
            SetCell(i, y, c, _scheme.Hud);
        }
    }

    private void FlushDiffToConsole()
    {
        if (_backBuffer is null || _frontBuffer is null || _backColors is null || _frontColors is null) { return; }

        for (int y = 0; y < _height; y++)
        {
            int rowStart = y * _width;

            for (int x = 0; x < _width; x++)
            {
                int idx = rowStart + x;
                if (_backBuffer[idx] == _frontBuffer[idx] && _backColors[idx] == _frontColors[idx])
                {
                    continue;
                }

                Console.SetCursorPosition(x, y);
                Console.ForegroundColor = _backColors[idx];
                Console.Write(_backBuffer[idx]);
            }
        }
    }

    private void SwapBuffers()
    {
        if (_backBuffer is null || _frontBuffer is null || _backColors is null || _frontColors is null) { return; }
        (_frontBuffer, _backBuffer) = (_backBuffer, _frontBuffer);
        (_frontColors, _backColors) = (_backColors, _frontColors);
    }

    private void SetCell(int x, int y, char c, ConsoleColor color)
    {
        if (_backBuffer is null || _backColors is null) { return; }
        if (x < 0 || x >= _width) { return; }
        if (y < 0 || y >= _height) { return; }
        int idx = (y * _width) + x;
        _backBuffer[idx] = c;
        _backColors[idx] = color;
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

    private ConsoleColor GetColor(DinoLife.Core.Entities.EntityType type)
    {
        return type switch
        {
            DinoLife.Core.Entities.EntityType.Herbivore => _scheme.Herbivore,
            DinoLife.Core.Entities.EntityType.Carnivore => _scheme.Carnivore,
            DinoLife.Core.Entities.EntityType.Plant => _scheme.Plant,
            DinoLife.Core.Entities.EntityType.Scavenger => _scheme.Scavenger,
            _ => _scheme.Default
        };
    }
}
