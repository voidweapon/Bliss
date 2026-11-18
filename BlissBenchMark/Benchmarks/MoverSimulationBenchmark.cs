using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Bliss.Timer;
using Unity.Mathematics;

namespace BlissBenchMark;

/// <summary>Measures real Root-driven Mover simulation for one fixed-step workload.</summary>
[MemoryDiagnoser]
public class MoverSimulationBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int MOVER_CONFIG_ID = 1;
    private const int EMPTY = 0;
    private const int SMALL = 100;
    private const int LARGE = 1000;
    private const long INITIAL_TIME = 1000;
    private const long TICK_MILLISECONDS = 50;
    private const long NO_SERVER_TIME_OFFSET = 0;
    private const float UNIT_AXIS = 1f;
    private const float ZERO_AXIS = 0f;
    private const float EXPECTED_DISTANCE = 0.5f;
    private const float DISTANCE_TOLERANCE = 0.1f;
    private static readonly ConstructorInfo rootConstructor =
        typeof(Root).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) }, null)
        ?? throw new InvalidOperationException("Root constructor was not found.");
    private static readonly MethodInfo registerSingleton =
        typeof(ASingleton).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private static readonly Action<Root> earlyUpdate =
        (typeof(Root).GetMethod("EarlyUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.EarlyUpdate was not found."))
        .CreateDelegate<Action<Root>>();
    private static readonly Action<Root> update =
        (typeof(Root).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.Update was not found."))
        .CreateDelegate<Action<Root>>();
    private static readonly FieldInfo? legacyPreviousTime =
        typeof(MoverComponent).GetField("PrevUpdateTime");

    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private ConfigManager configManager = null!;
    private MoverHandlerManager handlerManager = null!;
    private Root root = null!;
    private string previousDirectory = null!;
    private readonly List<MoverComponent> components = new();
    private readonly List<TransformComponent> transforms = new();

    /// <summary>Number of cargo entities, each with a straight-line Mover.</summary>
    [Params(EMPTY, SMALL, LARGE)]
    public int ParticipantCount { get; set; }

    /// <summary>Initializes production registries and validates that the measured tick moves cargo.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.previousDirectory = Directory.GetCurrentDirectory();
        string directory = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(directory, "ServerRes", "Configs")))
        {
            directory = Directory.GetParent(directory)?.FullName
                ?? throw new InvalidOperationException("ServerRes/Configs was not found.");
        }

        Directory.SetCurrentDirectory(Path.Combine(directory, "Server"));
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(MoverComponentSystem).Assembly,
            typeof(MoverSimulationBenchmark).Assembly,
        });
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);
        this.configManager = new ConfigManager();
        this.configManager.Init();
        registerSingleton.Invoke(this.configManager, null);
        this.handlerManager = new MoverHandlerManager();
        this.handlerManager.Init();
        registerSingleton.Invoke(this.handlerManager, null);

        this.Setup();
        this.Tick();
        foreach (TransformComponent transform in this.transforms)
        {
            if (Math.Abs(transform.Transform.Location.x - EXPECTED_DISTANCE) > DISTANCE_TOLERANCE)
            {
                throw new InvalidOperationException("The Root-driven Mover workload did not move its cargo.");
            }
        }
    }

    /// <summary>Builds and launches Mover hierarchies outside the measured tick.</summary>
    [IterationSetup]
    public void Setup()
    {
        this.root?.Dispose();
        this.components.Clear();
        this.transforms.Clear();
        setServerTime(INITIAL_TIME);
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(MoverSimulationBenchmark) });
        this.root.Scene.AddComponent<TimerComponent>();
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        for (int index = 0; index < this.ParticipantCount; index++)
        {
            BenchmarkCargo cargo = world.AddChild<BenchmarkCargo>();
            this.transforms.Add(cargo.AddComponent<TransformComponent>());
            MoverComponent component = cargo.AddComponent<MoverComponent>();
            component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo
            {
                TargetDir = new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS),
            });
            this.components.Add(component);
        }

        earlyUpdate(this.root);
        setServerTime(INITIAL_TIME + TICK_MILLISECONDS);
        foreach (MoverComponent component in this.components)
        {
            legacyPreviousTime?.SetValue(component, TimeInfo.Instance.ClientNow() - TICK_MILLISECONDS);
        }
    }

    /// <summary>Runs due Mover callbacks through the production Root driver.</summary>
    [Benchmark]
    public void Tick() => update(this.root);

    /// <summary>Releases benchmark-owned entities and singleton registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.root?.Dispose();
        this.handlerManager.Dispose();
        this.configManager.Dispose();
        this.eventSystem.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
        TimeInfo.Instance.ServerMinusClientTime = NO_SERVER_TIME_OFFSET;
        Directory.SetCurrentDirectory(this.previousDirectory);
    }

    private static void setServerTime(long time)
    {
        TimeInfo.Instance.ServerMinusClientTime = time - TimeInfo.Instance.FrameTime;
    }

    /// <summary>Cargo entity used by the production Mover workload.</summary>
    public sealed class BenchmarkCargo : Entity
    {
    }
}
