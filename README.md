# dotnet-runtime-async-demo

[![check](https://github.com/sproshchaev/dotnet-runtime-async-demo/actions/workflows/check.yml/badge.svg)](https://github.com/sproshchaev/dotnet-runtime-async-demo/actions/workflows/check.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Runtime Async in .NET 11: what changes when the async state machine moves from the C# compiler into the runtime. Classic and runtime-async methods run side by side in one process on the same .NET 11 runtime and differ only in how the compiler lowers them.

Article (in Russian): https://habr.com/ru/companies/otus/articles/1088900/

## Run

Requires .NET 11 SDK (RC2 or later, pinned in `global.json`) and Python 3.

```bash
./run.sh
```

The script builds everything from scratch, writes `results/actual.txt` and checks it against `expected.txt`. Exit code 1 on any mismatch.

Benchmarks (timings depend on the machine and are not checked):

```bash
dotnet run -c Release --project bench/AsyncBench -- --filter "*"
```

## Results

CI run on GitHub Actions `ubuntu-24.04`, .NET SDK 11.0.100-rc.2.26504.105, BenchmarkDotNet 0.16.0-preview.2. Allocations are stable between runs, timings are not and are shown for orientation only.

Live stack trace from three nested async methods (`src/AsyncProbe`, files `stack_classic.txt` and `stack_runtime.txt` in the `results` artifact):

| Lowering | Frames | AsyncMethodBuilderCore frames |
|---|---|---|
| Classic | 13 | 3 |
| Runtime async | 4 | 0 |

Ten nested async methods (`src/SizeProbe`):

| Lowering | SizeProbe.dll |
|---|---|
| Classic | 10,752 bytes |
| Runtime async | 5,632 bytes |

Two-level chain, BenchmarkDotNet out of process (default):

| Method | Mean | Allocated |
|---|---|---|
| ClassicCompleted | 26.95 ns | 144 B |
| RuntimeCompleted | 10.72 ns | 0 B |
| ClassicYielding | 447.47 ns | 248 B |
| RuntimeYielding | 288.78 ns | 168 B |

The same chain with `--inProcess`, and the same picture in `src/AsyncProbe`, which also runs in one process:

| Method | Allocated in process |
|---|---|
| ClassicCompleted | 144 B |
| RuntimeCompleted | 72 B |
| ClassicYielding | 192 B |
| RuntimeYielding | 240 B |

Exception thrown at the bottom of a recursive chain, out of process:

| Depth | Yield | Classic | Runtime async |
|---|---|---|---|
| 1 | no | 1.6 KB | 1.16 KB |
| 1 | yes | 1.68 KB | 1.4 KB |
| 10 | no | 15.13 KB | 2.13 KB |
| 30 | no | 84.2 KB | 5.71 KB |
| 30 | yes | 85.08 KB | 7.7 KB |

## What this shows and what it does not

- The out-of-process numbers match the ones published by the .NET team for .NET 11. The in-process numbers on RC2 do not: the synchronously completed chain still allocates 72 B, and the yielding chain allocates more than the classic one. The reason is not established here. The practical point is that an async microbenchmark on RC2 depends on how it is hosted, so compare like with like.
- These are microbenchmarks. An I/O-bound service waits milliseconds on the network, the savings here are nanoseconds and bytes per call.
- Runtime Async is a preview feature in .NET 11, results may change before and after GA.

## Structure

- `shared/Chains.cs` — the async chains measured by both the probe and the benchmarks
- `src/AsyncProbe` — live stack trace frames and allocated bytes per call, classic vs runtime async
- `src/SizeProbe` — ten nested async methods, built twice to compare assembly size
- `bench/AsyncBench` — BenchmarkDotNet: completed and yielding chains, exception at depth 1, 10, 30
- `expected.txt` — invariants that hold on any OS and CPU
- `check.py` — compares `results/actual.txt` with `expected.txt`

## License

MIT, see [LICENSE](LICENSE).
