using System.Diagnostics;
using System.Runtime.CompilerServices;

// Prints deterministic facts about classic and runtime-async lowering:
// live stack trace frames and allocated bytes per call.
// Timings are printed for information only, they depend on the machine.

var stackDir = args.Length > 0 ? args[0] : "results";
Directory.CreateDirectory(stackDir);

PrintStacks(stackDir);
PrintAllocations();

static void PrintStacks(string dir)
{
    string classic = Classic.OuterAsync().GetAwaiter().GetResult();
    string runtime = Runtime.OuterAsync().GetAwaiter().GetResult();
    File.WriteAllText(Path.Combine(dir, "stack_classic.txt"), classic);
    File.WriteAllText(Path.Combine(dir, "stack_runtime.txt"), runtime);

    Console.WriteLine($"stack.classic.frames {Frames(classic)}");
    Console.WriteLine($"stack.runtime.frames {Frames(runtime)}");
    Console.WriteLine($"stack.classic.builder_frames {BuilderFrames(classic)}");
    Console.WriteLine($"stack.runtime.builder_frames {BuilderFrames(runtime)}");
}

static int Frames(string trace) =>
    trace.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(l => l.TrimStart().StartsWith("at "));

static int BuilderFrames(string trace) =>
    trace.Split('\n').Count(l => l.Contains("AsyncMethodBuilderCore"));

static void PrintAllocations()
{
    Measure("completed.classic", () => Chains.ClassicCompletedOuter().GetAwaiter().GetResult());
    Measure("completed.runtime", () => Chains.RuntimeCompletedOuter().GetAwaiter().GetResult());
    Measure("yielding.classic", () => Chains.ClassicYieldingOuter().GetAwaiter().GetResult(), iterations: 200_000);
    Measure("yielding.runtime", () => Chains.RuntimeYieldingOuter().GetAwaiter().GetResult(), iterations: 200_000);
    Measure("throw30.classic", () => Chains.Invoke(Chains.ClassicThrowAsync(30)), iterations: 20_000);
    Measure("throw30.runtime", () => Chains.Invoke(Chains.RuntimeThrowAsync(30)), iterations: 20_000);
}

static void Measure(string name, Func<int> op, int iterations = 2_000_000)
{
    // Warm up twice with a pause, so tiered compilation reaches tier 1 before measuring.
    for (int i = 0; i < 50_000; i++) op();
    Thread.Sleep(500);
    for (int i = 0; i < 50_000; i++) op();

    long before = GC.GetTotalAllocatedBytes(precise: true);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++) op();
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
    public static async Task<string> OuterAsync()
    {
        await Task.CompletedTask;
        return await MiddleAsync();
    }

    [RuntimeAsyncMethodGeneration(false)]
    static async Task<string> MiddleAsync()
    {
        await Task.CompletedTask;
        return await InnerAsync();
    }

    [RuntimeAsyncMethodGeneration(false)]
    static async Task<string> InnerAsync()
    {
        await Task.CompletedTask;
        return new StackTrace(fNeedFileInfo: false).ToString();
    }
}

static class Runtime
{
    public static async Task<string> OuterAsync()
    {
        await Task.CompletedTask;
        return await MiddleAsync();
    }

    static async Task<string> MiddleAsync()
    {
        await Task.CompletedTask;
        return await InnerAsync();
    }

    static async Task<string> InnerAsync()
    {
        await Task.CompletedTask;
        return new StackTrace(fNeedFileInfo: false).ToString();
    }
}

static class Chains
{
    static readonly Task<int> s_completed = Task.FromResult(42);

    public static int Invoke(Task<int> task)
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
    public static async Task<int> ClassicCompletedOuter() => await ClassicCompletedInner();

    [RuntimeAsyncMethodGeneration(false)]
    static async Task<int> ClassicCompletedInner() => await s_completed;

    public static async Task<int> RuntimeCompletedOuter() => await RuntimeCompletedInner();

    static async Task<int> RuntimeCompletedInner() => await s_completed;

    [RuntimeAsyncMethodGeneration(false)]
    public static async Task<int> ClassicYieldingOuter() => await ClassicYieldingInner();

    [RuntimeAsyncMethodGeneration(false)]
    static async Task<int> ClassicYieldingInner()
    {
        await Task.Yield();
        return 42;
    }

    public static async Task<int> RuntimeYieldingOuter() => await RuntimeYieldingInner();

    static async Task<int> RuntimeYieldingInner()
    {
        await Task.Yield();
        return 42;
    }

    [RuntimeAsyncMethodGeneration(false)]
    public static async Task<int> ClassicThrowAsync(int depth)
    {
        if (depth == 0)
            throw new InvalidOperationException("uh oh");
        return await ClassicThrowAsync(depth - 1);
    }

    public static async Task<int> RuntimeThrowAsync(int depth)
    {
        if (depth == 0)
            throw new InvalidOperationException("uh oh");
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
