using DinoLife.Core.World;
using DinoLife.Core.Systems;
using System;

namespace DinoLife.Core.Simulation;

/// <summary>
/// Fixed-timestep simulation runner that advances world state through systems.
/// </summary>
public class SimulationEngine
{
    public const double TickRate = 60.0;
    public const double TickTime = 1.0 / TickRate;

    private readonly Planet _world;
    private readonly IClock _clock;
    private readonly List<ISystem> _systems;

    private double _accumulator;
    private double _lastTime;
    private bool _isStopped;

    /// <summary>
    /// World instance being simulated.
    /// </summary>
    public Planet World => _world;

    /// <summary>
    /// Whether the engine is currently stopped.
    /// </summary>
    public bool IsStopped => _isStopped;

    /// <summary>
    /// Create a new simulation engine instance.
    /// </summary>
    /// <param name="world">World to update.</param>
    /// <param name="clock">Clock used to measure real time.</param>
    /// <param name="systems">Systems that will update the world.</param>
    public SimulationEngine(Planet world, IClock clock, IEnumerable<ISystem> systems)
    {
        _world = world;
        _clock = clock;
        _systems = systems.ToList();
        _lastTime = clock.Now;
    }

    /// <summary>
    /// Avanza la simulación según el tiempo real acumulado.
    /// </summary>
    public void Step()
    {
        if (_isStopped) { return; }

        double currentTime = _clock.Now;
        double frameTime = currentTime - _lastTime;
        _lastTime = currentTime;

        _accumulator += frameTime;

        int ticksToRun = (int)(_accumulator * TickRate);
        if (ticksToRun <= 0) {return;}

        for (int i = 0; i < ticksToRun; i++){TickOnce();}

        _accumulator -= ticksToRun * TickTime;
    }

    /// <summary>
    /// Ejecuta un tick discreto de la simulación.
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
    /// Detiene la simulación evitando que el tiempo acumulado siga creciendo.
    /// </summary>
    public void Stop()
    {
        if (_isStopped) { return; }

        _isStopped = true;
        _accumulator = 0;
    }

    /// <summary>
    /// Reanuda la simulación reiniciando el tiempo base para evitar saltos.
    /// </summary>
    public void Resume()
    {
        if (!_isStopped) { return; }

        _isStopped = false;
        _lastTime = _clock.Now;
    }
}
