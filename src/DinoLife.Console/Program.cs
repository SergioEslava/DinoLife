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

        CreateEntities(world, EntityType.Plant, count: 60, rng, hasMovement: false, speed: 0f);
        CreateEntities(world, EntityType.Herbivore, count: 20, rng, hasMovement: true, speed: 3f);
        CreateEntities(world, EntityType.Carnivore, count: 8, rng, hasMovement: true, speed: 4f);
        CreateEntities(world, EntityType.Scavenger, count: 12, rng, hasMovement: true, speed: 2.5f);
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

            var flags = ComponentFlags.Transform;
            if (hasMovement) { flags |= ComponentFlags.Movement; }
            if (type != EntityType.Plant) { flags |= ComponentFlags.Metabolism; }
            if (type == EntityType.Herbivore || type == EntityType.Carnivore)
            {
                flags |= ComponentFlags.Diet;
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
                world.Metabolisms[slot] = new Metabolism
                {
                    Energy = 50f,
                    MaxEnergy = 100f,
                    HungerRate = 1.0f,
                    EnergyGainRate = 1.0f
                };
            }

            if (flags.HasFlag(ComponentFlags.Diet))
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
