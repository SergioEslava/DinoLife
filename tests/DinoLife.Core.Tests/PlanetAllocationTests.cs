using System;
using DinoLife.Core.World;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.World;

public class PlanetAllocationTests
{
    [Fact]
    public void Planet_AllocatesUpToMaxEntities()
    {
        var world = new Planet();

        // Allocate the maximum number of entity slots
        for (int i = 0; i < 5000; i++)
        {
            world.AllocateEntitySlot();
        }

        // Further allocation should throw because the world is full
        Action act = () => world.AllocateEntitySlot();
        act.Should().Throw<InvalidOperationException>();
    }
}
