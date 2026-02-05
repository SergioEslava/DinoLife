using DinoLife.Core.Systems;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Simulation;

public class SimulationTests
{
    
    [Fact]
    public void TickOnce_Should_Increment_World_Tick()
    {
        var world = new Planet();
        var clock = new FakeClock();

        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        engine.TickOnce();

        world.Tick.Should().Be(1);
    }

    [Fact]
    public void Step_Should_Advance_60_Ticks_Per_Second()
    {
        var world = new Planet();
        var clock = new FakeClock();

        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        clock.Advance(1.0);
        engine.Step();

        world.Tick.Should().Be(60);
    }

    [Fact]
    public void Systems_Should_Run_In_Order()
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
    public void Total_Execution_Steps()
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
    public void TickOnce_Should_Pass_Fixed_DeltaTime_To_Systems()
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
    public void Step_Should_Accumulate_Partial_Time_Without_Ticking()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        // Forwards less than a tick
        clock.Advance(SimulationEngine.TickTime / 2);
        engine.Step();

        world.Tick.Should().Be(0);

        // Forwards another half-tick -> complete tick
        clock.Advance(SimulationEngine.TickTime / 2);
        engine.Step();

        world.Tick.Should().Be(1);
    }

    [Fact]
    public void Step_Should_Run_Multiple_Ticks_When_Large_FrameTime()
    {
        var world = new Planet();
        var clock = new FakeClock();
        var engine = new SimulationEngine(world, clock, Array.Empty<ISystem>());

        // Forwards 3 ticks in one call
        clock.Advance(SimulationEngine.TickTime * 3);
        engine.Step();

        world.Tick.Should().Be(3);
    }

    [Fact]
    public void Step_Should_Respect_System_Order()
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

        // Avanza suficiente para 2 ticks
        clock.Advance(SimulationEngine.TickTime * 2);
        engine.Step();

        // Debe ejecutarse dos veces en orden
        calls.Should().Equal(1,2,3,1,2,3);
    }



}

internal sealed class FakeClock : IClock
{
    private double _now;
    public double Now => _now;

    public void Advance(double seconds)
    {
        _now += seconds;
    }
}

internal sealed class TestSystem : ISystem
{
    private readonly Action<Planet, double> _onUpdate;

    public TestSystem(Action<Planet, double> onUpdate)
    {
        _onUpdate = onUpdate;
    }

    public void Update(Planet world, double deltaTime)
    {
        _onUpdate(world, deltaTime);
    }
}