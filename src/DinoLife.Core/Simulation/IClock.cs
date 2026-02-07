namespace DinoLife.Core.Simulation;

/// <summary>
/// Simple clock abstraction used by the simulation engine to read time.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Current time in seconds from an arbitrary epoch. Should be monotonic.
    /// </summary>
    double Now { get; }
}
