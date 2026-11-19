using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Physics;
using ET;

namespace BlissBenchMark;

/// <summary>Measures real, resource-free gameplay subtree migration between prepared PhysX LevelScenes.</summary>
[MemoryDiagnoser]
public class GameplaySubtreeMigrationBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int SOURCE_LEVEL_ID = 303;
    private const int DESTINATION_LEVEL_ID = 304;
    private const int SMALL_SUBTREE_COUNT = 100;
    private const int LARGE_SUBTREE_COUNT = 1000;
    private static readonly ConstructorInfo rootConstructor = typeof(Root).GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic, null,
        new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) }, null)
        ?? throw new InvalidOperationException("Root constructor was not found.");
    private static readonly MethodInfo registerSingleton = typeof(ASingleton).GetMethod(
        "Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private Root root = null!;
    private Entity subtree = null!;
    private LogicScene destination = null!;

    /// <summary>Number of children in the migrated entity subtree.</summary>
    [Params(SMALL_SUBTREE_COUNT, LARGE_SUBTREE_COUNT)]
    public int EntityCount { get; set; }

    /// <summary>Initializes production registries and replaces only external Level resource loading.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(GameplaySubtreeMigration).Assembly,
            typeof(GameplaySubtreeMigrationBenchmark).Assembly,
        });
        this.typeManager.GetTypes(typeof(EventAttribute)).Remove(typeof(EventHandler_AfterCreateLevelScene_Server));
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);
    }

    /// <summary>Creates fresh source, destination and subtree outside the measured operation.</summary>
    [IterationSetup]
    public void Prepare()
    {
        this.root?.Dispose();
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(GameplaySubtreeMigrationBenchmark) });
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplaySubtreeMigrationBenchmark));
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        LogicScene source = LevelSceneFactory.Create(
            world, world, SOURCE_LEVEL_ID, SOURCE_LEVEL_ID, nameof(SOURCE_LEVEL_ID)).GetAwaiter().GetResult();
        this.destination = LevelSceneFactory.Create(
            world, world, DESTINATION_LEVEL_ID, DESTINATION_LEVEL_ID, nameof(DESTINATION_LEVEL_ID))
            .GetAwaiter().GetResult();
        source.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        this.destination.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        this.subtree = source.AddChild<BenchmarkCargo>();
        for (int index = 0; index < this.EntityCount; index++)
        {
            this.subtree.AddChild<BenchmarkCargo>();
        }
    }

    /// <summary>Runs the same public migration entry used by gameplay callers.</summary>
    [Benchmark]
    public void MigrateSubtree()
    {
        GameplaySubtreeMigration.MigrateAsync(this.subtree, this.destination).GetAwaiter().GetResult();
        if (!ReferenceEquals(this.subtree.Parent, this.destination))
        {
            throw new InvalidOperationException("The benchmark subtree was not migrated.");
        }
    }

    /// <summary>Releases the Root and global registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.root?.Dispose();
        this.eventSystem.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
    }

    /// <summary>Minimal entity in the measured subtree.</summary>
    public sealed class BenchmarkCargo : Entity
    {
    }

    /// <summary>Prepares a LevelScene without loading unrelated external map assets.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class PrepareBenchmarkLevel : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        /// <inheritdoc />
        protected override async ETTask Run(LogicScene scene, Event_AfterCreateLevelScene data) =>
            await ETTask.CompletedTask;
    }
}
