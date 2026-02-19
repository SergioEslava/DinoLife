using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Simulation;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using DinoLife.Persistence;
using DinoLife.Rendering;
using DinoLife.Rendering.Terminal;
using System.Diagnostics;

namespace DinoLife.Cli;

/// <summary>
/// Console entry point that wires input handling to the simulation engine.
/// </summary>
public sealed class Program
{
    private const string SaveDirectory = "saves";
    private const int AutosaveEveryTicks = 300;
    private static readonly float[] SpeedLevels = [0.25f, 0.5f, 1f, 2f, 4f];

    private bool _isExiting;
    private bool _tickOnceRequested;
    private SimulationEngine? _simulation;
    private readonly RendererMode _rendererMode;
    private IInteractiveRenderer? _renderer;
    private TerminalGuiRenderer? _tuiRenderer;
    private Planet? _world;
    private bool _showGrid;
    private bool _showPerformanceOverlay;
    private bool _showHelp;
    private bool _followEntity;
    private int _selectedEntitySlot = -1;
    private int _speedIndex = 2;
    private double _tickAccumulator;
    private long _lastLoopTimestamp;
    private string? _statusMessage;
    private readonly IWorldSerializer _serializer = new JsonWorldSerializer();
    private readonly SaveBrowser _saveBrowser = new SaveBrowser(new JsonWorldSerializer());
    private IReadOnlyList<SaveMetadata> _saves = Array.Empty<SaveMetadata>();
    private int _selectedSaveIndex = -1;
    private int _lastAutosaveTick;
    private int _menuSelection;

    public Program(string[] args)
    {
        _rendererMode = RendererModeParser.Parse(args);
    }

    /// <summary>
    /// Application entry point.
    /// </summary>
    public static void Main(string[] args)
    {
        new Program(args).Run();
    }

    private void Run()
    {
        ResetSimulation();
        RefreshSaveBrowser();

        _renderer = _rendererMode == RendererMode.Tui
            ? new TerminalGuiRenderer()
            : new TerminalRenderer();
        _renderer.ShowPerformanceOverlay = _showPerformanceOverlay;
        _tuiRenderer = _renderer as TerminalGuiRenderer;
        _renderer.Initialize();

        InputHandler input = new InputHandler();
        _lastLoopTimestamp = Stopwatch.GetTimestamp();

        try
        {
            while (!_isExiting)
            {
                input.Poll();
                ProcessInputQueue(input);
                StepSimulationBySpeed();

                if (_world is null || _renderer is null) { continue; }

                WorldSnapshot snapshot = WorldSnapshotBuilder.Build(_world);
                _renderer.ShowPerformanceOverlay = _showPerformanceOverlay;
                _renderer.ShowHelpOverlay = _showHelp;
                _renderer.StatusText = BuildStatusLine();
                _renderer.SelectedEntityId = GetSelectedEntityId();
                _renderer.FollowEntityId = _followEntity ? GetSelectedEntityId() : null;
                if (_tuiRenderer is not null)
                {
                    string[] menuItems = BuildMenuItems();
                    _tuiRenderer.MenuItems = menuItems;
                    _menuSelection = ClampMenuSelection(_menuSelection, menuItems.Length);
                    _tuiRenderer.SelectedMenuIndex = _menuSelection;
                    _tuiRenderer.MenuTitle = "COMMAND MENU";
                }
                _renderer.Render(snapshot);

                Thread.Sleep(16); // ~60 FPS render cadence
            }
        }
        finally
        {
            _renderer?.Shutdown();
        }
    }

