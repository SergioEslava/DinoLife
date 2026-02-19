namespace DinoLife.Cli;

internal enum RendererMode
{
    Legacy,
    Tui
}

internal static class RendererModeParser
{
    public static RendererMode Parse(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (!arg.StartsWith("--renderer", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? value = null;
            int separator = arg.IndexOf('=');
            if (separator >= 0 && separator < arg.Length - 1)
            {
                value = arg[(separator + 1)..];
            }
            else if (string.Equals(arg, "--renderer", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                value = args[i + 1];
                i++;
            }

            if (string.Equals(value, "tui", StringComparison.OrdinalIgnoreCase))
            {
                return RendererMode.Tui;
            }
        }

        return RendererMode.Legacy;
    }
}
