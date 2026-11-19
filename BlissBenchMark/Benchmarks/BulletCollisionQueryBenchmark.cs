using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Bliss.Physics;
using MagicPhysX;
using Unity.Mathematics;
using static MagicPhysX.NativeMethods;

namespace BlissBenchMark;

/// <summary>Measures production PhysX sphere overlap and sweep queries at representative scene sizes.</summary>
[MemoryDiagnoser]
public unsafe class BulletCollisionQueryBenchmark
{
    private const int ROOT_ID = 104;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 205;
    private const long WORLD_INSTANCE_ID = 10004;
    private const int SMALL = 10;
    private const int LARGE = 100;
    private const int ALL_LAYERS = -1;
    private const uint ALL_LAYER_FILTER = uint.MaxValue;
    private const uint ZERO_FILTER_WORD = 0;
    private const float COLLIDER_RADIUS = 0.5f;
    private const float QUERY_RADIUS = 0.5f;
    private const float MATERIAL_FRICTION = 0.5f;
    private const float MATERIAL_RESTITUTION = 0f;
    private const float ACTOR_SPACING = 2f;
    private const float QUERY_DISTANCE_MARGIN = 1f;
    private const float UNIT_AXIS = 1f;
    private const float ZERO = 0f;
    private const float IDENTITY_W = 1f;
    private static readonly ConstructorInfo rootConstructor =
        typeof(Root).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) }, null)
        ?? throw new InvalidOperationException("Root constructor was not found.");
    private static readonly MethodInfo registerSingleton =
        typeof(ASingleton).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private readonly List<nint> actors = new();
    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private Root root = null!;
    private PhysicalScene physicalScene = null!;

    /// <summary>Number of query-only static colliders in the physical scene.</summary>
    [Params(SMALL, LARGE)]
    public int ColliderCount { get; set; }

    /// <summary>Initializes production physical systems and verifies the measured queries.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(PhysicalSceneSystems).Assembly,
            typeof(BulletCollisionQueryBenchmark).Assembly,
        });
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);
        this.Setup();
        bool overlap = this.physicalScene.OverlapSpere(float3.zero, QUERY_RADIUS, ALL_LAYERS, out _);
        bool sweep = this.physicalScene.SpereCast(float3.zero, QUERY_RADIUS, new float3(UNIT_AXIS, ZERO, ZERO),
            this.ColliderCount * ACTOR_SPACING + QUERY_DISTANCE_MARGIN, ALL_LAYERS, out _);
        if (overlap || !sweep)
        {
            throw new InvalidOperationException("The production PhysX queries did not produce the expected hits.");
        }
    }

    /// <summary>Creates native query shapes outside the measured query invocation.</summary>
    [IterationSetup]
    public void Setup()
    {
        this.releaseScene();
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(BulletCollisionQueryBenchmark) });
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        this.physicalScene = world.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        PxMaterial* material = PxPhysics_createMaterial_mut(physicalWorld.Physics, MATERIAL_FRICTION,
            MATERIAL_FRICTION, MATERIAL_RESTITUTION);
        if (material == null)
        {
            throw new InvalidOperationException("Failed to create a PhysX material.");
        }

        try
        {
            for (int index = 0; index < this.ColliderCount; index++)
            {
                PxVec3 position = new PxVec3 { x = (index + 1) * ACTOR_SPACING, y = ZERO, z = ZERO };
                PxQuat rotation = new PxQuat { x = ZERO, y = ZERO, z = ZERO, w = IDENTITY_W };
                PxTransform transform = PxTransform_new_5(&position, &rotation);
                PxRigidStatic* actor = PxPhysics_createRigidStatic_mut(physicalWorld.Physics, &transform);
                if (actor == null)
                {
                    throw new InvalidOperationException("Failed to create a PhysX static actor.");
                }

                PxSphereGeometry geometry = PxSphereGeometry_new(COLLIDER_RADIUS);
                PxShape* shape = PxPhysics_createShape_mut(physicalWorld.Physics, (PxGeometry*)&geometry, material,
                    false, PxShapeFlags.SceneQueryShape);
                if (shape == null)
                {
                    PxRigidActor_release_mut((PxRigidActor*)actor);
                    throw new InvalidOperationException("Failed to attach a PhysX query shape.");
                }

                PxFilterData filterData = PxFilterData_new_2(ALL_LAYER_FILTER, ZERO_FILTER_WORD,
                    ZERO_FILTER_WORD, ZERO_FILTER_WORD);
                PxShape_setQueryFilterData_mut(shape, &filterData);
                bool attached = PxRigidActor_attachShape_mut((PxRigidActor*)actor, shape);
                PxShape_release_mut(shape);
                if (!attached)
                {
                    PxRigidActor_release_mut((PxRigidActor*)actor);
                    throw new InvalidOperationException("Failed to attach a PhysX query shape.");
                }

                if (!PxScene_addActor_mut(this.physicalScene.PxScene, (PxActor*)actor, null))
                {
                    PxRigidActor_release_mut((PxRigidActor*)actor);
                    throw new InvalidOperationException("Failed to add a PhysX query actor.");
                }

                this.actors.Add((nint)actor);
            }
        }
        finally
        {
            PxBase_release_mut((PxBase*)material);
        }
    }

    /// <summary>Measures one production overlap and one production sphere sweep.</summary>
    [Benchmark]
    public bool QueryPair()
    {
        bool overlap = this.physicalScene.OverlapSpere(float3.zero, QUERY_RADIUS, ALL_LAYERS, out _);
        bool sweep = this.physicalScene.SpereCast(float3.zero, QUERY_RADIUS, new float3(UNIT_AXIS, ZERO, ZERO),
            this.ColliderCount * ACTOR_SPACING + QUERY_DISTANCE_MARGIN, ALL_LAYERS, out _);
        return overlap || sweep;
    }

    /// <summary>Releases native actors, scenes and singleton registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.releaseScene();
        this.eventSystem.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
        PhysicalFunction.ReleaseFoundation();
    }

    private void releaseScene()
    {
        foreach (nint actor in this.actors)
        {
            PxScene_removeActor_mut(this.physicalScene.PxScene, (PxActor*)actor, false);
            PxRigidActor_release_mut((PxRigidActor*)actor);
        }

        this.actors.Clear();
        this.root?.Dispose();
        this.root = null!;
        this.physicalScene = null!;
    }
}
