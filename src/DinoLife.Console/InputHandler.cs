using System;
using System.Collections.Generic;

namespace DinoLife.Cli;

/// <summary>
/// Reads non-blocking keyboard input and enqueues semantic commands.
/// </summary>
public sealed class InputHandler
{
    private readonly Queue<InputCommand> _queue = new();

    public void Poll()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (TryMap(key, out InputCommand command))
            {
                _queue.Enqueue(command);
            }
        }
    }

    public bool TryDequeue(out InputCommand command)
    {
        if (_queue.Count == 0)
        {
            command = default;
            return false;
        }

        command = _queue.Dequeue();
        return true;
    }

    private static bool TryMap(ConsoleKeyInfo key, out InputCommand command)
    {
        switch (key.Key)
        {
            case ConsoleKey.Spacebar:
                command = new InputCommand(InputCommandType.TogglePause);
                return true;
            case ConsoleKey.RightArrow:
                command = new InputCommand(InputCommandType.StepOnce);
                return true;
            case ConsoleKey.Add:
            case ConsoleKey.OemPlus:
                command = new InputCommand(InputCommandType.SpeedUp);
                return true;
            case ConsoleKey.Subtract:
            case ConsoleKey.OemMinus:
                command = new InputCommand(InputCommandType.SpeedDown);
                return true;
            case ConsoleKey.S:
                command = new InputCommand(InputCommandType.SaveState);
                return true;
            case ConsoleKey.L:
                command = new InputCommand(InputCommandType.LoadState);
                return true;
            case ConsoleKey.B:
                command = new InputCommand(InputCommandType.RefreshSaveBrowser);
                return true;
            case ConsoleKey.R:
                command = new InputCommand(InputCommandType.ResetSimulation);
                return true;
            case ConsoleKey.Q:
            case ConsoleKey.Escape:
                command = new InputCommand(InputCommandType.Quit);
                return true;
            case ConsoleKey.P:
                command = new InputCommand(InputCommandType.TogglePerformanceOverlay);
                return true;
            case ConsoleKey.H:
                command = new InputCommand(InputCommandType.ToggleHelp);
                return true;

            // Camera controls still available from milestone 3.6
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                command = new InputCommand(InputCommandType.PanLeft);
                return true;
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                command = new InputCommand(InputCommandType.PanUp);
                return true;
            case ConsoleKey.DownArrow:
                command = new InputCommand(InputCommandType.PanDown);
                return true;
            case ConsoleKey.D:
                command = new InputCommand(InputCommandType.PanRight);
                return true;
            case ConsoleKey.Home:
                command = new InputCommand(InputCommandType.ResetCamera);
                return true;
            case ConsoleKey.F:
                command = new InputCommand(InputCommandType.ToggleFollowSelected);
                return true;
            case ConsoleKey.Tab:
            case ConsoleKey.Oem6:
                command = new InputCommand(InputCommandType.SelectNextEntity);
                return true;
            case ConsoleKey.Oem4:
                command = new InputCommand(InputCommandType.SelectPreviousEntity);
                return true;
            case ConsoleKey.G:
                command = new InputCommand(InputCommandType.ToggleGrid);
                return true;
            case ConsoleKey.J:
                command = new InputCommand(InputCommandType.BrowsePreviousSave);
                return true;
            case ConsoleKey.K:
                command = new InputCommand(InputCommandType.BrowseNextSave);
                return true;
            case ConsoleKey.O:
                command = new InputCommand(InputCommandType.TogglePerformanceOverlay);
                return true;
            default:
                command = default;
                return false;
        }
    }
}

public readonly record struct InputCommand(InputCommandType Type);

public enum InputCommandType
{
    TogglePause,
    StepOnce,
    SpeedUp,
    SpeedDown,
    SaveState,
    LoadState,
    ResetSimulation,
    Quit,
    TogglePerformanceOverlay,
    ToggleHelp,
    PanLeft,
    PanRight,
    PanUp,
    PanDown,
    ResetCamera,
    ToggleFollowSelected,
    SelectNextEntity,
    SelectPreviousEntity,
    ToggleGrid,
    BrowsePreviousSave,
    BrowseNextSave,
    RefreshSaveBrowser
}
