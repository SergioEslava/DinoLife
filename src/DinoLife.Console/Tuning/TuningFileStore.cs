using System.Text.Json;

namespace DinoLife.Cli.Tuning;

internal sealed class TuningFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string Export(string directory, SimulationTuningProfile profile)
    {
        Directory.CreateDirectory(directory);
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        string path = Path.Combine(directory, $"params-{profile.ProfileName.ToLowerInvariant()}-{timestamp}.json");
        string json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(path, json);
        return path;
    }

    public bool TryImportLatest(string directory, out SimulationTuningProfile profile, out string path, out string error)
    {
        profile = new SimulationTuningProfile();
        path = string.Empty;
        error = string.Empty;

        if (!Directory.Exists(directory))
        {
            error = "No tuning files available";
            return false;
        }

        string[] files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
        {
            error = "No tuning files available";
            return false;
        }

        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        path = files[^1];
        try
        {
            string json = File.ReadAllText(path);
            SimulationTuningProfile? loaded = JsonSerializer.Deserialize<SimulationTuningProfile>(json, JsonOptions);
            if (loaded is null)
            {
                error = "Invalid tuning file";
                return false;
            }

            profile = loaded;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to load tuning file: {ex.Message}";
            return false;
        }
    }
}
