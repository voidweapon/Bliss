using System.Reflection;
using System.Threading;
using Bliss.Core;

namespace Bliss.Tests;

[NonParallelizable]
public class GameplayWorldUpdaterTests
{
    private const int FIRST_TEST_INSTANCE_ID = 10000;
    private const long INVALID_INSTANCE_ID = 0;
    private const int EARLY_GROUP_FIRST = 50;
    private const int EARLY_GROUP_SECOND = 75;
    private const int UPDATE_GROUP_CUSTOM = 150;
    private const int LATE_GROUP_CUSTOM = 250;
    private const int LOW_PRIORITY = -10;
    private const int HIGH_PRIORITY = 10;

    private TypeManager _typeManager;
    private EntitySystemManager _entitySystemManager;
    private GameplayWorldUpdater _updater;
    private int _nextInstanceId;

    public static List<string> ExecutionLog { get; } = new List<string>();
    public static Action<GameplayEntity> EarlyUpdateCallback { get; set; }

    [SetUp]
    public void SetUp()
    {
        ExecutionLog.Clear();
        EarlyUpdateCallback = null;
        _nextInstanceId = FIRST_TEST_INSTANCE_ID;

        _typeManager = new TypeManager();
        _typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorldUpdaterTests).Assembly,
        });
        _typeManager.Register();

        _entitySystemManager = new EntitySystemManager();
        _entitySystemManager.Init();
        _entitySystemManager.Register();
        _updater = new GameplayWorldUpdater();
    }

    [TearDown]
    public void TearDown()
    {
        EarlyUpdateCallback = null;
        _entitySystemManager.Dispose();
        _typeManager.Dispose();
    }

    [Test]
    public void EarlyUpdateRunsOnlyEarlySystems()
    {
        var entity = CreateEntity();
        _updater.Add(entity);

        _updater.EarlyUpdate();

        CollectionAssert.AreEqual(
            new[]
            {
                $"early50:{entity.InstanceId}",
                $"early75Low:{entity.InstanceId}",
                $"early75High:{entity.InstanceId}",
                $"early100:{entity.InstanceId}",
            },
            ExecutionLog);
    }

    [Test]
    public void UpdatePhasesRunInGroupPriorityAndEntityRegistrationOrder()
    {
        var firstEntity = CreateEntity();
        var secondEntity = CreateEntity();
        _updater.Add(firstEntity);
        _updater.Add(secondEntity);

        _updater.EarlyUpdate();

        CollectionAssert.AreEqual(
            new[]
            {
                $"early50:{firstEntity.InstanceId}",
                $"early50:{secondEntity.InstanceId}",
                $"early75Low:{firstEntity.InstanceId}",
                $"early75Low:{secondEntity.InstanceId}",
                $"early75High:{firstEntity.InstanceId}",
                $"early75High:{secondEntity.InstanceId}",
                $"early100:{firstEntity.InstanceId}",
                $"early100:{secondEntity.InstanceId}",
            },
            ExecutionLog);

        ExecutionLog.Clear();
        _updater.Update();

        CollectionAssert.AreEqual(
            new[]
            {
                $"update150:{firstEntity.InstanceId}",
                $"update150:{secondEntity.InstanceId}",
                $"update200:{firstEntity.InstanceId}",
                $"update200:{secondEntity.InstanceId}",
            },
            ExecutionLog);

        ExecutionLog.Clear();
        _updater.LateUpdate();

        CollectionAssert.AreEqual(
            new[]
            {
                $"late250:{firstEntity.InstanceId}",
                $"late250:{secondEntity.InstanceId}",
                $"late300:{firstEntity.InstanceId}",
                $"late300:{secondEntity.InstanceId}",
            },
            ExecutionLog);
    }

    [Test]
    public void EntitiesAddedAfterEarlyUpdateWaitUntilTheNextFrame()
    {
        var originalEntity = CreateEntity();
        var deferredEntity = CreateEntity();
        EarlyUpdateCallback = _ =>
        {
            EarlyUpdateCallback = null;
            _updater.Add(deferredEntity);
        };
        _updater.Add(originalEntity);

        _updater.EarlyUpdate();
        _updater.Update();
        _updater.LateUpdate();

        Assert.That(
            ExecutionLog.Exists(entry => entry.EndsWith($":{deferredEntity.InstanceId}", StringComparison.Ordinal)),
            Is.False);

        ExecutionLog.Clear();
        _updater.EarlyUpdate();
        _updater.Update();
        _updater.LateUpdate();

        CollectionAssert.AreEqual(
            new[]
            {
                $"early50:{originalEntity.InstanceId}",
                $"early50:{deferredEntity.InstanceId}",
                $"early75Low:{originalEntity.InstanceId}",
                $"early75Low:{deferredEntity.InstanceId}",
                $"early75High:{originalEntity.InstanceId}",
                $"early75High:{deferredEntity.InstanceId}",
                $"early100:{originalEntity.InstanceId}",
                $"early100:{deferredEntity.InstanceId}",
                $"update150:{originalEntity.InstanceId}",
                $"update150:{deferredEntity.InstanceId}",
                $"update200:{originalEntity.InstanceId}",
                $"update200:{deferredEntity.InstanceId}",
                $"late250:{originalEntity.InstanceId}",
                $"late250:{deferredEntity.InstanceId}",
                $"late300:{originalEntity.InstanceId}",
                $"late300:{deferredEntity.InstanceId}",
            },
            ExecutionLog);
    }

    [Test]
    public void StaleEntityReferencesAreSkipped()
    {
        var entity = CreateEntity();
        _updater.Add(entity);
        entity.Invalidate();

        _updater.EarlyUpdate();
        _updater.Update();
        _updater.LateUpdate();

        Assert.That(ExecutionLog, Is.Empty);
    }

    private GameplayEntity CreateEntity()
    {
        var entity = new GameplayEntity();
        entity.SetInstanceId(Interlocked.Increment(ref _nextInstanceId));
        return entity;
    }

    public class GameplayEntity : Entity, IEarlyUpdate, IUpdate, ILateUpdate
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

    [EntitySystem]
    public class EarlyGroup50System : EarlyUpdateSystem<GameplayEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)EARLY_GROUP_FIRST;

        protected override void EarlyUpdate(GameplayEntity entity)
        {
            ExecutionLog.Add($"early50:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class EarlyGroup75LowPrioritySystem : EarlyUpdateSystem<GameplayEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)EARLY_GROUP_SECOND;

        public override int GetPriority() => LOW_PRIORITY;

        protected override void EarlyUpdate(GameplayEntity entity)
        {
            ExecutionLog.Add($"early75Low:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class EarlyGroup75HighPrioritySystem : EarlyUpdateSystem<GameplayEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)EARLY_GROUP_SECOND;

        public override int GetPriority() => HIGH_PRIORITY;

        protected override void EarlyUpdate(GameplayEntity entity)
        {
            ExecutionLog.Add($"early75High:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class EarlyGameplaySystem : EarlyUpdateSystem<GameplayEntity>
    {
        protected override void EarlyUpdate(GameplayEntity entity)
        {
            ExecutionLog.Add($"early100:{entity.InstanceId}");
            EarlyUpdateCallback?.Invoke(entity);
        }
    }

    [EntitySystem]
    public class UpdateGroup150System : UpdateSystem<GameplayEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)UPDATE_GROUP_CUSTOM;

        protected override void Update(GameplayEntity entity)
        {
            ExecutionLog.Add($"update150:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class UpdateGameplaySystem : UpdateSystem<GameplayEntity>
    {
        protected override void Update(GameplayEntity entity)
        {
            ExecutionLog.Add($"update200:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class LateGroup250System : LateUpdateSystem<GameplayEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)LATE_GROUP_CUSTOM;

        protected override void LateUpdate(GameplayEntity entity)
        {
            ExecutionLog.Add($"late250:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class LateGameplaySystem : LateUpdateSystem<GameplayEntity>
    {
        protected override void LateUpdate(GameplayEntity entity)
        {
            ExecutionLog.Add($"late300:{entity.InstanceId}");
        }
    }
}