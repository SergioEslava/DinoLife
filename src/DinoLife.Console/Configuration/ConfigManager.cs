using System.Text.Json;
using DinoLife.Cli.Tuning;

namespace DinoLife.Cli.Configuration;

internal sealed class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _appSettingsPath;
    private readonly string _worldConfigPath;
    private readonly string _appSchemaPath;
    private readonly string _worldSchemaPath;
    private DateTime _lastAppWriteUtc;
    private DateTime _lastWorldWriteUtc;
    private DateTime _nextPollUtc = DateTime.MinValue;

    public ConfigManager(string rootDirectory)
    {
        _appSettingsPath = Path.Combine(rootDirectory, "appsettings.json");
        _worldConfigPath = Path.Combine(rootDirectory, "world-config.json");
        _appSchemaPath = Path.Combine(rootDirectory, "appsettings.schema.json");
        _worldSchemaPath = Path.Combine(rootDirectory, "world-config.schema.json");
    }

    public bool LoadInitial(out AppSettingsConfig appSettings, out WorldConfig worldConfig, out string message)
    {
        appSettings = new AppSettingsConfig();
        worldConfig = new WorldConfig();
        message = string.Empty;

        bool appOk = TryLoadValidated(_appSettingsPath, _appSchemaPath, new AppSettingsConfig(), out appSettings, out string appError);
        bool worldOk = TryLoadValidated(_worldConfigPath, _worldSchemaPath, new WorldConfig
        {
            TuningProfile = TuningPresets.Balanced()
        }, out worldConfig, out string worldError);

        _lastAppWriteUtc = GetWriteUtc(_appSettingsPath);
        _lastWorldWriteUtc = GetWriteUtc(_worldConfigPath);

        if (!appOk || !worldOk)
        {
            message = string.Join(" | ", new[] { appError, worldError }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        return appOk && worldOk;
    }

    public bool TryHotReload(
        bool hotReloadEnabled,
        out AppSettingsConfig? reloadedApp,
        out WorldConfig? reloadedWorld,
        out string message)
    {
        reloadedApp = null;
        reloadedWorld = null;
        message = string.Empty;

        if (!hotReloadEnabled) { return false; }
        if (DateTime.UtcNow < _nextPollUtc) { return false; }
        _nextPollUtc = DateTime.UtcNow.AddMilliseconds(500);

        bool changed = false;
        List<string> notes = [];

        DateTime appWrite = GetWriteUtc(_appSettingsPath);
        if (appWrite > _lastAppWriteUtc)
        {
            _lastAppWriteUtc = appWrite;
            if (TryLoadValidated(_appSettingsPath, _appSchemaPath, new AppSettingsConfig(), out AppSettingsConfig loadedApp, out string error))
            {
                reloadedApp = loadedApp;
                changed = true;
                notes.Add("appsettings reloaded");
            }
            else
            {
                notes.Add($"appsettings ignored: {error}");
            }
        }

        DateTime worldWrite = GetWriteUtc(_worldConfigPath);
        if (worldWrite > _lastWorldWriteUtc)
        {
            _lastWorldWriteUtc = worldWrite;
            if (TryLoadValidated(_worldConfigPath, _worldSchemaPath, new WorldConfig
            {
                TuningProfile = TuningPresets.Balanced()
            }, out WorldConfig loadedWorld, out string error))
            {
                reloadedWorld = loadedWorld;
                changed = true;
                notes.Add("world-config reloaded");
            }
            else
            {
                notes.Add($"world-config ignored: {error}");
            }
        }

        if (notes.Count > 0)
        {
            message = string.Join(" | ", notes);
        }

        return changed;
    }

    private static DateTime GetWriteUtc(string path)
    {
        return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
    }

    private bool TryLoadValidated<T>(string configPath, string schemaPath, T defaults, out T config, out string error)
    {
        error = string.Empty;
        config = defaults;

        try
        {
            if (!File.Exists(configPath))
            {
                WriteDefaults(configPath, defaults);
            }

            string json = File.ReadAllText(configPath);
            using JsonDocument dataDoc = JsonDocument.Parse(json);
            if (!File.Exists(schemaPath))
            {
                error = $"Missing schema: {Path.GetFileName(schemaPath)}";
                return false;
            }

            string schemaJson = File.ReadAllText(schemaPath);
            using JsonDocument schemaDoc = JsonDocument.Parse(schemaJson);
            if (!SchemaSubsetValidator.Validate(dataDoc.RootElement, schemaDoc.RootElement, out string schemaError))
            {
                error = $"Schema validation failed: {schemaError}";
                return false;
            }

            T? parsed = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (parsed is null)
            {
                error = "Config deserialized to null";
                return false;
            }

            config = parsed;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void WriteDefaults<T>(string path, T defaults)
    {
        string json = JsonSerializer.Serialize(defaults, JsonOptions);
        File.WriteAllText(path, json);
    }
}
