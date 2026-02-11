namespace DinoLife.Persistence;

public sealed class SaveBrowser
{
    private readonly JsonWorldSerializer _serializer;

    public SaveBrowser(JsonWorldSerializer serializer)
    {
        _serializer = serializer;
    }

    public IReadOnlyList<SaveMetadata> ListSaves(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return Array.Empty<SaveMetadata>();
        }

        List<SaveMetadata> result = new();
        string[] files = Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly);
        foreach (string path in files)
        {
            if (_serializer.TryReadMetadata(path, out SaveMetadata metadata))
            {
                result.Add(metadata);
            }
        }

        result.Sort(static (a, b) => b.CreatedUtc.CompareTo(a.CreatedUtc));
        return result;
    }
}

public sealed record SaveMetadata(
    string Path,
    string Name,
    DateTimeOffset CreatedUtc,
    int Tick,
    long SizeBytes,
    bool IsValid);
