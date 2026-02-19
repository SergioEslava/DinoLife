namespace DinoLife.Cli;

internal enum UiFocus
{
    World,
    Menu
}

internal enum MenuCommand
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Activate,
    Close
}

internal sealed class InputRouter
{
    public UiFocus Focus { get; private set; } = UiFocus.World;

    public bool IsMenuFocused => Focus == UiFocus.Menu;

    public void SetMenuOpen(bool isOpen)
    {
        Focus = isOpen ? UiFocus.Menu : UiFocus.World;
    }

    public bool TryRouteMenuCommand(InputCommandType type, out MenuCommand command)
    {
        command = default;
        if (!IsMenuFocused) { return false; }

        switch (type)
        {
            case InputCommandType.PanUp:
                command = MenuCommand.MoveUp;
                return true;
            case InputCommandType.PanDown:
                command = MenuCommand.MoveDown;
                return true;
            case InputCommandType.PanLeft:
            case InputCommandType.SpeedDown:
                command = MenuCommand.MoveLeft;
                return true;
            case InputCommandType.PanRight:
            case InputCommandType.SpeedUp:
                command = MenuCommand.MoveRight;
                return true;
            case InputCommandType.MenuActivate:
                command = MenuCommand.Activate;
                return true;
            case InputCommandType.ToggleMenu:
            case InputCommandType.Quit:
                command = MenuCommand.Close;
                return true;
            default:
                return false;
        }
    }

    public bool BlocksWorldCommand(InputCommandType type)
    {
        if (!IsMenuFocused) { return false; }

        return type is not InputCommandType.ToggleMenu
            and not InputCommandType.MenuActivate
            and not InputCommandType.PanUp
            and not InputCommandType.PanDown
            and not InputCommandType.PanLeft
            and not InputCommandType.PanRight
            and not InputCommandType.SpeedDown
            and not InputCommandType.SpeedUp
            and not InputCommandType.ToggleHelp
            and not InputCommandType.Quit;
    }
}
