using System.Diagnostics;
using System.Runtime.CompilerServices;

// Prints deterministic facts about classic and runtime-async lowering:
// live stack trace frames and allocated bytes per call.
// Timings are printed for information only, they depend on the machine.

var stackDir = args.Length > 0 ? args[0] : "results";
Directory.CreateDirectory(stackDir);

PrintStack("classic", Classic.OuterAsync().GetAwaiter().GetResult(), stackDir);
PrintStack("runtime", Runtime.OuterAsync().GetAwaiter().GetResult(), stackDir);

Measure("completed.classic", Chains.ClassicCompletedOuter);
Measure("completed.runtime", Chains.RuntimeCompletedOuter);
Measure("yielding.classic", Chains.ClassicYieldingOuter, iterations: 200_000);
Measure("yielding.runtime", Chains.RuntimeYieldingOuter, iterations: 200_000);
Measure("throw30.classic", () => Chains.ClassicThrowAsync(30, yield: false), iterations: 20_000);
Measure("throw30.runtime", () => Chains.RuntimeThrowAsync(30, yield: false), iterations: 20_000);

static void PrintStack(string name, StackTrace trace, string dir)
{
    File.WriteAllText(Path.Combine(dir, $"stack_{name}.txt"), trace.ToString());

    // Frames are classified by declaring type, not by the localized text of ToString().
    int builderFrames = trace.GetFrames().Count(f =>
        f.GetMethod()?.DeclaringType?.FullName?.StartsWith("System.Runtime.CompilerServices.AsyncMethodBuilderCore") == true);
    Console.WriteLine($"stack.{name}.frames {trace.FrameCount}");
    Console.WriteLine($"stack.{name}.builder_frames {builderFrames}");
}

static void Measure(string name, Func<Task<int>> op, int iterations = 2_000_000)
{
    // Warm up twice with a pause, so tiered compilation reaches tier 1 before measuring.
    // Never warm up longer than we measure: throw30 and yielding ops are expensive.
    int warmup = Math.Min(50_000, iterations);
    for (int i = 0; i < warmup; i++) Chains.Invoke(op());
    Thread.Sleep(500);
    for (int i = 0; i < warmup; i++) Chains.Invoke(op());

    long before = GC.GetTotalAllocatedBytes(precise: true);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++) Chains.Invoke(op());
    sw.Stop();
    long after = GC.GetTotalAllocatedBytes(precise: true);

    long bytesPerOp = (long)Math.Round((after - before) / (double)iterations);
    double nsPerOp = sw.Elapsed.TotalNanoseconds / iterations;
    Console.WriteLine($"alloc.{name}.bytes_per_op {bytesPerOp}");
    Console.WriteLine($"info.{name}.ns_per_op {nsPerOp:F1}");
}

static class Classic
{
    [RuntimeAsyncMethodGeneration(false)]
    public static async Task<StackTrace> OuterAsync()
    {
        await Task.CompletedTask;
        return await MiddleAsync();
    }

    [RuntimeAsyncMethodGeneration(false)]
    static async Task<StackTrace> MiddleAsync()
    {
        await Task.CompletedTask;
        return await InnerAsync();
    }

    [RuntimeAsyncMethodGeneration(false)]
    static async Task<StackTrace> InnerAsync()
    {
        await Task.CompletedTask;
        return new StackTrace(fNeedFileInfo: false);
    }
}

static class Runtime
{
    public static async Task<StackTrace> OuterAsync()
    {
        await Task.CompletedTask;
        return await MiddleAsync();
    }

    static async Task<StackTrace> MiddleAsync()
    {
        await Task.CompletedTask;
        return await InnerAsync();
    }

    static async Task<StackTrace> InnerAsync()
    {
        await Task.CompletedTask;
        return new StackTrace(fNeedFileInfo: false);
    }
}
