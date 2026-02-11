using System;
using System.Diagnostics;
using DinoLife.Core.Entities;
namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Simple terminal renderer using a double-buffered character grid.
/// </summary>
public sealed class TerminalRenderer : IRenderer
{
    private const int TopHudHeight = 1;
    private const int OverlayHeight = 8;
    private const int TickHistorySize = 60;

    private DoubleBuffer? _doubleBuffer;
    private int _width;
    private int _height;
    private int _drawableHeight;
    private int _viewportTop;
    private int _viewportBottomExclusive;
    private bool _initialized;
    private readonly ColorScheme _scheme;
    private readonly Stopwatch _frameClock = Stopwatch.StartNew();
    private long _lastFrameTicks;
    private int _lastTick = -1;
    private float _fps;
    private float _tps;
    private float _lastTickMs;
    private readonly float[] _tickMsHistory = new float[TickHistorySize];
    private int _tickMsCount;
    private int _tickMsWriteIndex;

    public TerminalRenderer(ColorScheme? scheme = null)
    {
        _scheme = scheme ?? new ColorScheme();
    }

    public bool ShowPerformanceOverlay { get; set; }

    public void Initialize()
    {
        EnsureBuffers();
        UpdateViewport();
        Console.CursorVisible = false;
        Console.ForegroundColor = _scheme.Default;
        Console.Clear();
        _initialized = true;
    }

    public void Render(WorldSnapshot snapshot)
    {
        if (!_initialized) { Initialize(); }

        EnsureBuffers();
        UpdateViewport();
        ClearBackBuffer();
        UpdateRates(snapshot.Tick);
        DrawHud(snapshot);

        if (snapshot.ShowGrid)
        {
            DrawGrid(snapshot);
        }

        DrawCorpses(snapshot);
        DrawEntities(snapshot);
        DrawPerformanceOverlay(snapshot);

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

    private void UpdateViewport()
    {
        _viewportTop = TopHudHeight;
        int reservedBottom = ShowPerformanceOverlay ? OverlayHeight : 0;
        _viewportBottomExclusive = Math.Max(_viewportTop + 1, _height - reservedBottom);
        _drawableHeight = Math.Max(1, _viewportBottomExclusive - _viewportTop);
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
            int y = _viewportTop + ToScreenY(entity.Position.Y, snapshot.WorldSize.Y);

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
            int y = _viewportTop + ToScreenY(corpse.Position.Y, snapshot.WorldSize.Y);
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
                SetCell(sx, sy + _viewportTop, '|', _scheme.Grid);
            }
        }

