namespace DinoLife.Cli.Tuning;

internal static class TuningPresets
{
    public static SimulationTuningProfile Balanced()
    {
        return new SimulationTuningProfile
        {
            ProfileName = "Balanced"
        };
    }

    public static SimulationTuningProfile Chaotic()
    {
        return new SimulationTuningProfile
        {
            ProfileName = "Chaotic",
            HerbivoreSpeed = 5.5f,
            CarnivoreSpeed = 6.5f,
            ScavengerSpeed = 5.2f,
            HerbivoreHungerRate = 2.2f,
            CarnivoreHungerRate = 2.7f,
            ScavengerHungerRate = 1.6f,
            HerbivoreReproductionThreshold = 38f,
            CarnivoreReproductionThreshold = 52f,
            ScavengerReproductionThreshold = 42f,
            HerbivoreDetectionRadius = 34f,
            CarnivoreDetectionRadius = 38f,
            ScavengerDetectionRadius = 45f,
            PlantGrowthRate = 1.5f,
            PlantRespawnTime = 11f
        };
    }

    public static SimulationTuningProfile Stable()
    {
        return new SimulationTuningProfile
        {
            ProfileName = "Stable",
            HerbivoreSpeed = 2.4f,
            CarnivoreSpeed = 3.0f,
            ScavengerSpeed = 2.3f,
            HerbivoreHungerRate = 0.9f,
            CarnivoreHungerRate = 1.1f,
            ScavengerHungerRate = 0.45f,
            HerbivoreReproductionThreshold = 70f,
            CarnivoreReproductionThreshold = 95f,
            ScavengerReproductionThreshold = 72f,
            HerbivoreDetectionRadius = 16f,
            CarnivoreDetectionRadius = 18f,
            ScavengerDetectionRadius = 24f,
            PlantGrowthRate = 0.9f,
            PlantRespawnTime = 18f
        };
    }
}
