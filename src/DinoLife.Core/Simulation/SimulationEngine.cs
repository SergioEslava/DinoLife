using DinoLife.Core.World;
using DinoLife.Core.Systems;

public class SimulationEngine
{
    /// <summary>
    /// Target logic ticks per second.
    /// </summary>
    public const double TickRate = 60.0;

    /// <summary>
    /// Duration of a single simulation tick in seconds.
    /// </summary>
    public const double TickTime = 1.0 / TickRate;

    private readonly Planet _world;
    private readonly IClock _clock;
    private readonly List<ISystem> _systems;

    private double _accumulator;
    private double _lastTime;
    private bool _running;

    /// <summary>
    /// The simulation world being driven by this engine.
    /// </summary>
    public Planet World => _world;

    /// <summary>
    /// Creates a new simulation engine for the provided <paramref name="world"/> using
    /// the given <paramref name="clock"/> and systems collection.
    /// </summary>
    public SimulationEngine(
        Planet world,
        IClock clock,
        IEnumerable<ISystem> systems)
    {
        _world = world;
        _clock = clock;
        _systems = systems.ToList();
        _lastTime = clock.Now;
    }

    /// <summary>
    /// Advance the simulation according to the clock. Accumulates real time and
    /// executes the required number of fixed logic ticks.
    /// </summary>
    public void Step()
    {
            double currentTime = _clock.Now;
            double frameTime = currentTime - _lastTime;
            _lastTime = currentTime;

            _accumulator += frameTime;

            int ticksToRun = (int)(_accumulator * TickRate);

            if (ticksToRun <= 0) { return; }

            for (int i = 0; i < ticksToRun; i++)
            {
                TickOnce();
            }

            _accumulator -= ticksToRun * TickTime;
    }

    /// <summary>
    /// Execute a single fixed logic tick. Calls <see cref="ISystem.Update"/> on each system
    /// with a fixed delta time of <see cref="TickTime"/>.
    /// </summary>
    public void TickOnce()
    {
        foreach (ISystem system in _systems)
        {
            system.Update(_world, TickTime);
        }

        _world.Tick++;
    }

    /// <summary>
    /// Run the engine loop until <see cref="Stop"/> is called.
    /// This method blocks the calling thread.
    /// </summary>
    public void Run()
    {
        _running = true;
        while (_running)
        {
            Step();
        }
    }

    /// <summary>
    /// Stops the run loop started by <see cref="Run"/>.
    /// </summary>
    public void Stop() => _running = false;
}
