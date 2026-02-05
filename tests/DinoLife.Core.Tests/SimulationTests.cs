using DinoLife.Core.Systems;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Simulation;

public class SimulationEngineTests
{
    [Fact]
    public void SimulationEngine_TickOnce_IncrementsWorldTick_WhenCalled()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        engine.TickOnce();

        world.Tick.Should().Be(1);
    }

    [Fact]
    public void SimulationEngine_Step_Advances60Ticks_WhenOneSecondElapsed()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        clock.Advance(1.0);
        engine.Step();

        world.Tick.Should().Be(60);
    }

    [Fact]
    public void SimulationEngine_TickOnce_CallsSystemsInOrder_WhenMultipleSystemsPresent()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var calls = new List<int>();
        var systems = new ISystem[]
        {
            new TestSystem((_, _) => calls.Add(1)),
            new TestSystem((_, _) => calls.Add(2)),
            new TestSystem((_, _) => calls.Add(3))
        };

        var engine = new SimulationEngine(world, clock, systems);
        engine.TickOnce();

        calls.Should().ContainInOrder(1, 2, 3);
    }

    [Fact]
    public void SimulationEngine_TickOnce_CallsAllSystemsExactlyOnce_PerTick()
    {
        int callCount = 0;
        var systems = new ISystem[]
        {
            new TestSystem((_, _) => callCount++)
        };
        var engine = new SimulationEngine(new Planet(), new FakeClock(), systems);

        engine.TickOnce();
        engine.TickOnce();

        callCount.Should().Be(2);
    }

    [Fact]
    public void SimulationEngine_TickOnce_PassesFixedDeltaTimeToSystems_WhenCalled()
    {
        double receivedDelta = 0;
        var systems = new ISystem[]
        {
            new TestSystem((_, dt) => receivedDelta = dt)
        };
        var engine = new SimulationEngine(new Planet(), new FakeClock(), systems);
        engine.TickOnce();

        receivedDelta.Should().Be(SimulationEngine.TickTime);
    }

    [Fact]
    public void SimulationEngine_Step_AccumulatesPartialTimeWithoutTicking_WhenLessThanTickTimeElapsed()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        // First half-tick -> should not tick
        clock.Advance(SimulationEngine.TickTime / 2);
        engine.Step();
        world.Tick.Should().Be(0);

        // Second half-tick -> completes tick
        clock.Advance(SimulationEngine.TickTime / 2);
        engine.Step();
        world.Tick.Should().Be(1);
    }

    [Fact]
    public void SimulationEngine_Step_RunsMultipleTicks_WhenFrameTimeExceedsTickTime()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        clock.Advance(SimulationEngine.TickTime * 3);
        engine.Step();

        world.Tick.Should().Be(3);
    }

    [Fact]
    public void SimulationEngine_Step_RespectsSystemOrder_WhenMultipleTicksAndSystemsPresent()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var calls = new List<int>();
        var systems = new ISystem[]
        {
            new TestSystem((_, _) => calls.Add(1)),
            new TestSystem((_, _) => calls.Add(2)),
            new TestSystem((_, _) => calls.Add(3))
        };
        var engine = new SimulationEngine(world, clock, systems);

        clock.Advance(SimulationEngine.TickTime * 2);
        engine.Step();

        calls.Should().Equal(1,2,3,1,2,3);
    }
}

// =======================
// Helpers
// =======================
internal sealed class FakeClock : IClock
{
    private double _now;
    public double Now => _now;
    public void Advance(double seconds) => _now += seconds;
}

internal sealed class TestSystem : ISystem
{
    private readonly Action<Planet, double> _onUpdate;
    public TestSystem(Action<Planet, double> onUpdate) => _onUpdate = onUpdate;
    public void Update(Planet world, double deltaTime) => _onUpdate(world, deltaTime);
}
