using System;

namespace DinoLife.Rendering.Terminal.UI;

/// <summary>
/// Full-screen help overlay with command reference and gameplay guidance.
/// </summary>
public sealed class HelpOverlay : IOverlay
{
    public bool IsVisible { get; set; }

    public void Render(UiCanvas canvas)
    {
        int width = Math.Max(52, canvas.Width - 4);
        int height = Math.Max(12, canvas.Height - 4);
        width = Math.Min(width, canvas.Width);
        height = Math.Min(height, canvas.Height);

        int left = Math.Max(0, (canvas.Width - width) / 2);
        int top = Math.Max(0, (canvas.Height - height) / 2);

        canvas.FillRect(left, top, width, height, ' ', ConsoleColor.Gray, ConsoleColor.Black);
        canvas.DrawBox(left, top, width, height, ConsoleColor.White, ConsoleColor.Black);
        canvas.DrawText(left + 2, top, "HELP [H close]", ConsoleColor.White, ConsoleColor.Black);

        string[] lines =
        [
            "Command reference:",
            "Space pause/resume | RightArrow step | +/- sim speed",
            "WASD or Arrows pan camera | Home reset camera | F follow selected",
            "Tab / [ ] select entities | G grid | P performance | M command menu",
            "S save | L load selected | J/K browse saves | R reset world | Q quit",
            "",
            "Entity behavior summary:",
            "Herbivores eat plants, avoid carnivores, and reproduce above threshold.",
            "Carnivores hunt herbivores; very hungry carnivores may chase scavengers.",
            "Scavengers avoid carnivores and prioritize nearby corpses.",
            "Plants regrow energy while active and respawn after being consumed.",
            "",
            "Tips for interesting scenarios:",
            "Use Parameter Tuning to lower plant growth and raise hunger for collapse tests.",
            "Use Chaotic preset to trigger booms/crashes quickly and stress balancing.",
            "Use Stable preset for long-running equilibrium and benchmark comparisons.",
            "Increase detection radii to intensify predator-prey feedback loops."
        ];

        int y = top + 2;
        int maxY = top + height - 2;
        for (int i = 0; i < lines.Length && y < maxY; i++, y++)
        {
            string line = lines[i];
            if (line.Length > width - 4)
            {
                line = line[..(width - 4)];
            }

            ConsoleColor color = line.EndsWith(":")
                ? ConsoleColor.White
                : ConsoleColor.Gray;

            canvas.DrawText(left + 2, y, line, color, ConsoleColor.Black);
        }
    }
}
