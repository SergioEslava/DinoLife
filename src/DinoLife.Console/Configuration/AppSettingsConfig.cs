namespace DinoLife.Cli.Configuration;

internal sealed class AppSettingsConfig
{
    public string RendererDefault { get; set; } = "legacy";
    public int TargetFrameMs { get; set; } = 16;
    public int AutosaveEveryTicks { get; set; } = 300;
    public bool HotReloadEnabled { get; set; } = true;
    public string SaveDirectory { get; set; } = "saves";
    public string TuningDirectory { get; set; } = "tuning";
}
