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
            new SpatialGridSystem(),
            new BehaviorSystem(),
            new MovementSystem(),
            new MetabolismSystem(),
            new PlantGrowthSystem(),
            new HuntingSystem(),
            new ReproductionSystem(),
            new DeathSystem()
        };

        return new SimulationEngine(world, clock, systems);
    }
}
