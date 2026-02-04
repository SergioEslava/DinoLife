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

    [Fact]
    public void Multiply_By_Scalar_Should_Scale_Vector()
    {
        var vector = new Vector2(2, 3);

        Vector2 result = vector * 2f;

        result.X.Should().Be(4f);
        result.Y.Should().Be(6f);
    }

    [Fact]
    public void Multiply_Scalar_By_Vector_Should_Scale_Vector()
    {
        var vector = new Vector2(2, 3);

        Vector2 result = 2f * vector;

        result.Should().Be(new Vector2(4, 6));
    }

    [Fact]
    public void Magnitude_Should_Be_Correct()
    {
        var vector = new Vector2(3, 4);

        float magnitude = vector.Magnitude();

        magnitude.Should().Be(5f);
    }

    [Fact]
    public void Normalizing_Zero_Vector_Should_Return_Zero_Vector()
    {
        var vector = new Vector2(0, 0);

        Vector2 normalized = vector.Normalized();

        normalized.Should().Be(new Vector2(0, 0));
    }

    [Fact]
    public void Equality_Operator_Should_Return_True_For_Same_Values()
    {
        var a = new Vector2(1.5f, 2.5f);
        var b = new Vector2(1.5f, 2.5f);

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Inequality_Operator_Should_Return_True_For_Different_Values()
    {
        var a = new Vector2(1, 2);
        var b = new Vector2(2, 1);

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_Match_Operator_Equality()
    {
        var a = new Vector2(5, 5);
        var b = new Vector2(5, 5);

        a.Equals(b).Should().BeTrue();
    }

}