    private void ProcessInputQueue(InputHandler input)
    {
        while (input.TryDequeue(out InputCommand command))
        {
            switch (command.Type)
            {
                case InputCommandType.TogglePause:
                    TogglePause();
                    break;
                case InputCommandType.StepOnce:
                    RequestTick();
                    break;
                case InputCommandType.SpeedUp:
                    IncreaseSpeed();
                    break;
                case InputCommandType.SpeedDown:
                    DecreaseSpeed();
                    break;
                case InputCommandType.SaveState:
                    SaveState();
                    break;
                case InputCommandType.LoadState:
                    LoadState();
                    break;
                case InputCommandType.RefreshSaveBrowser:
                    RefreshSaveBrowser();
                    break;
                case InputCommandType.ResetSimulation:
                    ResetSimulation();
                    break;
                case InputCommandType.Quit:
                    if (IsMenuOpen())
                    {
                        ToggleMenu();
                        break;
                    }

                    Exit();
                    break;
                case InputCommandType.TogglePerformanceOverlay:
                    TogglePerformanceOverlay();
                    break;
                case InputCommandType.ToggleHelp:
                    ToggleHelp();
                    break;
                case InputCommandType.PanLeft:
                    if (IsMenuOpen()) { break; }
                    _renderer?.Pan(-1f, 0f);
                    break;
                case InputCommandType.PanRight:
                    if (IsMenuOpen()) { break; }
                    _renderer?.Pan(1f, 0f);
                    break;
                case InputCommandType.PanUp:
                    if (MoveMenuSelection(-1)) { break; }
                    _renderer?.Pan(0f, -1f);
                    break;
                case InputCommandType.PanDown:
                    if (MoveMenuSelection(1)) { break; }
                    _renderer?.Pan(0f, 1f);
                    break;
                case InputCommandType.ResetCamera:
                    _renderer?.ResetCamera();
                    break;
                case InputCommandType.ToggleFollowSelected:
                    ToggleFollowSelected();
                    break;
                case InputCommandType.SelectNextEntity:
                    SelectRelativeEntity(1);
                    break;
                case InputCommandType.SelectPreviousEntity:
                    SelectRelativeEntity(-1);
                    break;
                case InputCommandType.ToggleGrid:
                    ToggleGrid();
                    break;
                case InputCommandType.BrowsePreviousSave:
                    SelectPreviousSave();
                    break;
                case InputCommandType.BrowseNextSave:
                    SelectNextSave();
                    break;
                case InputCommandType.ToggleMenu:
                    ToggleMenu();
                    break;
                case InputCommandType.MenuActivate:
                    ActivateMenuSelection();
                    break;
            }
        }
    }

    private void StepSimulationBySpeed()
    {
        if (_simulation is null) { return; }

        if (_tickOnceRequested)
        {
            _simulation.TickOnce();
            _tickOnceRequested = false;
            return;
        }

        if (_simulation.IsStopped) { return; }

        long now = Stopwatch.GetTimestamp();
        double deltaSeconds = (now - _lastLoopTimestamp) / (double)Stopwatch.Frequency;
        _lastLoopTimestamp = now;
        if (deltaSeconds <= 0d) { return; }

        double speed = SpeedLevels[_speedIndex];
        _tickAccumulator += deltaSeconds * speed;

        int maxTicksPerFrame = 240;
        int ticks = 0;
        while (_tickAccumulator >= SimulationEngine.TickTime && ticks < maxTicksPerFrame)
        {
            _simulation.TickOnce();
            _tickAccumulator -= SimulationEngine.TickTime;
            ticks++;
            TryAutosave();
        }
    }

    private void TogglePause()
    {
        if (_simulation is null) { return; }
        if (_simulation.IsStopped)
        {
            _simulation.Resume();
            SetStatus("Simulation resumed");
        }
        else
        {
            _simulation.Stop();
            SetStatus("Simulation paused");
        }
    }

    private void RequestTick()
    {
        if (_simulation is not null && _simulation.IsStopped)
        {
            _tickOnceRequested = true;
            SetStatus("Stepping one tick");
        }
    }

    private void IncreaseSpeed()
    {
        if (_speedIndex < SpeedLevels.Length - 1)
        {
            _speedIndex++;
            SetStatus($"Speed {SpeedLevels[_speedIndex]:0.##}x");
        }
    }

    private void DecreaseSpeed()
    {
        if (_speedIndex > 0)
        {
            _speedIndex--;
            SetStatus($"Speed {SpeedLevels[_speedIndex]:0.##}x");
        }
    }

