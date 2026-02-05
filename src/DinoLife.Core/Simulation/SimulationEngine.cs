using DinoLife.Core.World;
using DinoLife.Core.Systems;

public class SimulationEngine
{
    public const double TickRate = 60.0;
    public const double TickTime = 1.0 / TickRate;

    private readonly Planet _world;
    private readonly IClock _clock;
    private readonly List<ISystem> _systems;

    private double _accumulator;
    private double _lastTime;
    private bool _running;

    public Planet World => _world;

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
    /// Forwards the simulation by clock's time
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
    /// Execute exactly one logic tick
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
    /// Real loop
    /// </summary>
    public void Run()
    {
        _running = true;
        while (_running)
        {
            Step();
        }
    }

    public void Stop() => _running = false;
}
