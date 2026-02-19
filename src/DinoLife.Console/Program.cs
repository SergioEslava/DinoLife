using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Simulation;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using DinoLife.Persistence;
using DinoLife.Rendering;
using DinoLife.Rendering.Terminal;
using DinoLife.Cli.Configuration;
using DinoLife.Cli.Tuning;
using System.Diagnostics;

namespace DinoLife.Cli;

/// <summary>
/// Console entry point that wires input handling to the simulation engine.
/// </summary>
public sealed class Program
{
    private static readonly float[] SpeedLevels = [0.25f, 0.5f, 1f, 2f, 4f];
    private static readonly ParameterEntry[] TuningEntries =
    [
        new("Movement", "Herbivore speed", 0.1f, 0.2f, 20f, p => p.HerbivoreSpeed, (p, v) => p.HerbivoreSpeed = v),
        new("Movement", "Carnivore speed", 0.1f, 0.2f, 20f, p => p.CarnivoreSpeed, (p, v) => p.CarnivoreSpeed = v),
        new("Movement", "Scavenger speed", 0.1f, 0.2f, 20f, p => p.ScavengerSpeed, (p, v) => p.ScavengerSpeed = v),
        new("Metabolism", "Herbivore hunger", 0.05f, 0.05f, 10f, p => p.HerbivoreHungerRate, (p, v) => p.HerbivoreHungerRate = v),
        new("Metabolism", "Carnivore hunger", 0.05f, 0.05f, 10f, p => p.CarnivoreHungerRate, (p, v) => p.CarnivoreHungerRate = v),
        new("Metabolism", "Scavenger hunger", 0.05f, 0.05f, 10f, p => p.ScavengerHungerRate, (p, v) => p.ScavengerHungerRate = v),
        new("Reproduction", "Herbivore threshold", 1f, 1f, 200f, p => p.HerbivoreReproductionThreshold, (p, v) => p.HerbivoreReproductionThreshold = v),
        new("Reproduction", "Carnivore threshold", 1f, 1f, 200f, p => p.CarnivoreReproductionThreshold, (p, v) => p.CarnivoreReproductionThreshold = v),
        new("Reproduction", "Scavenger threshold", 1f, 1f, 200f, p => p.ScavengerReproductionThreshold, (p, v) => p.ScavengerReproductionThreshold = v),
        new("Detection", "Herbivore radius", 0.5f, 1f, 100f, p => p.HerbivoreDetectionRadius, (p, v) => p.HerbivoreDetectionRadius = v),
        new("Detection", "Carnivore radius", 0.5f, 1f, 100f, p => p.CarnivoreDetectionRadius, (p, v) => p.CarnivoreDetectionRadius = v),
        new("Detection", "Scavenger radius", 0.5f, 1f, 100f, p => p.ScavengerDetectionRadius, (p, v) => p.ScavengerDetectionRadius = v),
        new("Growth", "Plant growth rate", 0.05f, 0.01f, 5f, p => p.PlantGrowthRate, (p, v) => p.PlantGrowthRate = v),
        new("Growth", "Plant respawn time", 0.5f, 1f, 200f, p => p.PlantRespawnTime, (p, v) => p.PlantRespawnTime = v)
    ];

