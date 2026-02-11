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

            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                program.PanLeft();
                break;

            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                program.PanRight();
                break;

            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                program.PanUp();
                break;

            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                program.PanDown();
                break;

            case ConsoleKey.Add:
            case ConsoleKey.OemPlus:
                program.ZoomIn();
                break;

            case ConsoleKey.Subtract:
            case ConsoleKey.OemMinus:
                program.ZoomOut();
                break;

            case ConsoleKey.Home:
                program.ResetCamera();
                break;

            case ConsoleKey.F:
                program.ToggleFollowSelected();
                break;

            case ConsoleKey.Tab:
                program.SelectNextEntity();
                break;

            case ConsoleKey.Oem4:
                program.SelectPreviousEntity();
                break;

            case ConsoleKey.Oem6:
                program.SelectNextEntity();
                break;

            case ConsoleKey.U:
                program.ToggleTurbo();
                break;

            case ConsoleKey.O:
                program.TogglePerformanceOverlay();
                break;
        }
    }
}
