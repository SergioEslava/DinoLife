using DinoLife.Core.Simulation;
using DinoLife.Core.World;
using System;
using System.Threading;

namespace DinoLife.Cli;

/// <summary>
/// Console entry point that wires input handling to the simulation engine.
/// </summary>
public class Program
{
    private bool _isExiting;
    private bool _tickOnceRequested;
    private SimulationEngine? _simulation;

    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        new Program().Run();
    }

    /// <summary>
    /// Main loop that advances the simulation and processes input.
    /// </summary>
    private void Run()
    {
        Planet world = new Planet();
        _simulation = FactorySimulation.GenerateDefaultSimulation(world);

        InputHandler input = new InputHandler();

        while (!_isExiting)
        {
            input.Poll(this);

            if (_tickOnceRequested)
            {
                _simulation!.TickOnce();
                _tickOnceRequested = false; 
            }
            else if (!_simulation!.IsStopped)
            {
                _simulation!.Step();
            }

            Thread.Sleep(16); // ~60fps
        }
    }

    /// <summary>
    /// Request a clean exit from the main loop.
    /// </summary>
    public void Exit() => _isExiting = true;

    /// <summary>
    /// Toggle simulation pause/resume.
    /// </summary>
    public void TogglePause()
    {
        if (_simulation is null) { return; }

        if (_simulation.IsStopped) { _simulation.Resume(); }
        else { _simulation.Stop(); }
    }

    /// <summary>
    /// Request a single tick when the simulation is paused.
    /// </summary>
    public void RequestTick()
    {
        if (_simulation is not null && _simulation.IsStopped) {_tickOnceRequested = true;}
    }
}