        for (int gy = 1; gy < cellsY; gy++)
        {
            float wy = gy * cellSize;
            int sy = _viewportTop + ToScreenY(wy, worldHeight);
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
                int sy = _viewportTop + ToScreenY(wy, worldHeight);
                SetCell(sx, sy, '+', _scheme.Grid);
            }
        }
    }

    private void DrawHud(WorldSnapshot snapshot)
    {
        int y = 0;
        for (int i = 0; i < _width; i++) { SetCell(i, y, ' ', _scheme.Hud); }

        int x = 0;
        x = WriteText(x, y, $"Tick:{snapshot.Tick} ", _scheme.Hud);
        x = WriteText(x, y, $"FPS:{_fps:0.0} ", _scheme.Hud);
        x = WriteText(x, y, $"TPS:{_tps:0.0} ", _scheme.Hud);
        x = WriteText(x, y, $"H:{snapshot.Stats.Herbivores} C:{snapshot.Stats.Carnivores} P:{snapshot.Stats.Plants} S:{snapshot.Stats.Scavengers} ", _scheme.Hud);
        x = WriteText(x, y, $"E:{snapshot.Stats.TotalEnergy:0.0} ", _scheme.Hud);
        x = WriteText(x, y, $"AvgAge:{snapshot.Stats.AverageLifespan:0.0}s ", _scheme.Hud);

        x = WriteText(x, y, "HP ", _scheme.Hud);
        x = WriteText(x, y, $"L:{snapshot.Stats.HealthLow} ", _scheme.HealthLow);
        x = WriteText(x, y, $"M:{snapshot.Stats.HealthMedium} ", _scheme.HealthMedium);
        WriteText(x, y, $"H:{snapshot.Stats.HealthHigh}", _scheme.HealthHigh);
    }

    private void DrawPerformanceOverlay(WorldSnapshot snapshot)
    {
        if (!ShowPerformanceOverlay) { return; }
        if (_height <= TopHudHeight + 2) { return; }

        int panelTop = Math.Max(_viewportBottomExclusive, TopHudHeight + 1);
        int panelBottomExclusive = _height;

        for (int y = panelTop; y < panelBottomExclusive; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                SetCell(x, y, ' ', _scheme.Hud);
            }
        }

        int entitiesAlive = snapshot.Stats.Herbivores
            + snapshot.Stats.Carnivores
            + snapshot.Stats.Plants
            + snapshot.Stats.Scavengers;
        float memoryMb = GC.GetTotalMemory(false) / (1024f * 1024f);

        int line = panelTop;
        int px = 0;
        px = WriteText(px, line, "PERF [O toggle] ", _scheme.Hud);
        px = WriteText(px, line, $"FPS:{_fps:0.0} ", _scheme.Hud);
        px = WriteText(px, line, $"TPS:{_tps:0.0} ", _scheme.Hud);
        px = WriteText(px, line, $"TickMs:{_lastTickMs:0.00} ", _scheme.Hud);
        px = WriteText(px, line, $"Mem:{memoryMb:0.0}MB ", _scheme.Hud);
        WriteText(px, line, $"Ent:{entitiesAlive}", _scheme.Hud);

        int graphLabelY = line + 1;
        if (graphLabelY < panelBottomExclusive)
        {
            WriteText(0, graphLabelY, "Tick time graph (last 60)", _scheme.Hud);
        }

        int graphTop = line + 2;
        int graphRows = Math.Max(1, panelBottomExclusive - graphTop - 1);
        int graphWidth = Math.Min(TickHistorySize, Math.Max(0, _width - 2));
        if (graphRows <= 0 || graphWidth <= 0 || _tickMsCount == 0) { return; }

        int points = Math.Min(_tickMsCount, graphWidth);
        float maxTickMs = 0.1f;
        for (int i = 0; i < points; i++)
        {
            float sample = GetHistorySampleFromOldest(points, i);
            if (sample > maxTickMs) { maxTickMs = sample; }
        }

        for (int i = 0; i < points; i++)
        {
            float sample = GetHistorySampleFromOldest(points, i);
            int barHeight = (int)MathF.Round((sample / maxTickMs) * (graphRows - 1));
            if (barHeight < 0) { barHeight = 0; }
            if (barHeight > graphRows - 1) { barHeight = graphRows - 1; }

            ConsoleColor barColor = sample <= 16.7f
                ? _scheme.HealthHigh
                : sample <= 33.3f
                    ? _scheme.HealthMedium
                    : _scheme.HealthLow;

            int x = 1 + i;
            for (int row = 0; row < graphRows; row++)
            {
                int y = graphTop + row;
                bool filled = row >= (graphRows - 1 - barHeight);
                SetCell(x, y, filled ? '#' : '.', filled ? barColor : _scheme.Grid);
            }
        }

        int axisY = panelBottomExclusive - 1;
        if (axisY >= graphTop)
        {
            WriteText(0, axisY, $"0ms/{maxTickMs:0.0}ms", _scheme.Hud);
        }
    }

    private int WriteText(int x, int y, string text, ConsoleColor color)
    {
        int writeX = x;
        for (int i = 0; i < text.Length && writeX < _width; i++)
        {
            SetCell(writeX, y, text[i], color);
            writeX++;
        }

        return writeX;
    }

    private void UpdateRates(int currentTick)
    {
        long nowTicks = _frameClock.ElapsedTicks;
        if (_lastFrameTicks == 0L)
        {
            _lastFrameTicks = nowTicks;
            _lastTick = currentTick;
            return;
        }

        double deltaSeconds = (nowTicks - _lastFrameTicks) / (double)Stopwatch.Frequency;
        if (deltaSeconds <= 0d) { return; }

        _fps = (float)(1d / deltaSeconds);
        int tickDelta = Math.Max(0, currentTick - _lastTick);
        _tps = (float)(tickDelta / deltaSeconds);
        _lastTickMs = tickDelta > 0 ? (float)((deltaSeconds * 1000d) / tickDelta) : 0f;
        PushTickHistory(_lastTickMs);

        _lastFrameTicks = nowTicks;
        _lastTick = currentTick;
    }

    private void PushTickHistory(float tickMs)
    {
        _tickMsHistory[_tickMsWriteIndex] = tickMs;
        _tickMsWriteIndex = (_tickMsWriteIndex + 1) % TickHistorySize;
        if (_tickMsCount < TickHistorySize) { _tickMsCount++; }
    }

    private float GetHistorySampleFromOldest(int points, int offset)
    {
        int oldest = (_tickMsWriteIndex - points + TickHistorySize) % TickHistorySize;
        int idx = (oldest + offset) % TickHistorySize;
        return _tickMsHistory[idx];
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
