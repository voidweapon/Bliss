using System.Reflection;
using Bliss.Core;

namespace Bliss.Tests;

[NonParallelizable]
public class GameplayWorldLifecycleTests
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long GAMEPLAY_WORLD_ID = 202;
    private const long GAMEPLAY_WORLD_INSTANCE_ID = 10001;
    private const int LEVEL_SCENE_ID = 303;
    private const long LEVEL_SCENE_INSTANCE_ID = 10002;
    private const int FIRST_TEST_INSTANCE_ID = 20000;
    private const long INVALID_INSTANCE_ID = 0;

    private TypeManager _typeManager;
    private EntitySystemManager _entitySystemManager;
    private Root _root;
    private int _nextInstanceId;

    public static List<string> ExecutionLog { get; } = new List<string>();

    [SetUp]
    public void SetUp()
    {
        ExecutionLog.Clear();
        _nextInstanceId = FIRST_TEST_INSTANCE_ID;

        _typeManager = new TypeManager();
        _typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorldLifecycleTests).Assembly,
        });
        _typeManager.Register();

        _entitySystemManager = new EntitySystemManager();
        _entitySystemManager.Init();
        _entitySystemManager.Register();
        _root = new Root(ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(GameplayWorldLifecycleTests));
    }

    [TearDown]
    public void TearDown()
    {
        _root?.Dispose();
        _entitySystemManager.Dispose();
        _typeManager.Dispose();
    }

    [Test]
    public void FactoryCreatesGameplayWorldWithRequestedIdentityAndParent()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));

        Assert.That(gameplayWorld, Is.TypeOf<GameplayWorld>());
        Assert.That(gameplayWorld.Parent, Is.SameAs(_root.Scene));
        Assert.That(gameplayWorld.Id, Is.EqualTo(GAMEPLAY_WORLD_ID));
        Assert.That(gameplayWorld.InstanceId, Is.EqualTo(GAMEPLAY_WORLD_INSTANCE_ID));
        Assert.That(gameplayWorld.SceneType, Is.EqualTo(ELogicSceneType.GameplayWorld));
        Assert.That(gameplayWorld.Name, Is.EqualTo(nameof(GameplayWorld)));
    }

    [Test]
    public void RootPhasesForwardThroughLevelComponentToGameplayEntity()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var levelScene = LogicSceneFactoryCore.CreateLogicScene(
            _root.Scene,
            LEVEL_SCENE_ID,
            LEVEL_SCENE_INSTANCE_ID,
            ELogicSceneType.Level,
            nameof(LEVEL_SCENE_ID));
        var levelGameWorldComponent = levelScene.AddComponent<LevelGameWorldComponent>();
        levelGameWorldComponent.GameplayWorld = gameplayWorld;
        var entity = gameplayWorld.AddChild<UpdateGameplayEntity>();

        _root.EarlyUpdate();
        _root.Update();
        _root.LateUpdate();

        CollectionAssert.AreEqual(
            new[]
            {
                $"gameplayEarly:{entity.InstanceId}",
                $"gameplayUpdate:{entity.InstanceId}",
                $"gameplayLate:{entity.InstanceId}",
            },
            ExecutionLog);
    }

    [Test]
    public void GameplayEntityRegistrationOutsideGameplayWorldThrowsWithTypeDetails()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => _root.Scene.AddChild<UpdateGameplayEntity>());

        Assert.That(exception.Message, Does.Contain(typeof(UpdateGameplayEntity).FullName));
        Assert.That(exception.Message, Does.Contain(typeof(LogicScene).FullName));
    }

    [Test]
    public void OrdinaryEntityStillRunsThroughTheRootEntitySystem()
    {
        var entity = _root.Scene.AddChild<OrdinaryUpdateEntity>();

        _root.EarlyUpdate();
        _root.Update();

        CollectionAssert.AreEqual(new[] { $"rootUpdate:{entity.InstanceId}" }, ExecutionLog);
    }

    public class UpdateGameplayEntity : GamePlayEntity, IEarlyUpdate, IUpdate, ILateUpdate
    {
        public void SetInstanceId(long instanceId)
        {
            InstanceId = instanceId;
        }

        public void Invalidate()
        {
            InstanceId = INVALID_INSTANCE_ID;
        }
    }

    public class OrdinaryUpdateEntity : Entity, IUpdate
    {
    }

    [EntitySystem]
    public class EarlyGameplayEntitySystem : EarlyUpdateSystem<UpdateGameplayEntity>
    {
        protected override void EarlyUpdate(UpdateGameplayEntity entity)
        {
            ExecutionLog.Add($"gameplayEarly:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class UpdateGameplayEntitySystem : UpdateSystem<UpdateGameplayEntity>
    {
        protected override void Update(UpdateGameplayEntity entity)
        {
            ExecutionLog.Add($"gameplayUpdate:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class LateGameplayEntitySystem : LateUpdateSystem<UpdateGameplayEntity>
    {
        protected override void LateUpdate(UpdateGameplayEntity entity)
        {
            ExecutionLog.Add($"gameplayLate:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class OrdinaryUpdateEntitySystem : UpdateSystem<OrdinaryUpdateEntity>
    {
        protected override void Update(OrdinaryUpdateEntity entity)
        {
            ExecutionLog.Add($"rootUpdate:{entity.InstanceId}");
        }
    }
}
