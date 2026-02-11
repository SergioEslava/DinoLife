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
    private const string SavePath = "savegame.json";
    private static readonly float[] SpeedLevels = [0.25f, 0.5f, 1f, 2f, 4f];

    private bool _isExiting;
    private bool _tickOnceRequested;
    private SimulationEngine? _simulation;
    private TerminalRenderer? _renderer;
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

    /// <summary>
    /// Application entry point.
    /// </summary>
    public static void Main(string[] args)
    {
        new Program().Run();
    }

    private void Run()
    {
        ResetSimulation();

        _renderer = new TerminalRenderer
        {
            ShowPerformanceOverlay = _showPerformanceOverlay
        };
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
        WorldStatePersistence.Save(SavePath, _world);
        SetStatus($"Saved: {SavePath}");
    }

    private void LoadState()
    {
        if (!WorldStatePersistence.TryLoad(SavePath, out Planet loaded, out string error))
        {
            SetStatus(error);
            return;
        }

        _world = loaded;
        _simulation = FactorySimulation.GenerateDefaultSimulation(_world);
        _showGrid = _world.DebugDrawGrid;
        _tickAccumulator = 0d;
        _lastLoopTimestamp = Stopwatch.GetTimestamp();
        SetStatus($"Loaded: {SavePath}");
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
        string baseText = _simulation?.IsStopped == true ? $"{speed} Paused" : speed;
        if (string.IsNullOrWhiteSpace(_statusMessage)) { return baseText; }
        return $"{baseText} | {_statusMessage}";
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
}
