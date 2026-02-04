using DinoLife.Core.Entities;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Entities;

public class EntityTests
{
    [Fact]
    public void Constructor_Should_Create_Alive_Entity()
    {
        var entity = new Entity(EntityType.Carnivore, ComponentFlags.Movement);

        entity.IsAlive.Should().BeTrue();
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Has_Should_Return_True_When_Flag_Is_Present()
    {
        var entity = new Entity(
            EntityType.Carnivore,
            ComponentFlags.Movement | ComponentFlags.Diet
        );

        entity.Has(ComponentFlags.Movement).Should().BeTrue();
        entity.Has(ComponentFlags.Reproduction).Should().BeFalse();
    }

    [Fact]
    public void Kill_Should_Mark_Entity_As_Dead_And_Remove_Flags()
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
