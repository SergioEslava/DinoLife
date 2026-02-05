using BenchmarkDotNet.Running;

namespace DinoLife.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Ejecuta todos los benchmarks en este assembly
        BenchmarkRunner.Run<SimulationEngineBenchmark>();
        BenchmarkRunner.Run<SimulationEngineFullLoadBenchmark>();
    }
}
