using BenchmarkDotNet.Attributes;
using DinoLife.Core.World;
using DinoLife.Core.Systems;
using DinoLife.Core.Simulation;

namespace DinoLife.Benchmarks;

[MemoryDiagnoser]
public class SimulationEngineFullLoadBenchmark
{
    private SimulationEngine _engineWith5000Entities = null!;

    [GlobalSetup]
    public void Setup()
    {
        var planet = new Planet();

        // Allocate 5000 empty entities
        for (int i = 0; i < 5000; i++)
        {
            planet.AllocateEntitySlot();
        }

        // Dummy systems (simulate work)
        var systems = new ISystem[]
        {
            new TestSystem((_, _) => { }),
            new TestSystem((_, _) => { }),
            new TestSystem((_, _) => { })
        };

        _engineWith5000Entities = new SimulationEngine(planet, new StopwatchClock(), systems);
    }

    [Benchmark]
    public void TickOnce_5000Entities()
    {
        _engineWith5000Entities.TickOnce();
    }

    [Benchmark]
    public void Step_5000Entities()
    {
        _engineWith5000Entities.Step();
    }
}