    private bool _isExiting;
    private bool _tickOnceRequested;
    private SimulationEngine? _simulation;
    private RendererMode _rendererMode = RendererMode.Legacy;
    private readonly bool _rendererOverridden;
    private IInteractiveRenderer? _renderer;
    private TerminalGuiRenderer? _tuiRenderer;
    private Planet? _world;
    private bool _showGrid;
    private bool _showPerformanceOverlay;
    private bool _showHelp;
    private bool _followEntity;
    private int _selectedEntitySlot = -1;
    private int _speedIndex = 2;
    private int _targetFrameMs = 16;
    private int _autosaveEveryTicks = 300;
    private string _saveDirectory = "saves";
    private string _tuningDirectory = "tuning";
    private double _tickAccumulator;
    private long _lastLoopTimestamp;
    private string? _statusMessage;
    private readonly IWorldSerializer _serializer = new JsonWorldSerializer();
    private readonly SaveBrowser _saveBrowser = new SaveBrowser(new JsonWorldSerializer());
    private IReadOnlyList<SaveMetadata> _saves = Array.Empty<SaveMetadata>();
    private int _selectedSaveIndex = -1;
    private int _lastAutosaveTick;
    private int _menuSelection;
    private MenuContext _menuContext = MenuContext.Main;
    private readonly InputRouter _inputRouter = new();
    private SimulationTuningProfile _tuningProfile = TuningPresets.Balanced();
    private readonly TuningFileStore _tuningFileStore = new();
    private readonly ConfigManager _configManager = new(Environment.CurrentDirectory);
    private AppSettingsConfig _appSettings = new();
    private WorldConfig _worldConfig = new();

    public Program(string[] args)
    {
        RendererMode? rendererMode = RendererModeParser.ParseOptional(args);
        if (rendererMode.HasValue)
        {
            _rendererOverridden = true;
            _rendererMode = rendererMode.Value;
        }
    }

    /// <summary>
    /// Application entry point.
    /// </summary>
    public static void Main(string[] args)
    {
        new Program(args).Run();
    }

    private void LoadConfiguration()
    {
        bool loaded = _configManager.LoadInitial(out _appSettings, out _worldConfig, out string message);
        ApplyAppSettings(_appSettings);
        ApplyWorldConfig(_worldConfig, applyToCurrentWorld: false);

        if (!_rendererOverridden)
        {
            _rendererMode = ParseRenderer(_appSettings.RendererDefault);
        }

        if (!loaded && !string.IsNullOrWhiteSpace(message))
        {
            SetStatus($"Config warning: {message}");
        }
    }

    private void TryHotReloadConfigs()
    {
        bool changed = _configManager.TryHotReload(
            _appSettings.HotReloadEnabled,
            out AppSettingsConfig? reloadedApp,
            out WorldConfig? reloadedWorld,
            out string message);

        if (!changed && string.IsNullOrWhiteSpace(message)) { return; }

        if (reloadedApp is not null)
        {
            _appSettings = reloadedApp;
            ApplyAppSettings(_appSettings);
        }

        if (reloadedWorld is not null)
        {
            _worldConfig = reloadedWorld;
            ApplyWorldConfig(_worldConfig, applyToCurrentWorld: true);
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            SetStatus(message);
        }
    }

    private void ApplyAppSettings(AppSettingsConfig appSettings)
    {
        _targetFrameMs = Math.Clamp(appSettings.TargetFrameMs, 1, 1000);
        _autosaveEveryTicks = Math.Clamp(appSettings.AutosaveEveryTicks, 1, 1_000_000);
        _saveDirectory = string.IsNullOrWhiteSpace(appSettings.SaveDirectory) ? "saves" : appSettings.SaveDirectory;
        _tuningDirectory = string.IsNullOrWhiteSpace(appSettings.TuningDirectory) ? "tuning" : appSettings.TuningDirectory;
    }

    private void ApplyWorldConfig(WorldConfig worldConfig, bool applyToCurrentWorld)
    {
        _tuningProfile = worldConfig.TuningProfile?.Clone() ?? TuningPresets.Balanced();

        if (!applyToCurrentWorld || _world is null) { return; }

        _world.WorldSize = new Vector2(worldConfig.WorldWidth, worldConfig.WorldHeight);
        ApplyTuningToWorld();
    }

    private static RendererMode ParseRenderer(string value)
    {
        return string.Equals(value, "tui", StringComparison.OrdinalIgnoreCase)
            ? RendererMode.Tui
            : RendererMode.Legacy;
    }

    private void Run()
    {
        LoadConfiguration();
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
                TryHotReloadConfigs();
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
                    _tuiRenderer.MenuTitle = _menuContext == MenuContext.Main
                        ? "COMMAND MENU"
                        : $"PARAMETER TUNING [{_tuningProfile.ProfileName}]";
                }
                _renderer.Render(snapshot);

