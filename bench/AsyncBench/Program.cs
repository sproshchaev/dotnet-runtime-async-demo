using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

// dotnet run -c Release --project bench/AsyncBench -- --filter "*"
// Out of process by default; add --inProcess to run in the host process.
// Classic and runtime-async methods (shared/Chains.cs) run on the same .NET 11 runtime
// and differ only in how the compiler lowers them.
var config = DefaultConfig.Instance
    .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(displayGenColumns: false)))
    .HideColumns(Column.Job, Column.Error, Column.StdDev, Column.Median, Column.RatioSD);
BenchmarkSwitcher.FromAssembly(typeof(ChainBenchmarks).Assembly).Run(args, config);

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ChainBenchmarks
{
    [Benchmark(Baseline = true), BenchmarkCategory("Completed")]
    public Task<int> ClassicCompleted() => Chains.ClassicCompletedOuter();

    [Benchmark, BenchmarkCategory("Completed")]
    public Task<int> RuntimeCompleted() => Chains.RuntimeCompletedOuter();

    [Benchmark(Baseline = true), BenchmarkCategory("Yielding")]
    public Task<int> ClassicYielding() => Chains.ClassicYieldingOuter();

    [Benchmark, BenchmarkCategory("Yielding")]
    public Task<int> RuntimeYielding() => Chains.RuntimeYieldingOuter();
}

public class ThrowBenchmarks
{
    [Params(1, 10, 30)]
    public int Depth;

    [Params(false, true)]
    public bool Yield;

    [Benchmark(Baseline = true)]
    public int Classic() => Chains.Invoke(Chains.ClassicThrowAsync(Depth, Yield));

    [Benchmark]
    public int Runtime() => Chains.Invoke(Chains.RuntimeThrowAsync(Depth, Yield));
}
