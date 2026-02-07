using DinoLife.Core.World;
using DinoLife.Core.Systems;

namespace DinoLife.Core.Simulation;

public static class FactorySimulation
{
    public static SimulationEngine GenerateDefaultSimulation(Planet world)
    {
        var clock = new StopwatchClock();
        return new SimulationEngine(world, clock, Array.Empty<ISystem>());
    }
}