                Thread.Sleep(_targetFrameMs);
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
            if (_inputRouter.TryRouteMenuCommand(command.Type, out MenuCommand menuCommand))
            {
                HandleMenuCommand(menuCommand);
                continue;
            }

            if (_inputRouter.BlocksWorldCommand(command.Type))
            {
                continue;
            }

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
                    Exit();
                    break;
                case InputCommandType.TogglePerformanceOverlay:
                    TogglePerformanceOverlay();
                    break;
                case InputCommandType.ToggleHelp:
                    ToggleHelp();
                    break;
                case InputCommandType.PanLeft:
                    _renderer?.Pan(-1f, 0f);
                    break;
                case InputCommandType.PanRight:
                    _renderer?.Pan(1f, 0f);
                    break;
                case InputCommandType.PanUp:
                    _renderer?.Pan(0f, -1f);
                    break;
                case InputCommandType.PanDown:
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

    private void HandleMenuCommand(MenuCommand command)
    {
        switch (command)
        {
            case MenuCommand.MoveUp:
                MoveMenuSelection(-1);
                break;
            case MenuCommand.MoveDown:
                MoveMenuSelection(1);
                break;
            case MenuCommand.MoveLeft:
                AdjustMenuSelectionValue(-1f);
                break;
            case MenuCommand.MoveRight:
                AdjustMenuSelectionValue(1f);
                break;
            case MenuCommand.Activate:
                ActivateMenuSelection();
                break;
            case MenuCommand.Close:
                ToggleMenu();
                break;
        }
    }

    private void ToggleMenu()
    {
        if (_tuiRenderer is null) { return; }

        _tuiRenderer.ShowMenuOverlay = !_tuiRenderer.ShowMenuOverlay;
        if (_tuiRenderer.ShowMenuOverlay)
        {
            _menuContext = MenuContext.Main;
        }
        _inputRouter.SetMenuOpen(_tuiRenderer.ShowMenuOverlay);
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

    private void AdjustMenuSelectionValue(float direction)
    {
        if (_menuContext != MenuContext.Tuning) { return; }

        int paramCount = TuningEntries.Length;
        if (_menuSelection < 0 || _menuSelection >= paramCount) { return; }

        ParameterEntry entry = TuningEntries[_menuSelection];
        float current = entry.Getter(_tuningProfile);
        float next = Math.Clamp(current + (entry.Step * direction), entry.Min, entry.Max);
        if (Math.Abs(next - current) <= 0.0001f) { return; }

        entry.Setter(_tuningProfile, next);
        ApplyTuningToWorld();
        SetStatus($"Tuning updated: {entry.Label} = {next:0.##}");
    }

    private void ActivateMenuSelection()
    {
        if (_tuiRenderer is null || !_tuiRenderer.ShowMenuOverlay) { return; }

        if (_menuContext == MenuContext.Tuning)
        {
            ActivateTuningSelection();
            return;
        }

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
                OpenTuningMenu();
                break;
            case 5:
                TogglePerformanceOverlay();
                break;
            case 6:
                ToggleHelp();
                break;
            case 7:
                ToggleGrid();
                break;
            case 8:
                ToggleFollowSelected();
                break;
            case 9:
                _renderer?.ResetCamera();
                break;
            case 10:
                ResetSimulation();
                break;
            case 11:
                Exit();
                break;
        }
    }

    private void OpenTuningMenu()
    {
        _menuContext = MenuContext.Tuning;
        _menuSelection = ClampMenuSelection(0, BuildMenuItems().Length);
        _tuiRenderer?.RequestFullRedraw();
    }

