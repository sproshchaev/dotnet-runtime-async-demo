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

Filled in from the CI run.

## Structure

- `shared/Chains.cs` — the async chains measured by both the probe and the benchmarks
- `src/AsyncProbe` — live stack trace frames and allocated bytes per call, classic vs runtime async
- `src/SizeProbe` — ten nested async methods, built twice to compare assembly size
- `bench/AsyncBench` — BenchmarkDotNet: completed and yielding chains, exception at depth 1, 10, 30
- `expected.txt` — invariants that hold on any OS and CPU
- `check.py` — compares `results/actual.txt` with `expected.txt`

## License

MIT, see [LICENSE](LICENSE).
