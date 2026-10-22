#!/usr/bin/env bash
# Builds everything from scratch, writes results/actual.txt and checks it against expected.txt.
set -euo pipefail
cd "$(dirname "$0")"

rm -rf results build
mkdir -p results build

dotnet --version | sed 's/^/info.sdk /' > results/sdk.txt

echo "== AsyncProbe"
dotnet run -c Release --project src/AsyncProbe -- results | tee results/probe.txt

echo "== SizeProbe"
dotnet build src/SizeProbe -c Release -p:RuntimeAsync=false -o build/classic --no-incremental -nologo -v q
dotnet build src/SizeProbe -c Release -p:RuntimeAsync=true -o build/runtime --no-incremental -nologo -v q
size() { wc -c < "$1" | tr -d ' '; }
{
  echo "size.classic.bytes $(size build/classic/SizeProbe.dll)"
  echo "size.runtime.bytes $(size build/runtime/SizeProbe.dll)"
} | tee results/size.txt

grep -h -E '^(stack|alloc|size)\.' results/probe.txt results/size.txt > results/actual.txt

echo "== Check against expected.txt"
python3 check.py expected.txt results/actual.txt
