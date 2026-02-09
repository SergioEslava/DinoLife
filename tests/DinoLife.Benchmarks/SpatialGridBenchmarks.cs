using BenchmarkDotNet.Attributes;
using DinoLife.Core.World;
using DinoLife.Core.Utils;

namespace DinoLife.Benchmarks;

[MemoryDiagnoser]
public class SpatialGridBenchmarks
{
    private SpatialGrid _grid = null!;
    private Vector2[] _positions = null!;
    private Vector2[] _queryPoints = null!;
    private List<int> _results = null!;

    [GlobalSetup]
    public void Setup()
    {
        var worldSize = new Vector2(1000f, 1000f);
        _grid = new SpatialGrid(worldSize, cellSize: 10f);

        var rng = new Random(1234);
        _positions = new Vector2[5000];
        for (int i = 0; i < _positions.Length; i++)
        {
            _positions[i] = new Vector2(
                (float)rng.NextDouble() * worldSize.X,
                (float)rng.NextDouble() * worldSize.Y);
        }

        _queryPoints = new Vector2[100];
        for (int i = 0; i < _queryPoints.Length; i++)
        {
            _queryPoints[i] = new Vector2(
                (float)rng.NextDouble() * worldSize.X,
                (float)rng.NextDouble() * worldSize.Y);
        }

        _results = new List<int>(256);
    }

    [Benchmark]
    public void SpatialGrid_5000Entities_100Queries()
    {
        _grid.Clear();
        for (int i = 0; i < _positions.Length; i++)
        {
            _grid.Insert(i, _positions[i]);
        }

        for (int q = 0; q < _queryPoints.Length; q++)
        {
            _grid.QueryRadius(_queryPoints[q], radius: 20f, _results);
        }
    }
}
