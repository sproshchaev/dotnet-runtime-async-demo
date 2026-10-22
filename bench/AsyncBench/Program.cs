using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Runtime.CompilerServices;

// dotnet run -c Release --project bench/AsyncBench -- --filter "*"
// Default: in-process toolchain. Add --outofproc to run every benchmark in a separate
// generated process, as BenchmarkDotNet does by default.
// In both modes classic and runtime-async methods run on the same .NET 11 runtime
// and differ only in how the compiler lowers them.
bool outOfProc = args.Contains("--outofproc");
var job = outOfProc ? Job.Default : Job.Default.WithToolchain(InProcessEmitToolchain.Instance);
var config = DefaultConfig.Instance.AddJob(job).WithOptions(ConfigOptions.DisableOptimizationsValidator);
BenchmarkSwitcher.FromAssembly(typeof(ChainBenchmarks).Assembly)
    .Run(args.Where(a => a != "--outofproc").ToArray(), config);

[MemoryDiagnoser(false)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class ChainBenchmarks
{
    private static readonly Task<int> s_completed = Task.FromResult(42);

    [Benchmark(Baseline = true), BenchmarkCategory("Completed")]
    public Task<int> ClassicCompleted() => ClassicCompletedOuter();

    [Benchmark, BenchmarkCategory("Completed")]
    public Task<int> RuntimeCompleted() => RuntimeCompletedOuter();

    [Benchmark(Baseline = true), BenchmarkCategory("Yielding")]
    public Task<int> ClassicYielding() => ClassicYieldingOuter();

    [Benchmark, BenchmarkCategory("Yielding")]
    public Task<int> RuntimeYielding() => RuntimeYieldingOuter();

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> ClassicCompletedOuter() => await ClassicCompletedInner();

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> ClassicCompletedInner() => await s_completed;

    private static async Task<int> RuntimeCompletedOuter() => await RuntimeCompletedInner();

    private static async Task<int> RuntimeCompletedInner() => await s_completed;

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> ClassicYieldingOuter() => await ClassicYieldingInner();

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> ClassicYieldingInner()
    {
        await Task.Yield();
        return 42;
    }

    private static async Task<int> RuntimeYieldingOuter() => await RuntimeYieldingInner();

    private static async Task<int> RuntimeYieldingInner()
    {
        await Task.Yield();
        return 42;
    }
}

[MemoryDiagnoser(false)]
[HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class ThrowBenchmarks
{
    [Params(1, 10, 30)]
    public int Depth;

    [Params(false, true)]
    public bool Yield;

    [Benchmark(Baseline = true)]
    public int Classic() => Invoke(ClassicThrowAsync(Depth));

    [Benchmark]
    public int Runtime() => Invoke(RuntimeThrowAsync(Depth));

    private static int Invoke(Task<int> task)
    {
        try
        {
            return task.GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }

    [RuntimeAsyncMethodGeneration(false)]
    private async Task<int> ClassicThrowAsync(int depth)
    {
        if (depth == 0)
        {
            if (Yield) await Task.Yield();
            throw new InvalidOperationException("uh oh");
        }
        return await ClassicThrowAsync(depth - 1);
    }

    private async Task<int> RuntimeThrowAsync(int depth)
    {
        if (depth == 0)
        {
            if (Yield) await Task.Yield();
            throw new InvalidOperationException("uh oh");
        }
        return await RuntimeThrowAsync(depth - 1);
    }
}

namespace System.Runtime.CompilerServices
{
    // Experimental per-method switch recognized by the C# compiler by name and signature.
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) : Attribute
    {
        public bool RuntimeAsync => runtimeAsync;
    }
}