    private void ToggleGrid()
    {
        _showGrid = !_showGrid;
        if (_world is not null)
        {
            _world.DebugDrawGrid = _showGrid;
        }
    }

    private void TogglePerformanceOverlay()
    {
        _showPerformanceOverlay = !_showPerformanceOverlay;
        if (_renderer is not null)
        {
            _renderer.ShowPerformanceOverlay = _showPerformanceOverlay;
        }
    }

    private void ToggleHelp()
    {
        _showHelp = !_showHelp;
    }

    private bool IsMenuOpen() => _tuiRenderer?.ShowMenuOverlay == true;

    private void ToggleMenu()
    {
        if (_tuiRenderer is null) { return; }

        _tuiRenderer.ShowMenuOverlay = !_tuiRenderer.ShowMenuOverlay;
        if (_tuiRenderer.ShowMenuOverlay)
        {
            _menuSelection = ClampMenuSelection(_menuSelection, BuildMenuItems().Length);
        }
    }

    private bool MoveMenuSelection(int delta)
    {
        if (_tuiRenderer is null || !_tuiRenderer.ShowMenuOverlay) { return false; }

        string[] items = BuildMenuItems();
        if (items.Length == 0) { return true; }

        _menuSelection = ClampMenuSelection(_menuSelection, items.Length);
        _menuSelection = (_menuSelection + delta + items.Length) % items.Length;
        return true;
    }

    private void ActivateMenuSelection()
    {
        if (_tuiRenderer is null || !_tuiRenderer.ShowMenuOverlay) { return; }

        switch (_menuSelection)
        {
            case 0:
                TogglePause();
                break;
            case 1:
                RequestTick();
                break;
            case 2:
                SaveState();
                break;
            case 3:
                LoadState();
                break;
            case 4:
                TogglePerformanceOverlay();
                break;
            case 5:
                ToggleHelp();
                break;
            case 6:
                ToggleGrid();
                break;
            case 7:
                ToggleFollowSelected();
                break;
            case 8:
                _renderer?.ResetCamera();
                break;
            case 9:
                ResetSimulation();
                break;
            case 10:
                Exit();
                break;
        }
    }

    private void ToggleFollowSelected()
    {
        if (!EnsureSelectedEntity())
        {
            _followEntity = false;
            return;
        }

        _followEntity = !_followEntity;
    }

    private void SaveState()
    {
        if (_world is null) { return; }
        string path = BuildManualSavePath();
        _serializer.Save(path, _world);
        RefreshSaveBrowser(selectPath: path);
        SetStatus($"Saved: {Path.GetFileName(path)}");
    }

    private void LoadState()
    {
        if (_saves.Count == 0)
        {
            RefreshSaveBrowser();
        }

        if (_saves.Count == 0)
        {
            SetStatus("No saves available");
            return;
        }

        if (_selectedSaveIndex < 0 || _selectedSaveIndex >= _saves.Count)
        {
            _selectedSaveIndex = 0;
        }

        SaveMetadata selected = _saves[_selectedSaveIndex];
        if (!_serializer.TryLoad(selected.Path, out Planet loaded, out string error))
        {
            SetStatus(error);
            return;
        }

        _world = loaded;
        _simulation = FactorySimulation.GenerateDefaultSimulation(_world);
        _showGrid = _world.DebugDrawGrid;
        _tickAccumulator = 0d;
        _lastLoopTimestamp = Stopwatch.GetTimestamp();
        _lastAutosaveTick = _world.Tick;
        SetStatus($"Loaded: {selected.Name}");
    }

    private void ResetSimulation()
    {
        Planet world = new Planet
        {
            WorldSize = new Vector2(120f, 40f)
        };
        SeedWorld(world);
        world.DebugDrawGrid = _showGrid;
        _world = world;
        _simulation = FactorySimulation.GenerateDefaultSimulation(world);
        _tickAccumulator = 0d;
        _lastLoopTimestamp = Stopwatch.GetTimestamp();
        _lastAutosaveTick = world.Tick;
        _selectedEntitySlot = -1;
        _followEntity = false;
        SetStatus("Simulation reset");
    }

