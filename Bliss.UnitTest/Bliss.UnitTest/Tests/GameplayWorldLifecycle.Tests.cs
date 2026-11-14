using System.Reflection;
using Bliss.Core;
using ET;

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
    private const int NESTED_LEVEL_CONFIG_ID = 404;
    private const int ROOT_LEVEL_CONFIG_ID = 405;
    private const long NESTED_LEVEL_ID = 406;
    private const long NESTED_CLIENT_SCENE_ID = 407;

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
        TestLevelInitializationHandler.Fail = false;
        TestLevelInitializationHandler.CreateNested = false;
        TestLevelInitializationHandler.NestedWasCommittedDuringParentInitialization = false;
        TestLevelInitializationHandler.FailCommit = false;
        TestLevelInitializationHandler.AppliedEffectCount = 0;
        TestLevelInitializationHandler.ParentDuringInitialization = null;
        TestLevelInitializationHandler.AddUpdateEntity = false;
        TestLevelInitializationHandler.UpdateEntity = null;
        TestLevelInitializationHandler.PublishAfterCommit = false;
        TestLevelInitializationHandler.AfterCommitWasPublished = false;
        TestLevelInitializationHandler.AfterCommitRanDuringInitialization = false;

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

    [Test]
    public void LevelEntitiesEnterRootUpdateSchedulingOnlyAfterCommit()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var eventSystem = new EventSystem();
        eventSystem.Init();
        eventSystem.Register();
        TestLevelInitializationHandler.AddUpdateEntity = true;
        TestLevelInitializationHandler.PublishAfterCommit = true;

        try
        {
            LevelSceneFactory.Create(
                gameplayWorld,
                gameplayWorld,
                ROOT_LEVEL_CONFIG_ID,
                LEVEL_SCENE_ID,
                nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult();

            OrdinaryUpdateEntity entity = TestLevelInitializationHandler.UpdateEntity
                ?? throw new InvalidOperationException("The initialization handler did not create an update entity.");
            Assert.That(TestLevelInitializationHandler.AfterCommitRanDuringInitialization, Is.False);
            Assert.That(TestLevelInitializationHandler.AfterCommitWasPublished, Is.True);
            Assert.That(
                ExecutionLog.Exists(entry => entry.EndsWith($":{entity.InstanceId}", StringComparison.Ordinal)),
                Is.False);

            _root.EarlyUpdate();
            _root.Update();

            Assert.That(ExecutionLog, Does.Contain($"rootUpdate:{entity.InstanceId}"));
        }
        finally
        {
            eventSystem.Dispose();
        }
    }

    [TearDown]
    public void TearDown()
    {
        _root?.Dispose();
        _entitySystemManager.Dispose();
        _typeManager.Dispose();
    }

    [Test]
    public void GeneralSceneFactoryRejectsLevelScenesWithoutAddingAChild()
    {
        Assert.Throws<InvalidOperationException>(() =>
            LogicSceneFactoryCore.CreateLogicScene(
                _root.Scene,
                LEVEL_SCENE_ID,
                LEVEL_SCENE_INSTANCE_ID,
                ELogicSceneType.Level,
                nameof(LEVEL_SCENE_ID)));

        Assert.That(_root.Scene.GetChild<LogicScene>(LEVEL_SCENE_ID), Is.Null);
    }

    [Test]
    public void MissingLevelInitializationHandlerRemovesNewSceneAndReportsError()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var eventSystem = new EventSystem();
        eventSystem.Register();

        try
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                LevelSceneFactory.Create(
                    gameplayWorld,
                    gameplayWorld,
                    LEVEL_SCENE_ID,
                    LEVEL_SCENE_ID,
                    nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult());

            Assert.That(exception.Message, Does.Contain(nameof(Event_AfterCreateLevelScene)));
            Assert.That(gameplayWorld.GetChild<LogicScene>(LEVEL_SCENE_ID), Is.Null);
        }
        finally
        {
            eventSystem.Dispose();
        }
    }

    [Test]
    public void LevelFactoryRejectsParentOutsideOwningWorldWithoutAddingScene()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));

        Assert.Throws<InvalidOperationException>(() =>
            LevelSceneFactory.Create(
                _root.Scene,
                gameplayWorld,
                LEVEL_SCENE_ID,
                LEVEL_SCENE_ID,
                nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult());

        Assert.That(_root.Scene.GetChild<LogicScene>(LEVEL_SCENE_ID), Is.Null);
    }

    [Test]
    public void LevelInitializationFailurePropagatesAndRemovesScene()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var eventSystem = new EventSystem();
        eventSystem.Init();
        eventSystem.Register();
        TestLevelInitializationHandler.Fail = true;

        try
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                LevelSceneFactory.Create(
                    gameplayWorld,
                    gameplayWorld,
                    LEVEL_SCENE_ID,
                    LEVEL_SCENE_ID,
                    nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult());

            Assert.That(exception.Message, Is.EqualTo("level initialization failed"));
            Assert.That(gameplayWorld.GetChild<LogicScene>(LEVEL_SCENE_ID), Is.Null);
        }
        finally
        {
            TestLevelInitializationHandler.Fail = false;
            eventSystem.Dispose();
        }
    }

    [Test]
    public void NestedLevelScenesShareWorldAndActivateTogetherAfterAncestorInitialization()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var eventSystem = new EventSystem();
        eventSystem.Init();
        eventSystem.Register();
        TestLevelInitializationHandler.CreateNested = true;
        TestLevelInitializationHandler.NestedWasCommittedDuringParentInitialization = true;

        try
        {
            LogicScene rootLevel = LevelSceneFactory.Create(
                gameplayWorld,
                gameplayWorld,
                ROOT_LEVEL_CONFIG_ID,
                LEVEL_SCENE_ID,
                nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult();
            LogicScene nestedLevel = rootLevel.GetChild<LogicScene>(NESTED_LEVEL_ID);

            Assert.That(nestedLevel, Is.Not.Null);
            Assert.That(TestLevelInitializationHandler.NestedWasCommittedDuringParentInitialization, Is.False);
            Assert.That(rootLevel.GetComponent<LevelGameWorldComponent>().IsCommitted, Is.True);
            Assert.That(nestedLevel.GetComponent<LevelGameWorldComponent>().IsCommitted, Is.True);
            Assert.That(nestedLevel.GetComponent<LevelGameWorldComponent>().GameplayWorld, Is.SameAs(gameplayWorld));
        }
        finally
        {
            TestLevelInitializationHandler.CreateNested = false;
            eventSystem.Dispose();
        }
    }

    [Test]
    public void FailedLevelCommitRollsBackAppliedEffectsAndRemovesScene()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var eventSystem = new EventSystem();
        eventSystem.Init();
        eventSystem.Register();
        TestLevelInitializationHandler.FailCommit = true;
        TestLevelInitializationHandler.AppliedEffectCount = 0;

        try
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                LevelSceneFactory.Create(
                    gameplayWorld,
                    gameplayWorld,
                    ROOT_LEVEL_CONFIG_ID,
                    LEVEL_SCENE_ID,
                    nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult());

            Assert.That(exception.Message, Is.EqualTo("level commit failed"));
            Assert.That(TestLevelInitializationHandler.AppliedEffectCount, Is.Zero);
            Assert.That(gameplayWorld.GetChild<LogicScene>(LEVEL_SCENE_ID), Is.Null);
        }
        finally
        {
            TestLevelInitializationHandler.FailCommit = false;
            TestLevelInitializationHandler.AddUpdateEntity = false;
            TestLevelInitializationHandler.UpdateEntity = null;
            eventSystem.Dispose();
        }
    }

    [Test]
    public void LevelCommitRejectsSceneMovedOutsideOwningWorldAndCleansItUp()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var eventSystem = new EventSystem();
        eventSystem.Init();
        eventSystem.Register();
        TestLevelInitializationHandler.ParentDuringInitialization = _root.Scene;

        try
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                LevelSceneFactory.Create(
                    gameplayWorld,
                    gameplayWorld,
                    ROOT_LEVEL_CONFIG_ID,
                    LEVEL_SCENE_ID,
                    nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult());

            Assert.That(exception.Message, Does.Contain("within its GameplayWorld subtree"));
            Assert.That(gameplayWorld.GetChild<LogicScene>(LEVEL_SCENE_ID), Is.Null);
            Assert.That(_root.Scene.GetChild<LogicScene>(LEVEL_SCENE_ID), Is.Null);
        }
        finally
        {
            TestLevelInitializationHandler.ParentDuringInitialization = null;
            eventSystem.Dispose();
        }
    }

    [Test]
    public void NestedLevelSceneRejectsConflictingGameplayWorld()
    {
        GameplayWorld gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        GameplayWorld otherWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID + 1,
            GAMEPLAY_WORLD_INSTANCE_ID + 1,
            nameof(otherWorld));
        var eventSystem = new EventSystem();
        eventSystem.Init();
        eventSystem.Register();

        try
        {
            LogicScene levelScene = LevelSceneFactory.Create(
                gameplayWorld,
                gameplayWorld,
                ROOT_LEVEL_CONFIG_ID,
                LEVEL_SCENE_ID,
                nameof(LEVEL_SCENE_ID)).GetAwaiter().GetResult();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                LevelSceneFactory.Create(
                    levelScene,
                    otherWorld,
                    NESTED_LEVEL_CONFIG_ID,
                    NESTED_LEVEL_ID,
                    nameof(NESTED_LEVEL_ID)).GetAwaiter().GetResult());

            Assert.That(exception.Message, Does.Contain("share the same GameplayWorld"));
            Assert.That(levelScene.GetChild<LogicScene>(NESTED_LEVEL_ID), Is.Null);
        }
        finally
        {
            eventSystem.Dispose();
        }
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
            ELogicSceneType.Client,
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

    [Event(ELogicSceneType.Level)]
    public class TestLevelInitializationHandler : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        public static bool Fail { get; set; }
        public static bool CreateNested { get; set; }
        public static bool NestedWasCommittedDuringParentInitialization { get; set; }
        public static bool FailCommit { get; set; }
        public static int AppliedEffectCount { get; set; }
        public static Entity? ParentDuringInitialization { get; set; }
        public static bool AddUpdateEntity { get; set; }
        public static OrdinaryUpdateEntity? UpdateEntity { get; set; }
        public static bool PublishAfterCommit { get; set; }
        public static bool AfterCommitWasPublished { get; set; }
        public static bool AfterCommitRanDuringInitialization { get; set; }

        protected override async ETTask Run(LogicScene scene, Event_AfterCreateLevelScene eventInfo)
        {
            if (Fail)
            {
                throw new InvalidOperationException("level initialization failed");
            }

            LevelGameWorldComponent association = scene.GetComponent<LevelGameWorldComponent>();
            if (ParentDuringInitialization != null)
            {
                scene.Parent = ParentDuringInitialization;
            }

            if (AddUpdateEntity)
            {
                LogicScene nestedScene = LogicSceneFactoryCore.CreateLogicScene(
                    scene,
                    NESTED_CLIENT_SCENE_ID,
                    NESTED_CLIENT_SCENE_ID,
                    ELogicSceneType.Client,
                    nameof(NESTED_CLIENT_SCENE_ID));
                UpdateEntity = nestedScene.AddChild<OrdinaryUpdateEntity>();
                scene.Root.EarlyUpdate();
                scene.Root.Update();
            }

            if (PublishAfterCommit)
            {
                association.AfterCommit(() => AfterCommitWasPublished = true);
                AfterCommitRanDuringInitialization = AfterCommitWasPublished;
            }

            if (CreateNested && eventInfo.LevelConfigId == ROOT_LEVEL_CONFIG_ID)
            {
                LogicScene nestedScene = await LevelSceneFactory.Create(
                    scene,
                    association.GameplayWorld,
                    NESTED_LEVEL_CONFIG_ID,
                    NESTED_LEVEL_ID,
                    nameof(NESTED_LEVEL_ID));
                NestedWasCommittedDuringParentInitialization =
                    nestedScene.GetComponent<LevelGameWorldComponent>().IsCommitted;
            }

            if (FailCommit)
            {
                association.DeferUntilCommit(
                    () => AppliedEffectCount++,
                    () => AppliedEffectCount--);
                association.DeferUntilCommit(
                    () => throw new InvalidOperationException("level commit failed"),
                    static () => { });
            }

            await ETTask.CompletedTask;
        }
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
