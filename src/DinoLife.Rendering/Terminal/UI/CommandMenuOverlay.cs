using System;
using System.Collections.Generic;

namespace DinoLife.Rendering.Terminal.UI;

/// <summary>
/// In-screen command menu with selection highlight.
/// </summary>
public sealed class CommandMenuOverlay : IOverlay
{
    public bool IsVisible { get; set; }

    public string Title { get; set; } = "COMMAND MENU";

    public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();

    public int SelectedIndex { get; set; } = -1;

    public string FooterHint { get; set; } = "M close | Enter activate";

    public void Render(UiCanvas canvas)
    {
        int maxItems = Math.Max(1, canvas.Height - 8);
        int itemCount = Math.Min(Items.Count, maxItems);
        int windowWidth = Math.Clamp(canvas.Width, 44, 82);
        int windowHeight = itemCount + 5;
        int left = Math.Max(0, (canvas.Width - windowWidth) / 2);
        int top = Math.Max(1, (canvas.Height - windowHeight) / 2);

        int selected = itemCount == 0
            ? -1
            : Math.Clamp(SelectedIndex, 0, itemCount - 1);

        canvas.DrawBox(left, top, windowWidth, windowHeight, ConsoleColor.White, ConsoleColor.Black);
        string title = $"{Title}";
        canvas.DrawText(left + 2, top, title, ConsoleColor.White, ConsoleColor.Black);

        for (int i = 0; i < itemCount; i++)
        {
            bool isSelected = i == selected;
            ConsoleColor fg = isSelected ? ConsoleColor.Black : ConsoleColor.Gray;
            ConsoleColor bg = isSelected ? ConsoleColor.Gray : ConsoleColor.Black;

            string line = $"{(isSelected ? '>' : ' ')} {Items[i]}";
            if (line.Length > windowWidth - 4)
            {
                line = line[..(windowWidth - 4)];
            }

            line = line.PadRight(windowWidth - 4);
            canvas.DrawText(left + 2, top + 2 + i, line, fg, bg);
        }

        string footer = FooterHint;
        if (footer.Length > windowWidth - 4)
        {
            footer = footer[..(windowWidth - 4)];
        }

        canvas.DrawText(left + 2, top + windowHeight - 2, footer, ConsoleColor.DarkGray, ConsoleColor.Black);
    }
}
