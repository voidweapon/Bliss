using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;

namespace BlissBenchMark;

/// <summary>
/// Compares entity mounting in an ordinary scene with the direct registration path used by LevelScenes.
/// Scene creation and setup are excluded from the measurement.
/// </summary>
[MemoryDiagnoser]
public class EntityRegistrationBenchmark
{
    private const int ROOT_ID = 1;
    private const int ROOT_ZONE = 1;
    private const long GAMEPLAY_WORLD_ID = 2;
    private const long GAMEPLAY_WORLD_INSTANCE_ID = 3;
    private const long LEVEL_SCENE_ID = 4;
    private const long LEVEL_SCENE_INSTANCE_ID = 5;
    private const int SMALL_ENTITY_COUNT = 100;
    private const int LARGE_ENTITY_COUNT = 1000;
    private static readonly ConstructorInfo rootConstructor = typeof(Root).GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) },
        null) ?? throw new InvalidOperationException("Root constructor was not found.");
    private static readonly MethodInfo typeManagerRegisterMethod =
        typeof(TypeManager).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private static readonly MethodInfo entitySystemManagerRegisterMethod =
        typeof(EntitySystemManager).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");

    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private Root root = null!;
    private Entity ordinaryParent = null!;
    private BenchmarkLevelScene levelScene = null!;

    /// <summary>Number of entities mounted during each benchmark operation.</summary>
    [Params(SMALL_ENTITY_COUNT, LARGE_ENTITY_COUNT)]
    public int EntityCount { get; set; }

    /// <summary>Initializes the production entity and update-system registries.</summary>
    [GlobalSetup]
    public void InitializeEntitySystems()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(EntityRegistrationBenchmark).Assembly,
        });
        typeManagerRegisterMethod.Invoke(this.typeManager, null);

        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        entitySystemManagerRegisterMethod.Invoke(this.entitySystemManager, null);
    }

    /// <summary>Releases global registries after all measurements.</summary>
    [GlobalCleanup]
    public void DisposeEntitySystems()
    {
        this.root?.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
    }

    /// <summary>Creates a fresh ordinary scene outside the measured operation.</summary>
    [IterationSetup(Target = nameof(OrdinaryEntityRegistration))]
    public void SetupOrdinaryScene()
    {
        this.disposeRoot();
        this.root = this.createRoot();
        this.ordinaryParent = this.root.Scene;
    }

    /// <summary>Creates a fresh initializing LevelScene outside the measured operation.</summary>
    [IterationSetup(Target = nameof(LevelSceneEntityRegistration))]
    public void SetupLevelScene()
    {
        this.disposeRoot();
        this.root = this.createRoot();
        GameplayWorld gameplayWorld = this.createGameplayWorld();

        this.levelScene = this.createLevelScene();
        gameplayWorld.AddChild(this.levelScene);
    }

    /// <summary>Measures ordinary entity mounting and immediate update registration.</summary>
    [Benchmark(Baseline = true)]
    public void OrdinaryEntityRegistration()
    {
        for (int entityIndex = 0; entityIndex < this.EntityCount; entityIndex++)
        {
            this.ordinaryParent.AddChild<BenchmarkUpdateEntity>();
        }
    }

    /// <summary>Measures entity mounting and direct Root update registration under a LevelScene.</summary>
    [Benchmark]
    public void LevelSceneEntityRegistration()
    {
        for (int entityIndex = 0; entityIndex < this.EntityCount; entityIndex++)
        {
            this.levelScene.AddChild<BenchmarkUpdateEntity>();
        }
    }

    private void disposeRoot()
    {
        this.root?.Dispose();
        this.root = null!;
    }

    private GameplayWorld createGameplayWorld()
    {
        return GameplayWorldFactory.Create(
            this.root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(EntityRegistrationBenchmark));
    }

    private BenchmarkLevelScene createLevelScene()
    {
        return new BenchmarkLevelScene(
            this.root,
            LEVEL_SCENE_ID,
            LEVEL_SCENE_INSTANCE_ID,
            nameof(BenchmarkLevelScene));
    }

    private Root createRoot()
    {
        return (Root)rootConstructor.Invoke(new object[]
        {
            ROOT_ID,
            ROOT_ZONE,
            ELogicSceneType.Main,
            nameof(EntityRegistrationBenchmark),
        });
    }

    private sealed class BenchmarkLevelScene : LogicScene
    {
        public BenchmarkLevelScene(Root root, long id, long instanceId, string name)
            : base(root, id, instanceId, ELogicSceneType.Level, name)
        {
        }
    }

    /// <summary>Minimal update entity used by the registration comparison.</summary>
    public sealed class BenchmarkUpdateEntity : Entity, IUpdate
    {
    }

    /// <summary>Registers benchmark entities with the production update scheduler.</summary>
    [EntitySystem]
    public sealed class BenchmarkUpdateEntitySystem : UpdateSystem<BenchmarkUpdateEntity>
    {
        protected override void Update(BenchmarkUpdateEntity entity)
        {
        }
    }
}
