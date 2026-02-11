using DinoLife.Rendering.Terminal;
using FluentAssertions;
using Xunit;

namespace DinoLife.Rendering.Tests;

public sealed class DoubleBufferTests
{
    [Fact]
    public void NewBuffer_ShouldStartWithoutDiff()
    {
        var buffer = new DoubleBuffer(8, 4, ConsoleColor.Gray);

        buffer.GetDiff().Should().BeEmpty();
    }

    [Fact]
    public void SetBackCell_ShouldEmitSingleDiff_WhenCellChanges()
    {
        var buffer = new DoubleBuffer(8, 4, ConsoleColor.Gray);

        buffer.SetBackCell(2, 1, 'H', ConsoleColor.Green);
        CellDiff[] diff = buffer.GetDiff().ToArray();

        diff.Should().HaveCount(1);
        diff[0].X.Should().Be(2);
        diff[0].Y.Should().Be(1);
        diff[0].Character.Should().Be('H');
        diff[0].Color.Should().Be(ConsoleColor.Green);
    }

    [Fact]
    public void SwapAndClearBack_ShouldEmitOnlyChangedCells_AndThenNoDiffOnStableFrame()
    {
        var buffer = new DoubleBuffer(6, 3, ConsoleColor.Gray);

        buffer.SetBackCell(1, 1, 'X', ConsoleColor.Red);
        buffer.Swap();

        buffer.ClearBack();
        CellDiff[] changedBackToDefault = buffer.GetDiff().ToArray();
        changedBackToDefault.Should().ContainSingle(d => d.X == 1 && d.Y == 1 && d.Character == ' ' && d.Color == ConsoleColor.Gray);

        buffer.Swap();
        buffer.ClearBack();
        buffer.GetDiff().Should().BeEmpty();
    }

    [Fact]
    public void SetBackCell_OutOfBounds_ShouldBeIgnored()
    {
        var buffer = new DoubleBuffer(5, 2, ConsoleColor.Gray);

        buffer.SetBackCell(-1, 0, 'A', ConsoleColor.White);
        buffer.SetBackCell(0, -1, 'A', ConsoleColor.White);
        buffer.SetBackCell(5, 0, 'A', ConsoleColor.White);
        buffer.SetBackCell(0, 2, 'A', ConsoleColor.White);

        buffer.GetDiff().Should().BeEmpty();
    }

    [Fact]
    public void Resize_ShouldResetFrontAndBackState()
    {
        var buffer = new DoubleBuffer(5, 2, ConsoleColor.Gray);
        buffer.SetBackCell(0, 0, 'A', ConsoleColor.White);
        buffer.Swap();

        buffer.Resize(7, 3);

        buffer.Width.Should().Be(7);
        buffer.Height.Should().Be(3);
        buffer.GetDiff().Should().BeEmpty();
    }
}