    private void ActivateTuningSelection()
    {
        int paramCount = TuningEntries.Length;
        int balancedIndex = paramCount;
        int chaoticIndex = paramCount + 1;
        int stableIndex = paramCount + 2;
        int exportIndex = paramCount + 3;
        int importIndex = paramCount + 4;
        int backIndex = paramCount + 5;

        if (_menuSelection >= 0 && _menuSelection < paramCount)
        {
            AdjustMenuSelectionValue(+1f);
            return;
        }

        if (_menuSelection == balancedIndex)
        {
            _tuningProfile = TuningPresets.Balanced();
            ApplyTuningToWorld();
            SetStatus("Preset applied: Balanced");
            return;
        }

        if (_menuSelection == chaoticIndex)
        {
            _tuningProfile = TuningPresets.Chaotic();
            ApplyTuningToWorld();
            SetStatus("Preset applied: Chaotic");
            return;
        }

        if (_menuSelection == stableIndex)
        {
            _tuningProfile = TuningPresets.Stable();
            ApplyTuningToWorld();
            SetStatus("Preset applied: Stable");
            return;
        }

        if (_menuSelection == exportIndex)
        {
            string path = _tuningFileStore.Export(_tuningDirectory, _tuningProfile);
            SetStatus($"Params exported: {Path.GetFileName(path)}");
            return;
        }

        if (_menuSelection == importIndex)
        {
            if (!_tuningFileStore.TryImportLatest(_tuningDirectory, out SimulationTuningProfile imported, out string path, out string error))
            {
                SetStatus(error);
                return;
            }

            _tuningProfile = imported;
            ApplyTuningToWorld();
            SetStatus($"Params imported: {Path.GetFileName(path)}");
            return;
        }

        if (_menuSelection == backIndex)
        {
            _menuContext = MenuContext.Main;
            _menuSelection = 0;
            _tuiRenderer?.RequestFullRedraw();
        }
    }

    private void ApplyTuningToWorld()
    {
        if (_world is null) { return; }
        WorldTuningApplier.Apply(_world, _tuningProfile);
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
        string path = BuildManualSavePath(_saveDirectory);
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
        ApplyTuningToWorld();
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
            WorldSize = new Vector2(_worldConfig.WorldWidth, _worldConfig.WorldHeight)
        };
        SeedWorld(world, _worldConfig, _tuningProfile);
        world.DebugDrawGrid = _showGrid;
        _world = world;
        _simulation = FactorySimulation.GenerateDefaultSimulation(world);
        ApplyTuningToWorld();
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
        string tuning = $"Preset:{_tuningProfile.ProfileName}";
        string baseText = _simulation?.IsStopped == true ? $"{speed} Paused {mode} {tuning}" : $"{speed} {mode} {tuning}";
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
        if (_world.Tick - _lastAutosaveTick < _autosaveEveryTicks) { return; }

