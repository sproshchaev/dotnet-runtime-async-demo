// Ten Task<int>-returning async methods, each awaiting the next.
// The same source is compiled with classic lowering and with runtime async,
// run.sh compares the size of SizeProbe.dll.

Console.WriteLine(await Layers.Layer0());

public static class Layers
{
    public static async Task<int> Layer0() => await Layer1();
    private static async Task<int> Layer1() => await Layer2();
    private static async Task<int> Layer2() => await Layer3();
    private static async Task<int> Layer3() => await Layer4();
    private static async Task<int> Layer4() => await Layer5();
    private static async Task<int> Layer5() => await Layer6();
    private static async Task<int> Layer6() => await Layer7();
    private static async Task<int> Layer7() => await Layer8();
    private static async Task<int> Layer8() => await Layer9();

    private static async Task<int> Layer9()
    {
        await Task.Yield();
        return 42;
    }
}
