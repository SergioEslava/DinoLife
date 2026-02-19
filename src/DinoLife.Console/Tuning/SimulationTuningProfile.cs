namespace DinoLife.Cli.Tuning;

internal sealed class SimulationTuningProfile
{
    public string ProfileName { get; set; } = "Balanced";

    // Movement speeds
    public float HerbivoreSpeed { get; set; } = 3.0f;
    public float CarnivoreSpeed { get; set; } = 4.0f;
    public float ScavengerSpeed { get; set; } = 3.0f;

    // Metabolism rates (hunger per second)
    public float HerbivoreHungerRate { get; set; } = 1.3f;
    public float CarnivoreHungerRate { get; set; } = 1.5f;
    public float ScavengerHungerRate { get; set; } = 0.6f;

    // Reproduction thresholds
    public float HerbivoreReproductionThreshold { get; set; } = 55f;
    public float CarnivoreReproductionThreshold { get; set; } = 80f;
    public float ScavengerReproductionThreshold { get; set; } = 55f;

    // Detection radii
    public float HerbivoreDetectionRadius { get; set; } = 20f;
    public float CarnivoreDetectionRadius { get; set; } = 20f;
    public float ScavengerDetectionRadius { get; set; } = 35f;

    // Plant growth
    public float PlantGrowthRate { get; set; } = 0.5f;
    public float PlantRespawnTime { get; set; } = 30f;

    public SimulationTuningProfile Clone()
    {
        return new SimulationTuningProfile
        {
            ProfileName = ProfileName,
            HerbivoreSpeed = HerbivoreSpeed,
            CarnivoreSpeed = CarnivoreSpeed,
            ScavengerSpeed = ScavengerSpeed,
            HerbivoreHungerRate = HerbivoreHungerRate,
            CarnivoreHungerRate = CarnivoreHungerRate,
            ScavengerHungerRate = ScavengerHungerRate,
            HerbivoreReproductionThreshold = HerbivoreReproductionThreshold,
            CarnivoreReproductionThreshold = CarnivoreReproductionThreshold,
            ScavengerReproductionThreshold = ScavengerReproductionThreshold,
            HerbivoreDetectionRadius = HerbivoreDetectionRadius,
            CarnivoreDetectionRadius = CarnivoreDetectionRadius,
            ScavengerDetectionRadius = ScavengerDetectionRadius,
            PlantGrowthRate = PlantGrowthRate,
            PlantRespawnTime = PlantRespawnTime
        };
    }
}
