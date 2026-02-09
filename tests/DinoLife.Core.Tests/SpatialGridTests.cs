using System.Collections.Generic;
using DinoLife.Core.Utils;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.World;

public class SpatialGridTests
{
    [Fact]
    public void SpatialGrid_QueryRadius_ReturnsEntitiesInRange()
    {
        var grid = new SpatialGrid(new Vector2(100f, 100f), cellSize: 10f);
        var results = new List<int>();

        grid.Insert(1, new Vector2(10f, 10f));
        grid.Insert(2, new Vector2(50f, 50f));

        grid.QueryRadius(new Vector2(12f, 10f), radius: 5f, results);

        results.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public void SpatialGrid_QueryRadius_ReturnsEmpty_WhenNoEntitiesNearby()
    {
        var grid = new SpatialGrid(new Vector2(100f, 100f), cellSize: 10f);
        var results = new List<int>();

        grid.Insert(1, new Vector2(80f, 80f));

        grid.QueryRadius(new Vector2(10f, 10f), radius: 5f, results);

        results.Should().BeEmpty();
    }
}
