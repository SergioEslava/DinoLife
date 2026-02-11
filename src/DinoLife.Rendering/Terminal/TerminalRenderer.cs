using System;
using System.Diagnostics;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;

namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Simple terminal renderer using a double-buffered character grid.
/// </summary>
public sealed class TerminalRenderer : IRenderer
{
    private const int TopHudHeight = 1;
    private const int OverlayHeight = 8;
    private const int TickHistorySize = 60;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4.0f;

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

    private bool _cameraInitialized;
    private Vector2 _lastWorldSize;
    private float _cameraCenterX;
    private float _cameraCenterY;
    private float _zoom = 1f;
    private float _cameraViewWidth = 1f;
    private float _cameraViewHeight = 1f;
    private float _cameraMinX;
    private float _cameraMinY;

    public TerminalRenderer(ColorScheme? scheme = null)
    {
        _scheme = scheme ?? new ColorScheme();
    }

    public bool ShowPerformanceOverlay { get; set; }

    public Guid? SelectedEntityId { get; set; }

    public Guid? FollowEntityId { get; set; }

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
        PrepareCamera(snapshot);
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

    public void Pan(float normalizedX, float normalizedY)
    {
        if (!_cameraInitialized) { return; }

        float deltaX = normalizedX * _cameraViewWidth * 0.1f;
        float deltaY = normalizedY * _cameraViewHeight * 0.1f;
        _cameraCenterX += deltaX;
        _cameraCenterY += deltaY;
        ClampCameraCenter();
    }

    public void ZoomIn()
    {
        _zoom = Math.Clamp(_zoom * 1.15f, MinZoom, MaxZoom);
        ClampCameraCenter();
    }

    public void ZoomOut()
    {
        _zoom = Math.Clamp(_zoom / 1.15f, MinZoom, MaxZoom);
        ClampCameraCenter();
    }

    public void ResetCamera()
    {
        _cameraInitialized = false;
        _zoom = 1f;
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

    private void PrepareCamera(WorldSnapshot snapshot)
    {
        _lastWorldSize = snapshot.WorldSize;
        if (_lastWorldSize.X <= 0f || _lastWorldSize.Y <= 0f)
        {
            _cameraViewWidth = 1f;
            _cameraViewHeight = 1f;
            _cameraMinX = 0f;
            _cameraMinY = 0f;
            return;
        }

        if (!_cameraInitialized)
        {
            _cameraCenterX = _lastWorldSize.X * 0.5f;
            _cameraCenterY = _lastWorldSize.Y * 0.5f;
            _cameraInitialized = true;
        }

        if (FollowEntityId.HasValue && TryGetEntityPosition(snapshot, FollowEntityId.Value, out Vector2 followPos))
        {
            _cameraCenterX = followPos.X;
            _cameraCenterY = followPos.Y;
        }

        ClampCameraCenter();
    }

    private void ClampCameraCenter()
    {
        if (_lastWorldSize.X <= 0f || _lastWorldSize.Y <= 0f) { return; }

        _cameraViewWidth = Math.Max(1f, _lastWorldSize.X / _zoom);
        _cameraViewHeight = Math.Max(1f, _lastWorldSize.Y / _zoom);

        float halfW = _cameraViewWidth * 0.5f;
        float halfH = _cameraViewHeight * 0.5f;

        if (_cameraViewWidth >= _lastWorldSize.X)
        {
            _cameraCenterX = _lastWorldSize.X * 0.5f;
        }
        else
        {
            _cameraCenterX = Math.Clamp(_cameraCenterX, halfW, _lastWorldSize.X - halfW);
        }

        if (_cameraViewHeight >= _lastWorldSize.Y)
        {
            _cameraCenterY = _lastWorldSize.Y * 0.5f;
        }
        else
        {
            _cameraCenterY = Math.Clamp(_cameraCenterY, halfH, _lastWorldSize.Y - halfH);
        }

        _cameraMinX = _cameraCenterX - halfW;
        _cameraMinY = _cameraCenterY - halfH;
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
            if (!TryProjectToScreen(entity.Position, out int x, out int y)) { continue; }

            char symbol = GetSymbol(entity.Type);
            ConsoleColor color = entity.Id == SelectedEntityId ? _scheme.SelectedEntity : GetColor(entity.Type);
            SetCell(x, y, symbol, color);
        }
    }

    private void DrawCorpses(WorldSnapshot snapshot)
    {
        for (int i = 0; i < snapshot.Corpses.Length; i++)
        {
            SnapshotCorpse corpse = snapshot.Corpses[i];
            if (!IsInsideWorld(corpse.Position, snapshot.WorldSize)) { continue; }
            if (!TryProjectToScreen(corpse.Position, out int x, out int y)) { continue; }
            SetCell(x, y, 'X', _scheme.Corpse);
        }
    }

    private static bool IsInsideWorld(Vector2 position, Vector2 worldSize)
    {
        if (worldSize.X <= 0f || worldSize.Y <= 0f) { return false; }
        return position.X >= 0f && position.X <= worldSize.X
            && position.Y >= 0f && position.Y <= worldSize.Y;
    }

