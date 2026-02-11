using DinoLife.Core.World;

namespace DinoLife.Persistence;

public interface IWorldSerializer
{
    string SchemaVersion { get; }

    void Save(string path, Planet world);

    bool TryLoad(string path, out Planet world, out string error);
}
