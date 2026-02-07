using System.Diagnostics;

namespace DinoLife.Core.Simulation;

/// <summary>
/// Clock implementation based on <see cref="Stopwatch"/> which provides a monotonic
/// time source suitable for simulation stepping.
/// </summary>
public sealed class StopwatchClock : IClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Current elapsed time in seconds since the clock was created.
    /// </summary>
    public double Now => _stopwatch.Elapsed.TotalSeconds;
}
