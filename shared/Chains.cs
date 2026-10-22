using System.Runtime.CompilerServices;

// Chains shared by src/AsyncProbe and bench/AsyncBench, so both measure the same code.
// Methods marked [RuntimeAsyncMethodGeneration(false)] keep the classic compiler lowering,
// the others are compiled with runtime-async=on.
public static class Chains
{
    private static readonly Task<int> s_completed = Task.FromResult(42);

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
    private static async Task<int> ClassicCompletedInner() => await s_completed;

    public static async Task<int> RuntimeCompletedOuter() => await RuntimeCompletedInner();

    private static async Task<int> RuntimeCompletedInner() => await s_completed;

    [RuntimeAsyncMethodGeneration(false)]
    public static async Task<int> ClassicYieldingOuter() => await ClassicYieldingInner();

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> ClassicYieldingInner()
    {
        await Task.Yield();
        return 42;
    }

    public static async Task<int> RuntimeYieldingOuter() => await RuntimeYieldingInner();

    private static async Task<int> RuntimeYieldingInner()
    {
        await Task.Yield();
        return 42;
    }

    [RuntimeAsyncMethodGeneration(false)]
    public static async Task<int> ClassicThrowAsync(int depth, bool yield)
    {
        if (depth == 0)
        {
            if (yield) await Task.Yield();
            throw new InvalidOperationException("uh oh");
        }
        return await ClassicThrowAsync(depth - 1, yield);
    }

    public static async Task<int> RuntimeThrowAsync(int depth, bool yield)
    {
        if (depth == 0)
        {
            if (yield) await Task.Yield();
            throw new InvalidOperationException("uh oh");
        }
        return await RuntimeThrowAsync(depth - 1, yield);
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
