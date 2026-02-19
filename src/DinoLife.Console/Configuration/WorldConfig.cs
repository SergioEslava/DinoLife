using DinoLife.Cli.Tuning;

namespace DinoLife.Cli.Configuration;

internal sealed class WorldConfig
{
    public float WorldWidth { get; set; } = 120f;
    public float WorldHeight { get; set; } = 40f;
    public int RandomSeed { get; set; } = 1234;
    public int InitialPlants { get; set; } = 60;
    public int InitialHerbivores { get; set; } = 20;
    public int InitialCarnivores { get; set; } = 8;
    public int InitialScavengers { get; set; } = 12;
    public SimulationTuningProfile TuningProfile { get; set; } = TuningPresets.Balanced();
}
