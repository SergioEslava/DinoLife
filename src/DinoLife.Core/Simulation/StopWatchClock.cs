using System.Diagnostics;

public sealed class StopwatchClock : IClock
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    public double Now => _stopwatch.Elapsed.TotalSeconds;
}
