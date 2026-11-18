using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Bliss.Core.EventSystem;
using ET;

namespace BlissBenchMark;

/// <summary>Measures real RootManager and child Root dispatch with active GameplayWorlds.</summary>
[MemoryDiagnoser]
public class RootSchedulingBenchmark
{
    private const int SMALL_ROOT_COUNT = 1;
    private const int MEDIUM_ROOT_COUNT = 10;
    private const int LARGE_ROOT_COUNT = 100;
    private const int ROOT_ID_BASE = 1000;
    private const int CHILD_ROOT_ID = 2000;
    private const int ROOT_ZONE = 1;
    private const int SUPPRESS_INFO_LOG_LEVEL = 4;
    private const int NO_UPDATES = 0;
    private const long WORLD_ID = 1;
    private const long WORLD_INSTANCE_ID = 2;
    private readonly List<RootSchedulingParticipant> participants = new();
    private int previousLogLevel;

    /// <summary>Number of top-level Roots, each with a child Root and two active Worlds.</summary>
    [Params(SMALL_ROOT_COUNT, MEDIUM_ROOT_COUNT, LARGE_ROOT_COUNT)]
    public int RootCount { get; set; }

    /// <summary>Initializes production schedulers and verifies that every World is updated.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.previousLogLevel = Log.LogLevel;
        Log.LogLevel = SUPPRESS_INFO_LOG_LEVEL;
        TypeManager typeManager = new TypeManager();
        typeManager.Init(new List<System.Reflection.Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(RootSchedulingBenchmark).Assembly,
        });
        World.Instance.AddSingleton(typeManager);
        World.Instance.AddSingleton<EntitySystemManager>();
        World.Instance.AddSingleton<EventSystem>();
        RootManager rootManager = World.Instance.AddSingleton<RootManager>();
        for (int index = 0; index < this.RootCount; index++)
        {
            rootManager.Create(ROOT_ID_BASE + index, ROOT_ZONE, ELogicSceneType.None, nameof(RootSchedulingBenchmark))
                .GetAwaiter().GetResult();
            Root root = RootSchedulingInit.LastCreatedRoot
                ?? throw new InvalidOperationException("The Root initializer did not receive a Root.");
            Root child = root.CreateChild(CHILD_ROOT_ID, ELogicSceneType.None, nameof(RootSchedulingBenchmark));
            this.addWorld(root);
            this.addWorld(child);
        }

        World.Instance.EarlyUpdate();
        World.Instance.Update();
        World.Instance.LateUpdate();
        World.Instance.EarlyUpdate();
        World.Instance.Update();
        foreach (RootSchedulingParticipant participant in this.participants)
        {
            if (participant.UpdateCount == NO_UPDATES)
            {
                throw new InvalidOperationException("The benchmark did not update every GameplayWorld.");
            }
        }
    }

    /// <summary>Runs a normal production RootManager update over top-level and child Roots.</summary>
    [Benchmark]
    public void Update() => World.Instance.Update();

    /// <summary>Releases Roots, Worlds and production registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        World.Instance.Dispose();
        Log.LogLevel = this.previousLogLevel;
    }

    private void addWorld(Root root)
    {
        GameplayWorld world = GameplayWorldFactory.Create(
            root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(RootSchedulingBenchmark));
        this.participants.Add(world.AddChild<RootSchedulingParticipant>());
    }
}

/// <summary>Initializes benchmark Roots without opening network endpoints.</summary>
[Invoke((long)ELogicSceneType.None)]
public sealed class RootSchedulingInit : AInvokeHandler<Invoke_RootInit, ETTask>
{
    /// <summary>Most recently initialized benchmark Root.</summary>
    public static Root? LastCreatedRoot { get; private set; }

    /// <inheritdoc />
    public override async ETTask Handle(Invoke_RootInit args)
    {
        LastCreatedRoot = args.Root;
        await ETTask.CompletedTask;
    }
}

/// <summary>Active GameplayWorld participant used to validate benchmark work.</summary>
public sealed class RootSchedulingParticipant : GamePlayEntity, IUpdate
{
    /// <summary>Number of normal gameplay updates observed.</summary>
    public int UpdateCount;
}

/// <summary>Updates the benchmark participant through the production entity scheduler.</summary>
[EntitySystem]
public sealed class RootSchedulingParticipantSystem : UpdateSystem<RootSchedulingParticipant>
{
    /// <inheritdoc />
    protected override void Update(RootSchedulingParticipant entity) => entity.UpdateCount++;
}
