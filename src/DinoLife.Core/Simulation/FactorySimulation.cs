using DinoLife.Core.World;
using DinoLife.Core.Systems;

namespace DinoLife.Core.Simulation;

public static class FactorySimulation
{
    public static SimulationEngine GenerateDefaultSimulation(Planet world)
    {
        var clock = new StopwatchClock();
        var systems = new ISystem[]
        {
            new BehaviorSystem(),
            new MovementSystem(),
            new MetabolismSystem()
        };

        return new SimulationEngine(world, clock, systems);
    }
}
