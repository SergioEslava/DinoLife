using DinoLife.Core.Utils;
using FluentAssertions;
using Xunit;

namespace DinoLife.Tests.Utils;

public class Vector2Tests
{
    [Fact]
    public void Vector2_Constructor_CreatesZeroPosition_WhenNoArguments()
    {
        var vector = new Vector2();

        vector.X.Should().Be(0f);
        vector.Y.Should().Be(0f);
    }

    [Fact]
    public void Vector2_Normalized_ReturnsUnitVector_WhenVectorIsNonZero()
    {
        var vector = new Vector2(3, 4);
        var expected = new Vector2(0.6f, 0.8f);

        vector.Normalized().Should().Be(expected);
    }

    [Fact]
    public void Vector2_MultiplyByScalar_ScalesVector_WhenVectorIsNonZero()
    {
        var vector = new Vector2(2, 3);

        Vector2 result = vector * 2f;

        result.X.Should().Be(4f);
        result.Y.Should().Be(6f);
    }

    [Fact]
    public void Vector2_ScalarMultiply_VectorScalesCorrectly_WhenVectorIsNonZero()
    {
        var vector = new Vector2(2, 3);

        Vector2 result = 2f * vector;

        result.Should().Be(new Vector2(4, 6));
    }

    [Fact]
    public void Vector2_Magnitude_ReturnsCorrectValue_WhenVectorIsNonZero()
    {
        var vector = new Vector2(3, 4);

        float magnitude = vector.Magnitude();

        magnitude.Should().Be(5f);
    }

    [Fact]
    public void Vector2_Normalized_ReturnsZeroVector_WhenVectorIsZero()
    {
        var vector = new Vector2(0, 0);

        vector.Normalized().Should().Be(new Vector2(0, 0));
    }

    [Fact]
    public void Vector2_EqualityOperator_ReturnsTrue_WhenVectorsHaveSameValues()
    {
        var a = new Vector2(1.5f, 2.5f);
        var b = new Vector2(1.5f, 2.5f);

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Vector2_InequalityOperator_ReturnsTrue_WhenVectorsHaveDifferentValues()
    {
        var a = new Vector2(1, 2);
        var b = new Vector2(2, 1);

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Vector2_EqualsMethod_MatchesOperatorEquality_WhenVectorsHaveSameValues()
    {
        var a = new Vector2(5, 5);
        var b = new Vector2(5, 5);

        a.Equals(b).Should().BeTrue();
    }
}