        string path = BuildAutosavePath(_world.Tick);
        _serializer.Save(path, _world);
        _lastAutosaveTick = _world.Tick;
        RefreshSaveBrowser(selectPath: path);
        SetStatus($"Autosaved: {Path.GetFileName(path)}");
    }

    private void RefreshSaveBrowser(string? selectPath = null)
    {
        _saves = _saveBrowser.ListSaves(_saveDirectory);
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

    private static string BuildManualSavePath(string saveDirectory)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(saveDirectory, $"manual-{timestamp}.json");
    }

    private string BuildAutosavePath(int tick)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(_saveDirectory, $"auto-{tick:D8}-{timestamp}.json");
    }

    private static void SeedWorld(Planet world, WorldConfig config, SimulationTuningProfile tuningProfile)
    {
        Random rng = new Random(config.RandomSeed);

        CreatePlantEntities(world, config.InitialPlants, tuningProfile, rng);
        CreateEntities(world, EntityType.Herbivore, config.InitialHerbivores, tuningProfile, rng, hasMovement: true);
        CreateEntities(world, EntityType.Carnivore, config.InitialCarnivores, tuningProfile, rng, hasMovement: true);
        CreateEntities(world, EntityType.Scavenger, config.InitialScavengers, tuningProfile, rng, hasMovement: true);
    }

    private static void CreatePlantEntities(Planet world, int count, SimulationTuningProfile tuningProfile, Random rng)
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
                GrowthRate = tuningProfile.PlantGrowthRate,
                RespawnTime = tuningProfile.PlantRespawnTime,
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
        SimulationTuningProfile tuningProfile,
        Random rng,
        bool hasMovement)
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
                float adjustedSpeed = type switch
                {
                    EntityType.Herbivore => tuningProfile.HerbivoreSpeed,
                    EntityType.Carnivore => tuningProfile.CarnivoreSpeed,
                    EntityType.Scavenger => tuningProfile.ScavengerSpeed,
                    _ => 0f
                };
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
                    EntityType.Herbivore => tuningProfile.HerbivoreHungerRate,
                    EntityType.Carnivore => tuningProfile.CarnivoreHungerRate,
                    EntityType.Scavenger => tuningProfile.ScavengerHungerRate,
                    _ => 1.0f
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
                        DetectionRadius = tuningProfile.ScavengerDetectionRadius,
                        EatRadius = 2.5f,
                        EatingDuration = 1f
                    };
                }
                else
                {
                    world.Diets[slot] = new Diet
                    {
                        FoodType = type == EntityType.Carnivore ? FoodType.Herbivore : FoodType.Plant,
                        DetectionRadius = type == EntityType.Carnivore
                            ? tuningProfile.CarnivoreDetectionRadius
                            : tuningProfile.HerbivoreDetectionRadius,
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
                        ReproductionThreshold = tuningProfile.CarnivoreReproductionThreshold,
                        ReproductionCost = 25f,
                        Cooldown = 0f,
                        CooldownDuration = 40f
                    };
                }
                else if (type == EntityType.Scavenger)
                {
                    world.Reproductions[slot] = new Reproduction
                    {
                        ReproductionThreshold = tuningProfile.ScavengerReproductionThreshold,
                        ReproductionCost = 20f,
                        Cooldown = 0f,
                        CooldownDuration = 60f
                    };
                }
                else
                {
                    world.Reproductions[slot] = new Reproduction
                    {
                        ReproductionThreshold = tuningProfile.HerbivoreReproductionThreshold,
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
        if (_menuContext == MenuContext.Tuning)
        {
            return BuildTuningMenuItems();
        }

        return
        [
            _simulation?.IsStopped == true ? "Resume simulation" : "Pause simulation",
            "Step one tick",
            "Save world",
            "Load selected save",
            "Parameter tuning...",
            _showPerformanceOverlay ? "Hide performance panel" : "Show performance panel",
            _showHelp ? "Hide help panel" : "Show help panel",
            _showGrid ? "Hide grid" : "Show grid",
            _followEntity ? "Disable follow selected" : "Enable follow selected",
            "Reset camera",
            "Reset simulation",
            "Quit"
        ];
    }

    private string[] BuildTuningMenuItems()
    {
        string[] items = new string[TuningEntries.Length + 6];
        for (int i = 0; i < TuningEntries.Length; i++)
        {
            ParameterEntry entry = TuningEntries[i];
            float value = entry.Getter(_tuningProfile);
            items[i] = $"{entry.Category} | {entry.Label}: {value:0.##}";
        }

        int baseIndex = TuningEntries.Length;
        items[baseIndex] = "Preset: Balanced";
        items[baseIndex + 1] = "Preset: Chaotic";
        items[baseIndex + 2] = "Preset: Stable";
        items[baseIndex + 3] = "Export params to file";
        items[baseIndex + 4] = "Import latest params file";
        items[baseIndex + 5] = "Back to command menu";
        return items;
    }

    private static int ClampMenuSelection(int currentSelection, int itemCount)
    {
        if (itemCount <= 0) { return -1; }
        if (currentSelection < 0) { return 0; }
        if (currentSelection >= itemCount) { return itemCount - 1; }
        return currentSelection;
    }

    private enum MenuContext
    {
        Main,
        Tuning
    }

    private readonly record struct ParameterEntry(
        string Category,
        string Label,
        float Step,
        float Min,
        float Max,
        Func<SimulationTuningProfile, float> Getter,
        Action<SimulationTuningProfile, float> Setter);
}
