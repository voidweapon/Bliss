using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;

namespace BlissBenchMark;

/// <summary>Measures cancellation of active and previously cancelled World timers.</summary>
[MemoryDiagnoser]
public class GameplayWorldTimerCancellationBenchmark
{
    private const int SMALL_TIMER_COUNT = 100;
    private const int LARGE_TIMER_COUNT = 5000;
    private const int SINGLE_BUCKET = 1;
    private const int MANY_BUCKETS = 100;
    private const long WORLD_ID = 1;
    private const long WORLD_INSTANCE_ID = 2;
    private const long FUTURE_DELAY_MILLISECONDS = 10000;
    private static readonly ConstructorInfo worldConstructor =
        typeof(GameplayWorld).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(Root), typeof(long), typeof(long), typeof(string) }, null)
        ?? throw new InvalidOperationException("GameplayWorld constructor was not found.");
    private static readonly Action callback = doNothing;
    private GameplayWorldTimer timer = null!;
    private long[] timerIds = null!;

    /// <summary>Number of timers cancelled in a measured batch.</summary>
    [Params(SMALL_TIMER_COUNT, LARGE_TIMER_COUNT)]
    public int TimerCount { get; set; }

    /// <summary>Number of distinct due-time buckets used by the timers.</summary>
    [Params(SINGLE_BUCKET, MANY_BUCKETS)]
    public int BucketCount { get; set; }

    /// <summary>Whether the identifiers already refer to cancelled timers.</summary>
    [Params(false, true)]
    public bool Stale { get; set; }

    /// <summary>Prepares a real timer service and identifiers outside the measured operation.</summary>
    [IterationSetup]
    public void Setup()
    {
        GameplayWorld world = (GameplayWorld)worldConstructor.Invoke(new object?[]
            { null, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorldTimerCancellationBenchmark) });
        this.timer = world.Timer;
        this.timerIds = new long[this.TimerCount];
        long dueTime = TimeInfo.Instance.ServerFrameTime() + FUTURE_DELAY_MILLISECONDS;
        for (int index = 0; index < this.TimerCount; index++)
        {
            long timerId = this.timer.ScheduleOnceAt(dueTime + index % this.BucketCount, callback);
            this.timerIds[index] = timerId;
            if (this.Stale && !this.timer.Cancel(ref timerId))
            {
                throw new InvalidOperationException("Failed to prepare a stale timer identifier.");
            }
        }
    }

    /// <summary>Cancels each timer and clears the caller's stored identifier.</summary>
    [Benchmark]
    public void CancelTimers()
    {
        for (int index = 0; index < this.timerIds.Length; index++)
        {
            bool cancelled = this.timer.Cancel(ref this.timerIds[index]);
            if (cancelled == this.Stale)
            {
                throw new InvalidOperationException("Unexpected timer cancellation result.");
            }
        }
    }

    /// <summary>Checks that all caller identifiers were cleared.</summary>
    [IterationCleanup]
    public void VerifyCancellation()
    {
        foreach (long timerId in this.timerIds)
        {
            if (timerId != default)
            {
                throw new InvalidOperationException("Timer identifier was not cleared.");
            }
        }
    }

    private static void doNothing()
    {
    }
}
