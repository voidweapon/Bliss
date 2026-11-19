using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Unity.Mathematics;

namespace BlissBenchMark;

/// <summary>Measures real gameplay entity creation and per-frame smoothing before and after ownership migration.</summary>
[MemoryDiagnoser]
public class GameplayOwnershipBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int SMALL_ENTITY_COUNT = 100;
    private const int LARGE_ENTITY_COUNT = 1000;
    private const int UNIT_CONFIG_ID = 1;
    private const long FIRST_UNIT_ID = 1;
    private const int NO_UPDATES = -1;
    private const float AOI_RADIUS = 10f;
    private const float AOI_ENTITY_SPACING = 64f;
    private const long ACTIVE_SMOOTHING_INTERVAL = 100000;
    private const long ACTIVE_SMOOTHING_ELAPSED = 10;
    private const float ACTIVE_SMOOTHING_OFFSET = 1f;
    private const long INITIAL_SMOOTHING_TIMESTAMP = 0;
    private const float ZERO_OFFSET = 0f;
    private const int EMPTY_ENTRY_COUNT = 0;
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
    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private Root root = null!;
    private GameplayWorld world = null!;
    private SmoothTransformComponent lastSmoothing = null!;
    private List<SmoothTransformComponent> smoothingParticipants = null!;

    /// <summary>Number of Units and SceneActors in each measured batch.</summary>
    [Params(SMALL_ENTITY_COUNT, LARGE_ENTITY_COUNT)]
    public int EntityCount { get; set; }

    /// <summary>Initializes production entity systems.</summary>
    [GlobalSetup(Targets = new[] { nameof(CreateParticipants), nameof(AoiLifecycle) })]
    public void Initialize()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(UnitHelper).Assembly,
            typeof(GameplayOwnershipBenchmark).Assembly,
        });
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);
    }

    /// <summary>Creates a fresh real Root and World outside creation measurements.</summary>
    [IterationSetup(Target = nameof(CreateParticipants))]
    public void SetupCreation() => this.createWorld();

    /// <summary>Creates a fresh World and AOI manager outside AOI lifecycle measurements.</summary>
    [IterationSetup(Target = nameof(AoiLifecycle))]
    public void SetupAoiLifecycle()
    {
        this.createWorld();
        this.world.AddComponent<AoiManagerComponent>();
    }

    /// <summary>Creates active participants outside update measurements and verifies the actual update path.</summary>
    [GlobalSetup(Targets = new[] { nameof(UpdateParticipants), nameof(UpdateActiveParticipants) })]
    public void SetupUpdate()
    {
        this.Initialize();
        this.createWorld();
        this.smoothingParticipants = new List<SmoothTransformComponent>();
        this.CreateParticipants();
        earlyUpdate(this.root);
        this.lastSmoothing.ViewTimeStamp = NO_UPDATES;
        update(this.root);
        if (this.lastSmoothing.ViewTimeStamp == NO_UPDATES)
        {
            throw new InvalidOperationException("The Root did not update the smoothing participant.");
        }
    }

    /// <summary>Creates production Units and SceneActors with dedicated components.</summary>
    [Benchmark]
    public void CreateParticipants()
    {
        for (int index = 0; index < this.EntityCount; index++)
        {
            Unit unit = UnitHelper.CreateMinimalUnit(this.world, UNIT_CONFIG_ID, index + FIRST_UNIT_ID,
                ENetRole.Authority, ENetRole.SimulatedProxy, float3.zero, quaternion.identity);
            unit.AddComponent<AOIEntityCmp>();
            this.lastSmoothing = unit.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>(
                new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
            this.smoothingParticipants?.Add(this.lastSmoothing);
            SceneActor actor = this.world.AddChild<SceneActor>();
            actor.AddComponent<AOIEntityCmp>();
        }
    }

    /// <summary>Runs normal Root updates over the prepared smoothing participants.</summary>
    [Benchmark]
    public void UpdateParticipants() => update(this.root);

    /// <summary>Prepares active interpolation outside the measured World update.</summary>
    [IterationSetup(Target = nameof(UpdateActiveParticipants))]
    public void SetupActiveSmoothing()
    {
        foreach (SmoothTransformComponent smoothing in this.smoothingParticipants)
        {
            smoothing.IsSmoothingComplete = false;
            smoothing.ViewTimeStamp = TimeInfo.Instance.ClientNow() - ACTIVE_SMOOTHING_ELAPSED;
            smoothing.SyncData.ViewSmoothTimeStamp = INITIAL_SMOOTHING_TIMESTAMP;
            smoothing.SyncData.LogicTimeStamp = ACTIVE_SMOOTHING_INTERVAL;
            smoothing.SyncData.LastCorrectionDelta = ACTIVE_SMOOTHING_INTERVAL;
            smoothing.SyncData.OriginalMeshTranslationOffset = new float3(ACTIVE_SMOOTHING_OFFSET, ZERO_OFFSET, ZERO_OFFSET);
            smoothing.SyncData.OriginalMeshRotationOffset = quaternion.identity;
            smoothing.SyncData.MeshRotationOffset = quaternion.identity;
            smoothing.SyncData.MeshRotationTarget = quaternion.identity;
        }
    }

    /// <summary>Runs normal Root updates over participants performing active visual interpolation.</summary>
    [Benchmark]
    public void UpdateActiveParticipants() => update(this.root);

    /// <summary>Verifies that the measured update advanced interpolation.</summary>
    [IterationCleanup(Target = nameof(UpdateActiveParticipants))]
    public void VerifyActiveSmoothing()
    {
        if (this.lastSmoothing.SyncData.ViewSmoothTimeStamp <= INITIAL_SMOOTHING_TIMESTAMP ||
            this.lastSmoothing.IsSmoothingComplete)
        {
            throw new InvalidOperationException("The Root did not advance active smoothing.");
        }
    }

    /// <summary>Measures real AOI registration and disposal for minimally created Units.</summary>
    [Benchmark]
    public void AoiLifecycle()
    {
        AoiManagerComponent manager = this.world.GetComponent<AoiManagerComponent>();
        for (int index = 0; index < this.EntityCount; index++)
        {
            float3 position = float3.zero;
            position.x = index * AOI_ENTITY_SPACING;
            Unit unit = UnitHelper.CreateMinimalUnit(this.world, UNIT_CONFIG_ID, index + FIRST_UNIT_ID,
                ENetRole.Authority, ENetRole.SimulatedProxy, position, quaternion.identity);
            AOIEntityCmp aoi = unit.AddComponent<AOIEntityCmp>();
            aoi.Add(AOIType.Visibility, manager.Register(
                AoiRegisterInfo.CreateSphere(unit, AOIType.Visibility, position, AOI_RADIUS)));
            aoi.Add(AOIType.NetworkVisibility, manager.Register(
                AoiRegisterInfo.CreateSphere(unit, AOIType.NetworkVisibility, position, AOI_RADIUS)));
            unit.Dispose();
        }

        if (manager.Entries.Count != EMPTY_ENTRY_COUNT)
        {
            throw new InvalidOperationException("The AOI entries were not released.");
        }
    }

    /// <summary>Releases per-iteration gameplay entities.</summary>
    [IterationCleanup(Targets = new[] { nameof(CreateParticipants), nameof(AoiLifecycle) })]
    public void CleanupCreation() => this.releaseWorld();

    /// <summary>Releases Roots and production entity registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.releaseWorld();
        this.eventSystem.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
    }

    private void createWorld()
    {
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(GameplayOwnershipBenchmark) });
        this.world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
    }

    private void releaseWorld()
    {
        this.root?.Dispose();
        this.root = null!;
    }
}
