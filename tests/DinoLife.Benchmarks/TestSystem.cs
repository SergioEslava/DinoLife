using DinoLife.Core.World;
using DinoLife.Core.Systems;

namespace DinoLife.Benchmarks;

// Helper for dummy systems
internal sealed class TestSystem : ISystem
{
    private readonly Action<Planet, double> _onUpdate;
    public TestSystem(Action<Planet, double> onUpdate) => _onUpdate = onUpdate;
    public void Update(Planet world, double deltaTime) => _onUpdate(world, deltaTime);
}