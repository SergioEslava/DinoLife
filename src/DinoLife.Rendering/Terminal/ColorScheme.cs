using System;

namespace DinoLife.Rendering.Terminal;

/// <summary>
/// Configurable colors for the terminal renderer.
/// </summary>
public sealed class ColorScheme
{
    public ConsoleColor Default { get; set; } = ConsoleColor.Gray;
    public ConsoleColor Hud { get; set; } = ConsoleColor.White;
    public ConsoleColor Grid { get; set; } = ConsoleColor.DarkGray;
    public ConsoleColor Corpse { get; set; } = ConsoleColor.DarkRed;

    public ConsoleColor Herbivore { get; set; } = ConsoleColor.Blue;
    public ConsoleColor Carnivore { get; set; } = ConsoleColor.Red;
    public ConsoleColor Plant { get; set; } = ConsoleColor.DarkGreen;
    public ConsoleColor Scavenger { get; set; } = ConsoleColor.Yellow;
    public ConsoleColor SelectedEntity { get; set; } = ConsoleColor.Cyan;

    public ConsoleColor HealthLow { get; set; } = ConsoleColor.Red;
    public ConsoleColor HealthMedium { get; set; } = ConsoleColor.Yellow;
    public ConsoleColor HealthHigh { get; set; } = ConsoleColor.Green;
}
