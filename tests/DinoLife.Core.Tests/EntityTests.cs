using DinoLife.Core.Entities;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Entities;

public class EntityTests
{
    [Fact]
    public void Entity_Constructor_SetsAliveAndId_WhenCreated()
    {
        var entity = new Entity(EntityType.Carnivore, ComponentFlags.Movement);

        entity.IsAlive.Should().BeTrue();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Entity_Has_ReturnsTrue_WhenFlagIsPresent_AndFalse_WhenFlagIsMissing()
    {
        var entity = new Entity(
            EntityType.Carnivore,
            ComponentFlags.Movement | ComponentFlags.Diet
        );

        entity.Has(ComponentFlags.Movement).Should().BeTrue();
        entity.Has(ComponentFlags.Reproduction).Should().BeFalse();
    }

    [Fact]
    public void Entity_Kill_SetsIsAliveFalse_AndClearsFlags_WhenEntityIsAlive()
    {
        var entity = new Entity(
            EntityType.Carnivore,
            ComponentFlags.Movement | ComponentFlags.Diet
        );

        entity.Kill();

        entity.IsAlive.Should().BeFalse();
        entity.Flags.Should().Be(ComponentFlags.None);
    }
}