    private void DrawGrid(WorldSnapshot snapshot)
    {
        float cellSize = snapshot.GridCellSize;
        if (cellSize <= 0f) { return; }

        float maxX = _cameraMinX + _cameraViewWidth;
        float maxY = _cameraMinY + _cameraViewHeight;

        int startGx = Math.Max(1, (int)MathF.Floor(_cameraMinX / cellSize) + 1);
        int endGx = Math.Max(startGx, (int)MathF.Ceiling(maxX / cellSize));

        for (int gx = startGx; gx < endGx; gx++)
        {
            float wx = gx * cellSize;
            if (!TryProjectToScreen(new Vector2(wx, _cameraMinY), out int sx, out _)) { continue; }
            for (int sy = _viewportTop; sy < _viewportBottomExclusive; sy++)
            {
                SetCell(sx, sy, '|', _scheme.Grid);
            }
        }

        int startGy = Math.Max(1, (int)MathF.Floor(_cameraMinY / cellSize) + 1);
        int endGy = Math.Max(startGy, (int)MathF.Ceiling(maxY / cellSize));

        for (int gy = startGy; gy < endGy; gy++)
        {
            float wy = gy * cellSize;
            if (!TryProjectToScreen(new Vector2(_cameraMinX, wy), out _, out int sy)) { continue; }
            for (int sx = 0; sx < _width; sx++)
            {
                SetCell(sx, sy, '-', _scheme.Grid);
            }
        }

        for (int gx = startGx; gx < endGx; gx++)
        {
            float wx = gx * cellSize;
            for (int gy = startGy; gy < endGy; gy++)
            {
                float wy = gy * cellSize;
                if (TryProjectToScreen(new Vector2(wx, wy), out int sx, out int sy))
                {
                    SetCell(sx, sy, '+', _scheme.Grid);
                }
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
        x = WriteText(x, y, $"Cam:{_cameraCenterX:0.0},{_cameraCenterY:0.0} ", _scheme.Hud);
        x = WriteText(x, y, $"Z:{_zoom:0.00} ", _scheme.Hud);
        x = WriteText(x, y, FollowEntityId.HasValue ? "Follow:ON " : "Follow:OFF ", _scheme.Hud);
        x = WriteText(x, y, $"H:{snapshot.Stats.Herbivores} C:{snapshot.Stats.Carnivores} P:{snapshot.Stats.Plants} S:{snapshot.Stats.Scavengers} ", _scheme.Hud);
        x = WriteText(x, y, $"E:{snapshot.Stats.TotalEnergy:0.0} ", _scheme.Hud);
        x = WriteText(x, y, $"AvgAge:{snapshot.Stats.AverageLifespan:0.0}s ", _scheme.Hud);

        x = WriteText(x, y, "HP ", _scheme.Hud);
        x = WriteText(x, y, $"L:{snapshot.Stats.HealthLow} ", _scheme.HealthLow);
        x = WriteText(x, y, $"M:{snapshot.Stats.HealthMedium} ", _scheme.HealthMedium);
        x = WriteText(x, y, $"H:{snapshot.Stats.HealthHigh} ", _scheme.HealthHigh);

        if (TryGetSelectedEntity(snapshot, out SnapshotEntity selected))
        {
            WriteText(x, y, $"Sel:{selected.Type} {selected.Id.ToString()[..6]}", _scheme.SelectedEntity);
        }
        else if (SelectedEntityId.HasValue)
        {
            WriteText(x, y, "Sel:<none>", _scheme.SelectedEntity);
        }
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

    private bool TryProjectToScreen(Vector2 worldPos, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (_cameraViewWidth <= 0f || _cameraViewHeight <= 0f) { return false; }

        float tx = (worldPos.X - _cameraMinX) / _cameraViewWidth;
        float ty = (worldPos.Y - _cameraMinY) / _cameraViewHeight;
        if (tx < 0f || tx > 1f || ty < 0f || ty > 1f) { return false; }

        x = Math.Clamp((int)(tx * (_width - 1)), 0, _width - 1);
        y = _viewportTop + Math.Clamp((int)(ty * (_drawableHeight - 1)), 0, _drawableHeight - 1);
        return true;
    }

    private static char GetSymbol(EntityType type)
    {
        return type switch
        {
            EntityType.Herbivore => 'H',
            EntityType.Carnivore => 'C',
            EntityType.Plant => '*',
            EntityType.Scavenger => 'S',
            _ => '?'
        };
    }

    private ConsoleColor GetColor(EntityType type)
    {
        return type switch
        {
            EntityType.Herbivore => _scheme.Herbivore,
            EntityType.Carnivore => _scheme.Carnivore,
            EntityType.Plant => _scheme.Plant,
            EntityType.Scavenger => _scheme.Scavenger,
            _ => _scheme.Default
        };
    }

    private bool TryGetEntityPosition(WorldSnapshot snapshot, Guid id, out Vector2 position)
    {
        for (int i = 0; i < snapshot.Entities.Length; i++)
        {
            if (snapshot.Entities[i].Id == id && snapshot.Entities[i].IsAlive)
            {
                position = snapshot.Entities[i].Position;
                return true;
            }
        }

        position = Vector2.Zero;
        return false;
    }

    private bool TryGetSelectedEntity(WorldSnapshot snapshot, out SnapshotEntity selected)
    {
        if (!SelectedEntityId.HasValue)
        {
            selected = default;
            return false;
        }

        for (int i = 0; i < snapshot.Entities.Length; i++)
        {
            if (snapshot.Entities[i].Id == SelectedEntityId.Value && snapshot.Entities[i].IsAlive)
            {
                selected = snapshot.Entities[i];
                return true;
            }
        }

        selected = default;
        return false;
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
}
