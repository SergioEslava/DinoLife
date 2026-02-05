using BenchmarkDotNet.Attributes;
using DinoLife.Core.World;
using DinoLife.Core.Systems;

namespace DinoLife.Benchmarks;

[MemoryDiagnoser]
public class SimulationEngineBenchmark
{
    private SimulationEngine _engineEmpty = null!;
    private SimulationEngine _engineWithSystems = null!;

    [GlobalSetup]
    public void Setup()
    {
        _engineEmpty = new SimulationEngine(new Planet(), new StopwatchClock(), Array.Empty<ISystem>());

        // Engine with dummy systems
        var systems = new ISystem[]
        {
            new TestSystem((_, _) => { }),
            new TestSystem((_, _) => { }),
            new TestSystem((_, _) => { })
        };
        _engineWithSystems = new SimulationEngine(new Planet(), new StopwatchClock(), systems);
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
        _engineEmpty.Step();
    }

    [Benchmark]
    public void SimulationEngine_Step_Performance_WithSystems()
    {
        _engineWithSystems.Step();
    }
}

// Helper for dummy systems
internal sealed class TestSystem : ISystem
{
    private readonly Action<Planet, double> _onUpdate;
    public TestSystem(Action<Planet, double> onUpdate) => _onUpdate = onUpdate;
    public void Update(Planet world, double deltaTime) => _onUpdate(world, deltaTime);
}
