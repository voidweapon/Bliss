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
