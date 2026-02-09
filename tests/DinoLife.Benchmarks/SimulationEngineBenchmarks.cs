using BenchmarkDotNet.Attributes;
using DinoLife.Core.World;
using DinoLife.Core.Systems;
using DinoLife.Core.Simulation;
using DinoLife.Core.Components;
using DinoLife.Core.Entities;
using DinoLife.Core.Utils;

namespace DinoLife.Benchmarks;

[MemoryDiagnoser]
public class SimulationEngineBenchmark
{
    private SimulationEngine _engineEmpty = null!;
    private SimulationEngine _engineWithSystems = null!;
    private FakeClock _clockEmpty = null!;
    private FakeClock _clockWithSystems = null!;

    [GlobalSetup]
    public void Setup()
    {
        _clockEmpty = new FakeClock();
        _engineEmpty = new SimulationEngine(new Planet(), _clockEmpty, Array.Empty<ISystem>());

        var world = new Planet { WorldSize = new Vector2(120f, 40f) };
        SeedWorld(world);

        _clockWithSystems = new FakeClock();
        var systems = new ISystem[]
        {
            new SpatialGridSystem(),
            new BehaviorSystem(),
            new MovementSystem(),
            new MetabolismSystem(),
            new PlantGrowthSystem(),
            new HuntingSystem(),
            new ReproductionSystem(),
            new DeathSystem()
        };
        _engineWithSystems = new SimulationEngine(world, _clockWithSystems, systems);
    }

    [Benchmark(Baseline = true)]
    public void SimulationEngine_TickOnce_Performance_EmptyEngine()
    {
        _engineEmpty.TickOnce();
    }

    [Benchmark]
    public void SimulationEngine_TickOnce_Performance_WithSystems()
    {
        _engineWithSystems.TickOnce();
    }

    [Benchmark]
    public void SimulationEngine_Step_Performance_EmptyEngine()
    {
        _clockEmpty.Advance(SimulationEngine.TickTime);
        _engineEmpty.Step();
    }

    [Benchmark]
    public void SimulationEngine_Step_Performance_WithSystems()
    {
        _clockWithSystems.Advance(SimulationEngine.TickTime);
        _engineWithSystems.Step();
    }

    private static void SeedWorld(Planet world)
    {
        var rng = new Random(1234);
        CreatePlants(world, count: 500, rng);
        CreateEntities(world, EntityType.Herbivore, count: 200, rng, speed: 3f);
        CreateEntities(world, EntityType.Carnivore, count: 40, rng, speed: 4f);
        CreateEntities(world, EntityType.Scavenger, count: 60, rng, speed: 2.5f);
    }

    private static void CreatePlants(Planet world, int count, Random rng)
    {
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

            float x = (float)rng.NextDouble() * world.WorldSize.X;
            float y = (float)rng.NextDouble() * world.WorldSize.Y;
            world.Transforms[slot] = new Transform(new Vector2(x, y));
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

    private static void CreateEntities(
        Planet world,
        EntityType type,
        int count,
        Random rng,
        float speed)
    {
        for (int i = 0; i < count; i++)
        {
            int slot = world.AllocateEntitySlot();

            ComponentFlags flags = ComponentFlags.Transform | ComponentFlags.Movement | ComponentFlags.Metabolism;
            if (type == EntityType.Herbivore || type == EntityType.Carnivore || type == EntityType.Scavenger)
            {
                flags |= ComponentFlags.Diet | ComponentFlags.Reproduction | ComponentFlags.Lifespan;
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
            world.Movements[slot] = new Movement
            {
                Velocity = Vector2.Zero,
                Speed = speed,
                Acceleration = speed * 2f
            };
            world.Metabolisms[slot] = new Metabolism
            {
                Energy = 50f,
                MaxEnergy = 100f,
                HungerRate = type == EntityType.Carnivore ? 1.5f : 1.0f,
                EnergyGainRate = 1.0f
            };
            world.Diets[slot] = new Diet
            {
                FoodType = type == EntityType.Carnivore ? FoodType.Herbivore
                    : type == EntityType.Scavenger ? FoodType.Corpse
                    : FoodType.Plant,
                DetectionRadius = type == EntityType.Scavenger ? 25f : 20f,
                EatRadius = 2f,
                EatingDuration = 1f
            };
            world.Reproductions[slot] = new Reproduction
            {
                ReproductionThreshold = type == EntityType.Carnivore ? 80f
                    : type == EntityType.Scavenger ? 40f
                    : 55f,
                ReproductionCost = type == EntityType.Carnivore ? 25f
                    : type == EntityType.Scavenger ? 15f
                    : 13f,
                Cooldown = 0f,
                CooldownDuration = type == EntityType.Carnivore ? 40f
                    : type == EntityType.Scavenger ? 35f
                    : 11f
            };
            world.Lifespans[slot] = new Lifespan
            {
                Age = 0f,
                MaxAge = type == EntityType.Carnivore ? 400f
                    : type == EntityType.Scavenger ? 350f
                    : 300f
            };
        }
    }
}

internal sealed class FakeClock : IClock
{
    private double _now;
    public double Now => _now;
    public void Advance(double seconds) => _now += seconds;
}
