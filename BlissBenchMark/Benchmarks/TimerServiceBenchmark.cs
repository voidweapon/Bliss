using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Bliss;
using Bliss.Timer;

namespace BlissBenchMark;

/// <summary>
/// Compares the real timer services using matching ownerless wait workloads.
/// GameplayWorldTimer also contains the world's fixed-step marker timer, so its update includes that extra timer.
/// This does not include world lifecycle, owner validation, or LogicUpdate dispatch.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class TimerServiceBenchmark
{
    private const int INITIAL_SERVER_TIME = 1000;
    private const int FIRST_DUE_TIME = 1010;
    private const int UPDATE_SERVER_TIME = 2000;
    private const int FIRST_FUTURE_DUE_TIME = 3000;
    private const int PERCENTAGE_BASE = 100;
    private const int SINGLE_BUCKET = 1;
    private const int MANY_BUCKETS = 100;
    private const int SMALL_TIMER_COUNT = 100;
    private const int LARGE_TIMER_COUNT = 5000;
    private const int SPARSE_DUE_PERCENTAGE = 1;
    private const int ALL_DUE_PERCENTAGE = 100;
    private const long WORLD_ID = 1;
    private const long WORLD_INSTANCE_ID = 2;
    private static readonly ConstructorInfo worldConstructor =
        typeof(GameplayWorld).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(Root), typeof(long), typeof(long), typeof(string) },
            null) ?? throw new InvalidOperationException("GameplayWorld constructor was not found.");
    private static readonly Action<TimerComponent> updateTimerComponent =
        (typeof(TimerComponentSystems).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TimerComponent update was not found."))
            .CreateDelegate<Action<TimerComponent>>();
    private static readonly Action<GameplayWorldTimer> updateGameplayTimer =
        (typeof(GameplayWorldTimer).GetMethod("update", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GameplayWorldTimer update was not found."))
            .CreateDelegate<Action<GameplayWorldTimer>>();

    private TimerComponent timerComponent = null!;
    private GameplayWorldTimer gameplayTimer = null!;

    /// <summary>Number of distinct due timestamps in the workload.</summary>
    [Params(SINGLE_BUCKET, MANY_BUCKETS)]
    public int BucketCount { get; set; }

    /// <summary>Number of waits registered for each workload.</summary>
    [Params(SMALL_TIMER_COUNT, LARGE_TIMER_COUNT)]
    public int TimerCount { get; set; }

    /// <summary>Percentage of waits due when the update runs.</summary>
    [Params(SPARSE_DUE_PERCENTAGE, ALL_DUE_PERCENTAGE)]
    public int DuePercentage { get; set; }

    /// <summary>Checks that both real services complete waits only after their due time.</summary>
    [GlobalSetup]
    public void ValidateWorkload()
    {
        this.Setup();
        var componentWait = this.timerComponent.WaitUntilAsync(FIRST_DUE_TIME);
        var gameplayWait = this.gameplayTimer.WaitUntilAsync(FIRST_DUE_TIME);
        if (componentWait.IsCompleted || gameplayWait.IsCompleted)
        {
            throw new InvalidOperationException("Timer waits completed before their due time.");
        }

        setServerTime(UPDATE_SERVER_TIME);
        updateTimerComponent(this.timerComponent);
        updateGameplayTimer(this.gameplayTimer);
        if (!componentWait.IsCompleted || !gameplayWait.IsCompleted ||
            !componentWait.GetResult() || !gameplayWait.GetResult())
        {
            throw new InvalidOperationException("Timer waits did not complete successfully.");
        }
    }

    /// <summary>Creates fresh services outside the measurement.</summary>
    [IterationSetup(Targets = new[] { nameof(TimerComponentRegistration), nameof(GameplayWorldTimerRegistration) })]
    public void Setup()
    {
        setServerTime(INITIAL_SERVER_TIME);
        this.timerComponent = new TimerComponent();
        // Ownerless scheduling does not use Root; avoiding one keeps lifecycle work outside this benchmark.
        var world = (GameplayWorld)worldConstructor.Invoke(new object?[]
            { null, WORLD_ID, WORLD_INSTANCE_ID, nameof(TimerServiceBenchmark) });
        this.gameplayTimer = world.Timer;
    }

    /// <summary>Registers waits through TimerComponent.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Registration")]
    public void TimerComponentRegistration()
    {
        for (int index = 0; index < this.TimerCount; index++)
        {
            _ = this.timerComponent.WaitUntilAsync(this.dueTime(index));
        }
    }

    /// <summary>Registers waits through GameplayWorldTimer.</summary>
    [Benchmark]
    [BenchmarkCategory("Registration")]
    public void GameplayWorldTimerRegistration()
    {
        for (int index = 0; index < this.TimerCount; index++)
        {
            _ = this.gameplayTimer.WaitUntilAsync(this.dueTime(index));
        }
    }

    /// <summary>Populates the real TimerComponent before measuring its update.</summary>
    [IterationSetup(Target = nameof(TimerComponentUpdate))]
    public void SetupTimerComponentUpdate()
    {
        this.Setup();
        this.TimerComponentRegistration();
        setServerTime(UPDATE_SERVER_TIME);
    }

    /// <summary>Populates the real GameplayWorldTimer before measuring its update.</summary>
    [IterationSetup(Target = nameof(GameplayWorldTimerUpdate))]
    public void SetupGameplayTimerUpdate()
    {
        this.Setup();
        this.GameplayWorldTimerRegistration();
        setServerTime(UPDATE_SERVER_TIME);
    }

    /// <summary>Processes all due TimerComponent waits.</summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Update")]
    public void TimerComponentUpdate() => updateTimerComponent(this.timerComponent);

    /// <summary>Processes all due GameplayWorldTimer waits.</summary>
    [Benchmark]
    [BenchmarkCategory("Update")]
    public void GameplayWorldTimerUpdate() => updateGameplayTimer(this.gameplayTimer);

    private long dueTime(int index)
    {
        int firstTimestamp = index < (long)this.TimerCount * this.DuePercentage / PERCENTAGE_BASE
            ? FIRST_DUE_TIME
            : FIRST_FUTURE_DUE_TIME;
        return firstTimestamp + index % this.BucketCount;
    }

    private static void setServerTime(long serverTime)
    {
        TimeInfo.Instance.ServerMinusClientTime = serverTime - TimeInfo.Instance.FrameTime;
    }
}
