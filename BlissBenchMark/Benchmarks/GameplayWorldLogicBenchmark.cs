using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;

namespace BlissBenchMark;

/// <summary>Measures the actual Root-driven fixed-step dispatch path.</summary>
[MemoryDiagnoser]
public class GameplayWorldLogicBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const long INITIAL_TIME = 1000;
    private const int EMPTY = 0;
    private const int SMALL = 100;
    private const int LARGE = 1000;
    private const int PRIORITY_BUCKETS = 4;
    private const int SECOND_TICK = 2;
    private const int TICKS_PER_OPERATION = 1000;
    private const int CHURN_CYCLES = 100;
    private static readonly ConstructorInfo rootConstructor =
        typeof(Root).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) },
            null) ?? throw new InvalidOperationException("Root constructor was not found.");
    private static readonly Action<Root> earlyUpdate =
        (typeof(Root).GetMethod("EarlyUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.EarlyUpdate was not found."))
        .CreateDelegate<Action<Root>>();
    private static readonly Action<Root> update =
        (typeof(Root).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.Update was not found."))
        .CreateDelegate<Action<Root>>();
    private static readonly MethodInfo registerSingleton =
        typeof(TypeManager).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private static readonly MethodInfo registerEntitySystems =
        typeof(EntitySystemManager).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("EntitySystemManager registration method was not found.");

    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private Root root = null!;
    private GameplayWorld world = null!;
    private readonly List<BenchmarkParticipant> participants = new List<BenchmarkParticipant>();
    private int callbackCount;
    private long nextTickTime;

    /// <summary>Number of registered participants.</summary>
    [Params(EMPTY, SMALL, LARGE)]
    public int ParticipantCount { get; set; }

    /// <summary>Initializes the production registries and verifies the workload.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(GameplayWorldLogicBenchmark).Assembly,
        });
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerEntitySystems.Invoke(this.entitySystemManager, null);
        this.Setup();
        this.Dispatch();
        if (this.callbackCount != this.ParticipantCount * TICKS_PER_OPERATION)
        {
            throw new InvalidOperationException("The Root-driven benchmark did not dispatch every participant.");
        }

        this.SetupRegistration();
        this.RegisterEntries();
        earlyUpdate(this.root);
        TimeInfo.Instance.ServerMinusClientTime =
            INITIAL_TIME + GameplayWorldLogicUpdate.STEP_MILLISECONDS - TimeInfo.Instance.FrameTime;
        update(this.root);
        if (this.callbackCount != this.ParticipantCount)
        {
            throw new InvalidOperationException("Registration workload did not register every participant.");
        }

        this.UnregisterEntries();
        TimeInfo.Instance.ServerMinusClientTime =
            INITIAL_TIME + GameplayWorldLogicUpdate.STEP_MILLISECONDS * SECOND_TICK - TimeInfo.Instance.FrameTime;
        update(this.root);
        if (this.callbackCount != this.ParticipantCount)
        {
            throw new InvalidOperationException("Removal workload left a participant registered.");
        }

        this.SetupChurn();
        this.ChurnEntries();
        earlyUpdate(this.root);
        TimeInfo.Instance.ServerMinusClientTime =
            INITIAL_TIME + GameplayWorldLogicUpdate.STEP_MILLISECONDS - TimeInfo.Instance.FrameTime;
        update(this.root);
        if (this.callbackCount != this.ParticipantCount)
        {
            throw new InvalidOperationException("Churn workload did not leave all participants registered.");
        }
    }

    /// <summary>Creates and registers participants outside the measured operation.</summary>
    [IterationSetup(Target = nameof(Dispatch))]
    public void Setup()
    {
        this.prepare(true);
        earlyUpdate(this.root);
        TimeInfo.Instance.ServerMinusClientTime =
            INITIAL_TIME + GameplayWorldLogicUpdate.STEP_MILLISECONDS - TimeInfo.Instance.FrameTime;
        update(this.root);
        if (this.callbackCount != this.ParticipantCount)
        {
            throw new InvalidOperationException("The Root-driven warm-up did not dispatch every participant.");
        }

        this.callbackCount = 0;
        this.nextTickTime = INITIAL_TIME + GameplayWorldLogicUpdate.STEP_MILLISECONDS * SECOND_TICK;
    }

    /// <summary>Creates participants without fixed-step entries before registration measurement.</summary>
    [IterationSetup(Target = nameof(RegisterEntries))]
    public void SetupRegistration() => this.prepare(false);

    /// <summary>Creates participants with fixed-step entries before removal measurement.</summary>
    [IterationSetup(Target = nameof(UnregisterEntries))]
    public void SetupRemoval() => this.prepare(true);

    /// <summary>Warms a repeated registration workload outside measurement.</summary>
    [IterationSetup(Target = nameof(ChurnEntries))]
    public void SetupChurn()
    {
        this.prepare(true);
        this.UnregisterEntries();
        this.RegisterEntries();
    }

    private void prepare(bool registerEntries)
    {
        this.root?.Dispose();
        this.participants.Clear();
        TimeInfo.Instance.ServerMinusClientTime = INITIAL_TIME - TimeInfo.Instance.FrameTime;
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(GameplayWorldLogicBenchmark) });
        this.world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        this.callbackCount = 0;
        for (int index = 0; index < this.ParticipantCount; index++)
        {
            this.participants.Add(this.world.AddChild<BenchmarkParticipant>());
        }

        if (registerEntries)
        {
            this.RegisterEntries();
        }
    }

    /// <summary>Processes repeated Timer and fixed ticks through Root.Update.</summary>
    [Benchmark(OperationsPerInvoke = TICKS_PER_OPERATION)]
    public void Dispatch()
    {
        for (int tick = 0; tick < TICKS_PER_OPERATION; tick++)
        {
            TimeInfo.Instance.ServerMinusClientTime = this.nextTickTime - TimeInfo.Instance.FrameTime;
            update(this.root);
            this.nextTickTime += GameplayWorldLogicUpdate.STEP_MILLISECONDS;
        }
    }

    /// <summary>Registers all participants with four interleaved priorities.</summary>
    [Benchmark]
    public void RegisterEntries()
    {
        for (int index = 0; index < this.participants.Count; index++)
        {
            this.world.LogicUpdate.Register(
                this.participants[index], nameof(Dispatch), index % PRIORITY_BUCKETS, this.countCallback);
        }
    }

    /// <summary>Removes all registered entries in participant order.</summary>
    [Benchmark]
    public void UnregisterEntries()
    {
        foreach (BenchmarkParticipant participant in this.participants)
        {
            if (!this.world.LogicUpdate.Unregister(participant, nameof(Dispatch)))
            {
                throw new InvalidOperationException("A benchmark entry was not registered.");
            }
        }
    }

    /// <summary>Measures repeated removal and registration of the same participants.</summary>
    [Benchmark(OperationsPerInvoke = CHURN_CYCLES)]
    public void ChurnEntries()
    {
        for (int cycle = 0; cycle < CHURN_CYCLES; cycle++)
        {
            this.UnregisterEntries();
            this.RegisterEntries();
        }
    }

    /// <summary>Releases benchmark-owned state and global registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.root?.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
        TimeInfo.Instance.ServerMinusClientTime = 0;
    }

    private void countCallback(float delta)
    {
        if (delta != GameplayWorldLogicUpdate.STEP_SECONDS)
        {
            throw new InvalidOperationException("Incorrect fixed delta.");
        }

        this.callbackCount++;
    }

    /// <summary>Participant used by the fixed-step workload.</summary>
    public sealed class BenchmarkParticipant : Entity
    {
    }
}