    private void Exit() => _isExiting = true;

    private void SelectRelativeEntity(int direction)
    {
        if (_world is null) { return; }
        if (_world.EntityCount == 0)
        {
            _selectedEntitySlot = -1;
            _followEntity = false;
            return;
        }

        int start = _selectedEntitySlot;
        if (start < 0 || start >= _world.EntityCount) { start = direction > 0 ? -1 : 0; }

        int slot = start;
        for (int i = 0; i < _world.EntityCount; i++)
        {
            slot += direction;
            if (slot >= _world.EntityCount) { slot = 0; }
            if (slot < 0) { slot = _world.EntityCount - 1; }

            if (_world.Entities[slot].IsAlive)
            {
                _selectedEntitySlot = slot;
                return;
            }
        }

        _selectedEntitySlot = -1;
        _followEntity = false;
    }

    private bool EnsureSelectedEntity()
    {
        if (_world is null) { return false; }
        if (_selectedEntitySlot >= 0 && _selectedEntitySlot < _world.EntityCount && _world.Entities[_selectedEntitySlot].IsAlive)
        {
            return true;
        }

        for (int i = 0; i < _world.EntityCount; i++)
        {
            if (_world.Entities[i].IsAlive)
            {
                _selectedEntitySlot = i;
                return true;
            }
        }

        _selectedEntitySlot = -1;
        return false;
    }

    private Guid? GetSelectedEntityId()
    {
        if (!EnsureSelectedEntity() || _world is null) { return null; }
        return _world.Entities[_selectedEntitySlot].Id;
    }

    private void SetStatus(string message)
    {
        _statusMessage = message;
    }

    private string BuildStatusLine()
    {
        string speed = $"Speed:{SpeedLevels[_speedIndex]:0.##}x";
        string mode = _rendererMode == RendererMode.Tui ? "Renderer:TUI" : "Renderer:Legacy";
        string baseText = _simulation?.IsStopped == true ? $"{speed} Paused {mode}" : $"{speed} {mode}";
        string saveInfo = _saves.Count > 0 && _selectedSaveIndex >= 0 && _selectedSaveIndex < _saves.Count
            ? $"Save:{_selectedSaveIndex + 1}/{_saves.Count} {_saves[_selectedSaveIndex].Name}"
            : "Save:None";
        baseText = $"{baseText} {saveInfo}";
        if (string.IsNullOrWhiteSpace(_statusMessage)) { return baseText; }
        return $"{baseText} | {_statusMessage}";
    }

    private void TryAutosave()
    {
        if (_world is null) { return; }
        if (_world.Tick - _lastAutosaveTick < AutosaveEveryTicks) { return; }

        string path = BuildAutosavePath(_world.Tick);
        _serializer.Save(path, _world);
        _lastAutosaveTick = _world.Tick;
        RefreshSaveBrowser(selectPath: path);
        SetStatus($"Autosaved: {Path.GetFileName(path)}");
    }

    private void RefreshSaveBrowser(string? selectPath = null)
    {
        _saves = _saveBrowser.ListSaves(SaveDirectory);
        if (_saves.Count == 0)
        {
            _selectedSaveIndex = -1;
            return;
        }

        if (!string.IsNullOrWhiteSpace(selectPath))
        {
            for (int i = 0; i < _saves.Count; i++)
            {
                if (string.Equals(_saves[i].Path, selectPath, StringComparison.OrdinalIgnoreCase))
                {
                    _selectedSaveIndex = i;
                    return;
                }
            }
        }

        if (_selectedSaveIndex < 0 || _selectedSaveIndex >= _saves.Count)
        {
            _selectedSaveIndex = 0;
        }
    }

    private void SelectNextSave()
    {
        if (_saves.Count == 0)
        {
            RefreshSaveBrowser();
            if (_saves.Count == 0)
            {
                SetStatus("No saves available");
                return;
            }
        }

        _selectedSaveIndex = (_selectedSaveIndex + 1 + _saves.Count) % _saves.Count;
        SetStatus($"Selected save: {_saves[_selectedSaveIndex].Name}");
    }

