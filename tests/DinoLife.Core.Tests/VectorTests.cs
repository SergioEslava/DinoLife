using DinoLife.Core.Utils;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Utils;

public class VectorTests()
{
    [Fact]
    public void Constructor_Should_Create_Zero_Position()
    {
        var vector = new Vector2();

        (vector.X == 0f).Should().BeTrue();
        (vector.Y == 0f).Should().BeTrue();
    }

    [Fact]
    public void Correct_Normalization()
    {
        var vector = new Vector2(3,4);
        var vectorNorm = new Vector2(0.6f,0.8f);

        (vector.Normalized()==vectorNorm).Should().BeTrue();
    }
}