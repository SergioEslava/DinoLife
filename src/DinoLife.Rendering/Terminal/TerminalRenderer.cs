using System;
using DinoLife.Core.Entities;
namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Simple terminal renderer using a double-buffered character grid.
/// </summary>
public sealed class TerminalRenderer : IRenderer
{
    private DoubleBuffer? _doubleBuffer;
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

        if (_doubleBuffer is not null && _width == width && _height == height)
        {
            return;
        }

        _width = width;
        _height = height;
        _drawableHeight = Math.Max(1, _height - 1); // Reserve last line for HUD

        if (_doubleBuffer is null)
        {
            _doubleBuffer = new DoubleBuffer(_width, _height, _scheme.Default);
        }
        else
        {
            _doubleBuffer.Resize(_width, _height);
        }

        Console.Clear();
    }

    private void ClearBackBuffer()
    {
        _doubleBuffer?.ClearBack();
    }

    private void DrawEntities(WorldSnapshot snapshot)
    {
        DrawEntityLayer(snapshot, EntityType.Plant);
        DrawEntityLayer(snapshot, EntityType.Herbivore);
        DrawEntityLayer(snapshot, EntityType.Scavenger);
        DrawEntityLayer(snapshot, EntityType.Carnivore);
    }

    private void DrawEntityLayer(WorldSnapshot snapshot, EntityType layerType)
    {
        for (int i = 0; i < snapshot.Entities.Length; i++)
        {
            SnapshotEntity entity = snapshot.Entities[i];
            if (!entity.IsAlive) { continue; }
            if (entity.Type != layerType) { continue; }
            if (!IsInsideWorld(entity.Position, snapshot.WorldSize)) { continue; }

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
            if (!IsInsideWorld(corpse.Position, snapshot.WorldSize)) { continue; }
            int x = ToScreenX(corpse.Position.X, snapshot.WorldSize.X);
            int y = ToScreenY(corpse.Position.Y, snapshot.WorldSize.Y);
            SetCell(x, y, 'X', _scheme.Corpse);
        }
    }

    private static bool IsInsideWorld(DinoLife.Core.Utils.Vector2 position, DinoLife.Core.Utils.Vector2 worldSize)
    {
        if (worldSize.X <= 0f || worldSize.Y <= 0f) { return false; }
        return position.X >= 0f && position.X <= worldSize.X
            && position.Y >= 0f && position.Y <= worldSize.Y;
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
        if (_doubleBuffer is null) { return; }

        foreach (CellDiff diff in _doubleBuffer.GetDiff())
        {
            Console.SetCursorPosition(diff.X, diff.Y);
            Console.ForegroundColor = diff.Color;
            Console.Write(diff.Character);
        }
    }

    private void SwapBuffers()
    {
        _doubleBuffer?.Swap();
    }

    private void SetCell(int x, int y, char c, ConsoleColor color)
    {
        _doubleBuffer?.SetBackCell(x, y, c, color);
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
