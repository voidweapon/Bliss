using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Timer;
using Unity.Mathematics;

namespace BlissBenchMark;

/// <summary>Measures the actual Root-driven Kinematic manager and platform simulation.</summary>
[MemoryDiagnoser]
public class KinematicSimulationBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int EMPTY = 0;
    private const int SMALL = 100;
    private const int LARGE = 1000;
    private const long INITIAL_TIME = 1000;
    private const long TICK_MILLISECONDS = 50;
    private const long NO_SERVER_TIME_OFFSET = 0;
    private const float EXPECTED_DELTA = 0.05f;
    private const float UNIT_AXIS = 1f;
    private const float ZERO_AXIS = 0f;
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
        typeof(KinematicMoverManager).GetField("PreviousTimeStamp");

    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private Root root = null!;
    private KinematicMoverManager manager = null!;
    private KinematicRigidbodyMover? lastMover;
    private readonly List<TransformComponent> transforms = new();
    private readonly List<BenchmarkController> controllers = new();

    /// <summary>Number of Kinematic platforms simulated by the manager.</summary>
    [Params(EMPTY, SMALL, LARGE)]
    public int ParticipantCount { get; set; }

    /// <summary>Initializes production registries and checks the measured platform work.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(KinematicMoverManagerSystems).Assembly,
            typeof(KinematicSimulationBenchmark).Assembly,
        });
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);

        this.Setup();
        this.Tick();
        foreach (BenchmarkController controller in this.controllers)
        {
            if (controller.LastDelta != EXPECTED_DELTA)
            {
                throw new InvalidOperationException("The Root-driven manager did not simulate a fixed step.");
            }
        }

        foreach (TransformComponent transform in this.transforms)
        {
            if (transform.Transform.Location.x != UNIT_AXIS)
            {
                throw new InvalidOperationException("The Root-driven manager did not move every platform.");
            }
        }

        this.Setup();
        this.TickWithRemoval();
        int expectedCount = this.ParticipantCount == EMPTY ? EMPTY : this.ParticipantCount - 1;
        if (this.manager.PhysicsMovers.Count != expectedCount)
        {
            throw new InvalidOperationException("The callback did not unregister the expected mover.");
        }
    }

    /// <summary>Creates a manager and Kinematic platforms outside the measured tick.</summary>
    [IterationSetup]
    public void Setup()
    {
        this.root?.Dispose();
        this.transforms.Clear();
        this.controllers.Clear();
        this.lastMover = null;
        setServerTime(INITIAL_TIME);
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(KinematicSimulationBenchmark) });
        this.root.Scene.AddComponent<TimerComponent>();
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        this.manager = world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
            new KinematicMoverManagerSettings { AutoSimulation = true });
        this.manager.Enable = true;
        for (int index = 0; index < this.ParticipantCount; index++)
        {
            BenchmarkPlatform platform = world.AddChild<BenchmarkPlatform>();
            this.transforms.Add(platform.AddComponent<TransformComponent>());
            platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
            KinematicRigidbodyMover mover = platform.AddComponent<KinematicRigidbodyMover>();
            this.lastMover = mover;
            BenchmarkController controller = new BenchmarkController();
            mover.MoverController = controller;
            this.controllers.Add(controller);
        }

        earlyUpdate(this.root);
        setServerTime(INITIAL_TIME + TICK_MILLISECONDS);
        legacyPreviousTime?.SetValue(this.manager, TimeInfo.Instance.ClientFrameTime() - TICK_MILLISECONDS);
    }

    /// <summary>Runs a due manager simulation through the production Root driver.</summary>
    [Benchmark]
    public void Tick() => update(this.root);

    /// <summary>Runs a tick that unregisters a live mover from the first participant callback.</summary>
    [Benchmark]
    public void TickWithRemoval()
    {
        if (this.ParticipantCount != EMPTY)
        {
            this.controllers[0].Manager = this.manager;
            this.controllers[0].MoverToRemove = this.lastMover;
        }
        update(this.root);
    }

    /// <summary>Releases benchmark-owned entities and singleton registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.root?.Dispose();
        this.eventSystem.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
        TimeInfo.Instance.ServerMinusClientTime = NO_SERVER_TIME_OFFSET;
    }

    private static void setServerTime(long time) =>
        TimeInfo.Instance.ServerMinusClientTime = time - TimeInfo.Instance.FrameTime;

    /// <summary>A platform entity for the Kinematic workload.</summary>
    public sealed class BenchmarkPlatform : Entity
    {
    }

    /// <summary>Provides a deterministic goal for a production Kinematic mover.</summary>
    public sealed class BenchmarkController : IMoverController
    {
        /// <summary>The delta observed by the most recent simulation.</summary>
        public float LastDelta { get; private set; }
        /// <summary>Manager used to unregister the selected participant.</summary>
        public KinematicMoverManager? Manager;
        /// <summary>Live participant to unregister during the next callback.</summary>
        public KinematicRigidbodyMover? MoverToRemove;

        /// <inheritdoc />
        public void UpdateMovement(out float3 goalPosition, out quaternion goalRotation, float deltaTime)
        {
            this.LastDelta = deltaTime;
            if (this.MoverToRemove != null)
            {
                this.Manager!.UnregisterPhysicsMover(this.MoverToRemove);
                this.MoverToRemove = null;
            }
            goalPosition = new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS);
            goalRotation = quaternion.identity;
        }
    }
}
