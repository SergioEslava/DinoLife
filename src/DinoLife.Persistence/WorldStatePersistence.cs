using DinoLife.Core.World;

namespace DinoLife.Persistence;

/// <summary>
/// Backward-compatible facade for save/load APIs.
/// </summary>
public static class WorldStatePersistence
{
    private static readonly IWorldSerializer Serializer = new JsonWorldSerializer();

    public static void Save(string path, Planet world)
    {
        Serializer.Save(path, world);
    }

    public static bool TryLoad(string path, out Planet world, out string error)
    {
        return Serializer.TryLoad(path, out world, out error);
    }
}