    private void SelectPreviousSave()
    {
        if (_saves.Count == 0)
        {
            RefreshSaveBrowser();
            if (_saves.Count == 0)
            {
                SetStatus("No saves available");
                return;
            }
        }

        _selectedSaveIndex = (_selectedSaveIndex - 1 + _saves.Count) % _saves.Count;
        SetStatus($"Selected save: {_saves[_selectedSaveIndex].Name}");
    }

    private static string BuildManualSavePath()
    {
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(SaveDirectory, $"manual-{timestamp}.json");
    }

    private static string BuildAutosavePath(int tick)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(SaveDirectory, $"auto-{tick:D8}-{timestamp}.json");
    }

    private static void SeedWorld(Planet world)
    {
        Random rng = new Random(1234);

        CreatePlantEntities(world, count: 60, rng);
        CreateEntities(world, EntityType.Herbivore, count: 20, rng, hasMovement: true, speed: 3f);
        CreateEntities(world, EntityType.Carnivore, count: 8, rng, hasMovement: true, speed: 4f);
        CreateEntities(world, EntityType.Scavenger, count: 12, rng, hasMovement: true, speed: 2.5f);
    }

    private static void CreatePlantEntities(Planet world, int count, Random rng)
    {
        Vector2[] positions = GenerateJitteredGrid(count, world.WorldSize, rng);
        for (int i = 0; i < count; i++)
        {
            int slot = world.AllocateEntitySlot();

            world.Entities[slot] = new Entity
            {
                Id = Guid.NewGuid(),
                Type = EntityType.Plant,
                Flags = ComponentFlags.Transform | ComponentFlags.Plant,
                IsAlive = true
            };

            world.Transforms[slot] = new Transform(positions[i]);
            world.Plants[slot] = new Plant
            {
                Energy = 20f,
                MaxEnergy = 50f,
                GrowthRate = 0.5f,
                RespawnTime = 30f,
                RespawnTimer = 0f,
                IsActive = true
            };
        }
    }

    private static Vector2[] GenerateJitteredGrid(int count, Vector2 worldSize, Random rng)
    {
        int columns = (int)MathF.Ceiling(MathF.Sqrt(count));
        int rows = (int)MathF.Ceiling(count / (float)columns);
        float cellWidth = worldSize.X / columns;
        float cellHeight = worldSize.Y / rows;

        Vector2[] positions = new Vector2[count];
        int index = 0;
        for (int y = 0; y < rows && index < count; y++)
        {
            for (int x = 0; x < columns && index < count; x++)
            {
                float jitterX = (float)rng.NextDouble() * cellWidth * 0.6f;
                float jitterY = (float)rng.NextDouble() * cellHeight * 0.6f;
                float px = (x * cellWidth) + (cellWidth * 0.2f) + jitterX;
                float py = (y * cellHeight) + (cellHeight * 0.2f) + jitterY;
                positions[index++] = new Vector2(px, py);
            }
        }

        return positions;
    }

    private static void CreateEntities(
        Planet world,
        EntityType type,
        int count,
        Random rng,
        bool hasMovement,
        float speed)
    {
        for (int i = 0; i < count; i++)
        {
            int slot = world.AllocateEntitySlot();

            ComponentFlags flags = ComponentFlags.Transform;
            if (hasMovement) { flags |= ComponentFlags.Movement; }
            if (type != EntityType.Plant) { flags |= ComponentFlags.Metabolism; }
            if (type == EntityType.Herbivore || type == EntityType.Carnivore || type == EntityType.Scavenger) { flags |= ComponentFlags.Diet; }
            if (type == EntityType.Herbivore || type == EntityType.Carnivore || type == EntityType.Scavenger) { flags |= ComponentFlags.Reproduction; }
            if (type != EntityType.Plant) { flags |= ComponentFlags.Lifespan; }

            world.Entities[slot] = new Entity
            {
                Id = Guid.NewGuid(),
                Type = type,
                Flags = flags,
                IsAlive = true
            };

            float x = (float)rng.NextDouble() * world.WorldSize.X;
            float y = (float)rng.NextDouble() * world.WorldSize.Y;
            world.Transforms[slot] = new Transform(new Vector2(x, y));

            if (hasMovement)
            {
                float adjustedSpeed = type == EntityType.Scavenger ? 3.0f : speed;
                world.Movements[slot] = new Movement
                {
                    Velocity = Vector2.Zero,
                    Speed = adjustedSpeed,
                    Acceleration = adjustedSpeed * 2f
                };
            }

            if (flags.HasFlag(ComponentFlags.Metabolism))
            {
                float hungerRate = type switch
                {
                    EntityType.Carnivore => 1.5f,
                    EntityType.Scavenger => 0.6f,
                    _ => 1.3f
                };
                float maxEnergy = type == EntityType.Scavenger ? 120f : 100f;
                float startEnergy = type == EntityType.Scavenger ? 60f : 50f;
                world.Metabolisms[slot] = new Metabolism
                {
                    Energy = startEnergy,
                    MaxEnergy = maxEnergy,
                    HungerRate = hungerRate,
                    EnergyGainRate = 1.0f
                };
            }

            if (flags.HasFlag(ComponentFlags.Diet))
            {
                if (type == EntityType.Scavenger)
                {
                    world.Diets[slot] = new Diet
                    {
                        FoodType = FoodType.Corpse,
                        DetectionRadius = 35f,
                        EatRadius = 2.5f,
                        EatingDuration = 1f
                    };
                }
                else
                {
                    world.Diets[slot] = new Diet
                    {
                        FoodType = type == EntityType.Carnivore ? FoodType.Herbivore : FoodType.Plant,
                        DetectionRadius = 20f,
                        EatRadius = 2f,
                        EatingDuration = 1f
                    };
                }
            }

            if (flags.HasFlag(ComponentFlags.Reproduction))
            {
                if (type == EntityType.Carnivore)
                {
                    world.Reproductions[slot] = new Reproduction
                    {
                        ReproductionThreshold = 80f,
                        ReproductionCost = 25f,
                        Cooldown = 0f,
                        CooldownDuration = 40f
                    };
                }
                else if (type == EntityType.Scavenger)
                {
                    world.Reproductions[slot] = new Reproduction
                    {
                        ReproductionThreshold = 55f,
                        ReproductionCost = 20f,
                        Cooldown = 0f,
                        CooldownDuration = 60f
                    };
                }
                else
                {
                    world.Reproductions[slot] = new Reproduction
                    {
                        ReproductionThreshold = 55f,
                        ReproductionCost = 13f,
                        Cooldown = 0f,
                        CooldownDuration = 11f
                    };
                }

                world.Reproductions[slot].Cooldown = (float)rng.NextDouble() * world.Reproductions[slot].CooldownDuration;
            }

            if (flags.HasFlag(ComponentFlags.Lifespan))
            {
                float maxAge = type switch
                {
                    EntityType.Carnivore => 400f,
                    EntityType.Scavenger => 350f,
                    _ => 300f
                };

                world.Lifespans[slot] = new Lifespan
                {
                    Age = (float)rng.NextDouble() * maxAge,
                    MaxAge = maxAge
                };
            }
        }
    }

    private string[] BuildMenuItems()
    {
        return
        [
            _simulation?.IsStopped == true ? "Resume simulation" : "Pause simulation",
            "Step one tick",
            "Save world",
            "Load selected save",
            _showPerformanceOverlay ? "Hide performance panel" : "Show performance panel",
            _showHelp ? "Hide help panel" : "Show help panel",
            _showGrid ? "Hide grid" : "Show grid",
            _followEntity ? "Disable follow selected" : "Enable follow selected",
            "Reset camera",
            "Reset simulation",
            "Quit"
        ];
    }

    private static int ClampMenuSelection(int currentSelection, int itemCount)
    {
        if (itemCount <= 0) { return -1; }
        if (currentSelection < 0) { return 0; }
        if (currentSelection >= itemCount) { return itemCount - 1; }
        return currentSelection;
    }
}
