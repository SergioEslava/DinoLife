using System;

namespace DinoLife.Cli;

/// <summary>
/// Reads console input and dispatches actions to the program.
/// </summary>
public class InputHandler
{
    /// <summary>
    /// Poll for a single key press and invoke the corresponding action.
    /// </summary>
    /// <param name="program">Program instance that owns the simulation controls.</param>
    public void Poll(Program program)
    {
        if (!Console.KeyAvailable) { return; }

        ConsoleKey key = Console.ReadKey(true).Key;

        switch (key)
        {
            case ConsoleKey.Escape:
                program.Exit();
                break;

            case ConsoleKey.Spacebar:
                program.TogglePause();
                break;

            case ConsoleKey.T:
                program.RequestTick();
                break;

            case ConsoleKey.G:
                program.ToggleGrid();
                break;

            case ConsoleKey.F:
                program.ToggleTurbo();
                break;

            case ConsoleKey.O:
                program.TogglePerformanceOverlay();
                break;
        }
    }
}
