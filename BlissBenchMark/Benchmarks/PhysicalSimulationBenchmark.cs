using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Bliss.Physics;
using Bliss.Timer;
using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace BlissBenchMark;

/// <summary>Measures real Root-driven server PhysX simulation with dynamic actors.</summary>
[MemoryDiagnoser]
public unsafe class PhysicalSimulationBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int EMPTY = 0;
    private const int SMALL = 10;
    private const int LARGE = 100;
    private const long INITIAL_TIME = 1000;
    private const long FIRST_TICK_TIME = 1050;
    private const long NO_SERVER_TIME_OFFSET = 0;
    private const float INITIAL_HEIGHT = 10f;
    private const float ACTOR_SPACING = 2f;
    private const float ZERO = 0f;
    private const float IDENTITY_W = 1f;
    private const float ACTOR_MASS = 1f;
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
    private readonly List<nint> actors = new();
    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private Root root = null!;
    private PhysicalScene physicalScene = null!;

    /// <summary>Number of independent dynamic actors in the PhysX scene.</summary>
    [Params(EMPTY, SMALL, LARGE)]
    public int ActorCount { get; set; }

    /// <summary>Initializes production systems and verifies that a tick advances actual native actors.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(PhysicalSceneSystems).Assembly,
            typeof(PhysicalSimulationBenchmark).Assembly,
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
        foreach (nint actor in this.actors)
        {
            if (PxRigidActor_getGlobalPose((PxRigidActor*)actor).p.y >= INITIAL_HEIGHT)
            {
                throw new InvalidOperationException("The Root-driven physical tick did not advance an actor.");
            }
        }
    }

    /// <summary>Creates native physical resources and actors outside the measured tick.</summary>
    [IterationSetup]
    public void Setup()
    {
        this.releaseScene();
        setServerTime(INITIAL_TIME);
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(PhysicalSimulationBenchmark) });
        this.root.Scene.AddComponent<TimerComponent>();
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        this.physicalScene = world.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        for (int index = 0; index < this.ActorCount; index++)
        {
            PxVec3 position = new PxVec3 { x = index * ACTOR_SPACING, y = INITIAL_HEIGHT, z = ZERO };
            PxQuat rotation = new PxQuat { x = ZERO, y = ZERO, z = ZERO, w = IDENTITY_W };
            PxTransform transform = PxTransform_new_5(&position, &rotation);
            PxRigidDynamic* actor = PxPhysics_createRigidDynamic_mut(physicalWorld.Physics, &transform);
            if (actor == null)
            {
                throw new InvalidOperationException("Failed to create a PhysX dynamic actor.");
            }

            ((PxRigidBody*)actor)->SetMassMut(ACTOR_MASS);
            PxScene_addActor_mut(this.physicalScene.PxScene, (PxActor*)actor, null);
            this.actors.Add((nint)actor);
        }

        this.physicalScene.StartUpdates();
        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
    }

    /// <summary>Runs a due physical simulation through the production Root driver.</summary>
    [Benchmark]
    public void Tick() => update(this.root);

    /// <summary>Releases native actors, scenes and singleton registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.releaseScene();
        this.eventSystem.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
        PhysicalFunction.ReleaseFoundation();
        TimeInfo.Instance.ServerMinusClientTime = NO_SERVER_TIME_OFFSET;
    }

    private void releaseScene()
    {
        foreach (nint actor in this.actors)
        {
            PxRigidActor_release_mut((PxRigidActor*)actor);
        }

        this.actors.Clear();
        this.root?.Dispose();
    }

    private static void setServerTime(long time) =>
        TimeInfo.Instance.ServerMinusClientTime = time - TimeInfo.Instance.FrameTime;
}
