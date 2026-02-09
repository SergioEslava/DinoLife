using DinoLife.Core.Simulation;
using DinoLife.Core.World;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;
using DinoLife.Rendering;
using DinoLife.Rendering.Terminal;
using System;
using System.Threading;

namespace DinoLife.Cli;

/// <summary>
/// Console entry point that wires input handling to the simulation engine.
/// </summary>
public class Program
{
    private bool _isExiting;
    private bool _tickOnceRequested;
    private SimulationEngine? _simulation;

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        new Program().Run();
    }

    /// <summary>
    /// Main loop that advances the simulation and processes input.
    /// </summary>
    private void Run()
    {
        Planet world = new Planet();
        world.WorldSize = new Vector2(120f, 40f);
        SeedWorld(world);
        _simulation = FactorySimulation.GenerateDefaultSimulation(world);

        IRenderer renderer = new TerminalRenderer();
        renderer.Initialize();

        InputHandler input = new InputHandler();

        try
        {
            while (!_isExiting)
            {
                input.Poll(this);

                if (_tickOnceRequested)
                {
                    _simulation!.TickOnce();
                    _tickOnceRequested = false;
                }
                else if (!_simulation!.IsStopped)
                {
                    _simulation!.Step();
                }

                WorldSnapshot snapshot = WorldSnapshotBuilder.Build(world);
                renderer.Render(snapshot);

                Thread.Sleep(16); // ~60fps
            }
        }
        finally
        {
            renderer.Shutdown();
        }
    }

    private static void SeedWorld(Planet world)
    {
        var rng = new Random(1234);

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
            if (type == EntityType.Herbivore || type == EntityType.Carnivore || type == EntityType.Scavenger)
            {
                flags |= ComponentFlags.Diet;
            }
            if (type == EntityType.Herbivore || type == EntityType.Carnivore || type == EntityType.Scavenger)
            {
                flags |= ComponentFlags.Reproduction;
            }
            if (type != EntityType.Plant)
            {
                flags |= ComponentFlags.Lifespan;
            }

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
                world.Movements[slot] = new Movement
                {
                    Velocity = Vector2.Zero,
                    Speed = speed,
                    Acceleration = speed * 2f
                };
            }

            if (flags.HasFlag(ComponentFlags.Metabolism))
            {
                float hungerRate = type == EntityType.Scavenger ? 0.8f : 1.0f;
                world.Metabolisms[slot] = new Metabolism
                {
                    Energy = 50f,
                    MaxEnergy = 100f,
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
                        DetectionRadius = 25f,
                        EatRadius = 1.5f,
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
                        ReproductionThreshold = 120f,
                        ReproductionCost = 50f,
                        Cooldown = 0f,
                        CooldownDuration = 120f
                    };
                }
                else if (type == EntityType.Scavenger)
                {
                    world.Reproductions[slot] = new Reproduction
                    {
                        ReproductionThreshold = 60f,
                        ReproductionCost = 30f,
                        Cooldown = 0f,
                        CooldownDuration = 100f
                    };
                }
                else
                {
                    world.Reproductions[slot] = new Reproduction
                    {
                        ReproductionThreshold = 80f,
                        ReproductionCost = 30f,
                        Cooldown = 0f,
                        CooldownDuration = 90f
                    };
                }
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
                    Age = 0f,
                    MaxAge = maxAge
                };
            }
        }
    }

    /// <summary>
    /// Request a clean exit from the main loop.
    /// </summary>
    public void Exit() => _isExiting = true;

    /// <summary>
    /// Toggle simulation pause/resume.
    /// </summary>
    public void TogglePause()
    {
        if (_simulation is null) { return; }

        if (_simulation.IsStopped) { _simulation.Resume(); }
        else { _simulation.Stop(); }
    }

    /// <summary>
    /// Request a single tick when the simulation is paused.
    /// </summary>
    public void RequestTick()
    {
        if (_simulation is not null && _simulation.IsStopped) {_tickOnceRequested = true;}
    }
}
