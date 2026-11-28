# Benchmark workflows

`BlissBenchMark` has two run modes. Exploratory runs are convenient for local iteration, but their results are not suitable for formal performance claims:

```sh
dotnet run --configuration Release --project BlissBenchMark -- explore --filter '*Timer*'
```

Formal runs use a fixed BenchmarkDotNet configuration and require a unique run ID, a workload description, a preparation description, and an explicit filter:

```sh
dotnet run --configuration Release --project BlissBenchMark -- run \
  --id timer-update-2026-11-20 \
  --workload "update 5000 timers across 100 buckets" \
  --preparation "construct timer state outside the measured method" \
  -- --filter '*Timer*'
```

Formal runs must launch from a Release build and use .NET 8, three launches, five warmup iterations, and ten measured iterations. They accept only the BenchmarkDotNet `--filter` argument, preventing command-line overrides to the fixed configuration. BenchmarkDotNet output and a `manifest.json` containing the commit, worktree fingerprint, workload, preparation, full formal configuration (including logger, columns, exporter, and diagnoser), operating system, architecture, processor description and count, .NET environment, benchmark names, actual parameters, and mean durations are written under `BlissBenchMark/artifacts/<run-id>/`. This directory is intentionally ignored by Git; preserve the artifacts locally when a run needs to be reviewed or compared. The manifest does not contain user names or absolute paths.

Compare only runs with equivalent workload and preparation descriptions, matching BenchmarkDotNet configuration and environment, and exactly matching benchmark cases and parameters:

```sh
dotnet run --configuration Release --project BlissBenchMark -- compare \
  --baseline timer-update-before \
  --candidate timer-update-after
```

If there is no comparable baseline, record that explicitly instead of reporting a percentage:

```sh
dotnet run --configuration Release --project BlissBenchMark -- no-baseline \
  --candidate timer-update-after \
  --reason "No equivalent implementation exists"
```

Exploratory runs do not create formal manifests. A formal manifest is written only after BenchmarkDotNet completes successfully. Benchmark runs are intentionally not part of routine pull-request CI. For a performance conclusion, manually record the run IDs, measured result, environment, and relevant limitations in the project documentation alongside the change.

## Main internal gateway results for #39

Formal comparison: `issue39-main-gateway-baseline-v2` against `issue39-main-gateway-candidate-v4`. Both runs used the workload “MessageSender RPC and one-way batches (1 and 16) over loopback UDP using a local peer” and prepared Root, Main, network drivers, and peer before each measured iteration. Configuration was BenchmarkDotNet 0.15.2 on .NET 8.0.19, three launches, five warmups, and ten measurement iterations, with `MemoryDiagnoser`.

Environment: macOS Darwin 25.6.0, Apple M1 Pro (10 processors), .NET SDK 8.0.413. Mean time and managed allocation are per benchmark invocation (one batch).

| Route | Batch | Baseline mean | Candidate mean | Time change | Baseline allocated | Candidate allocated |
|---|---:|---:|---:|---:|---:|---:|
| RPC | 1 | 11.43 ms | 10.94 ms | -4.25% | 33,127 B | 58,281 B |
| RPC | 16 | 13.58 ms | 16.27 ms | +19.82% | 419,545 B | 477,442 B |
| One-way | 1 | 9.96 ms | 9.94 ms | -0.20% | 41,863 B | 42,009 B |
| One-way | 16 | 9.97 ms | 9.98 ms | +0.12% | 202,698 B | 230,297 B |

One-way timing is effectively unchanged. RPC batch 1 is also inconclusive: baseline and candidate 99.9% confidence intervals overlap (10.30–12.55 ms and 10.25–11.63 ms). RPC batch 16 is noisy and the intervals overlap (11.16–15.99 ms and 13.64–18.89 ms); do not infer a definite timing regression from its +19.82% mean. Managed allocation increased in all candidate cases except that one-way batch 1 remained nearly flat; most notably RPC batch 1 increased by about 76%. This loopback benchmark includes UDP scheduling and polling and does not isolate gateway CPU cost. Treat the results as an end-to-end local comparison, not a claim about production network latency.

## Internal message enqueue results for #40

Formal comparison: `issue40-enqueue-baseline-v2` against `issue40-enqueue-candidate-v2`. Both runs measured one-message and 16-message internal queue admission batches, draining the target queue immediately after each batch. The live unmanaged Root route, process options, reusable message, and drain list were prepared outside measurement. Configuration was BenchmarkDotNet 0.15.2 on .NET 8.0.19, three launches, five warmups, and ten measurement iterations, with `MemoryDiagnoser`.

Environment: macOS Darwin 25.6.0, Apple M1 Pro (10 processors), .NET SDK 8.0.413. Mean time and managed allocation are per message; confidence intervals are 99.9%.

| Workload | Baseline mean | Candidate mean | Time change | Baseline allocated | Candidate allocated |
|---|---:|---:|---:|---:|---:|
| Single-message admission and drain | 44.87 ns | 44.16 ns | -1.58% | 0 B | 0 B |
| 16-message admission batch and drain | 39.68 ns | 39.73 ns | +0.13% | 0 B | 0 B |

Both confidence intervals overlap (single-message baseline 44.13–45.61 ns, candidate 43.98–44.35 ns; batch baseline 39.58–39.77 ns, candidate 39.62–39.84 ns). The measurements do not show a clear timing change or managed allocation from the admission lock. This benchmark includes queue draining and does not isolate concurrent stop transitions or network dispatch.
