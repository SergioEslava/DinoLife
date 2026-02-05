using DinoLife.Core.World;

namespace DinoLife.Core.Systems;

public interface ISystem
{
    void Update(Planet planet, double deltatime);
}