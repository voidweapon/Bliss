using System.Reflection;
using System.Threading;
using Bliss.Core;

namespace Bliss.Tests;

[NonParallelizable]
public class EntitySystemSchedulingTests
{
    private const int FIRST_TEST_INSTANCE_ID = 1000;
    private const long INVALID_INSTANCE_ID = 0;
    private const int EARLY_GROUP_FIRST = 50;
    private const int EARLY_GROUP_SECOND = 75;
    private const int UPDATE_GROUP_CUSTOM = 150;
    private const int LATE_GROUP_CUSTOM = 250;
    private const int INVALID_GROUP_VALUE = 999;
    private const int EARLY_SYSTEM_PRIORITY = 100;
    private const int LOWER_PRIORITY = -10;
    private const int HIGHER_PRIORITY = 10;
    private const int DEFAULT_PRIORITY = 0;

    private TypeManager _typeManager;
    private EntitySystemManager _entitySystemManager;
    private EntitySystem _entitySystem;
    private int _nextInstanceId;

    public static List<string> ExecutionLog { get; } = new List<string>();
    public static Action<SchedulingEntity> EarlyUpdateCallback { get; set; }
    public static bool UseInvalidUpdateGroup { get; set; }
    public static bool UseInvalidUpdateCategory { get; set; }
    public static bool UseInvalidEntityConstraint { get; set; }

    [SetUp]
    public void SetUp()
    {
        ExecutionLog.Clear();
        EarlyUpdateCallback = null;
        UseInvalidUpdateGroup = false;
        UseInvalidUpdateCategory = false;
        UseInvalidEntityConstraint = false;
        InvalidGroupUpdateSystem.GroupValue = INVALID_GROUP_VALUE;
        _nextInstanceId = FIRST_TEST_INSTANCE_ID;

        _typeManager = new TypeManager();
        _typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(EntitySystemSchedulingTests).Assembly,
        });
        _typeManager.Register();

        _entitySystemManager = new EntitySystemManager();
        _entitySystemManager.Init();
        _entitySystemManager.Register();
        _entitySystem = new EntitySystem();
    }

    [TearDown]
    public void TearDown()
    {
        EarlyUpdateCallback = null;
        UseInvalidUpdateGroup = false;
        UseInvalidUpdateCategory = false;
        UseInvalidEntityConstraint = false;
        InvalidGroupUpdateSystem.GroupValue = INVALID_GROUP_VALUE;
        _entitySystemManager.Dispose();
        _typeManager.Dispose();
    }

    [Test]
    public void UpdateEntriesRunTheirNumericRangesInGroupPriorityAndRegistrationOrder()
    {
        var firstEntity = CreateEntity();
        var secondEntity = CreateEntity();
        _entitySystem.RegisterSystem(firstEntity);
        _entitySystem.RegisterSystem(secondEntity);

        _entitySystem.EarlyUpdate();
        CollectionAssert.AreEqual(
            new[]
            {
                $"early50:{firstEntity.InstanceId}",
                $"early50:{secondEntity.InstanceId}",
                $"early75LowPriority:{firstEntity.InstanceId}",
                $"early75LowPriority:{secondEntity.InstanceId}",
                $"early75HighPriority:{firstEntity.InstanceId}",
                $"early75HighPriority:{secondEntity.InstanceId}",
                $"early100:{firstEntity.InstanceId}",
                $"early100:{secondEntity.InstanceId}",
            },
            ExecutionLog);

        ExecutionLog.Clear();
        _entitySystem.Update();
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
        _entitySystem.LateUpdate();
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
    public void EntitiesRegisteredAfterEarlyUpdateBoundaryWaitUntilNextFrame()
    {
        var originalEntity = CreateEntity();
        var deferredEntity = new DeferredUpdateEntity();
        deferredEntity.SetInstanceId(Interlocked.Increment(ref _nextInstanceId));
        EarlyUpdateCallback = _ =>
        {
            EarlyUpdateCallback = null;
            _entitySystem.RegisterSystem(deferredEntity);
        };
        _entitySystem.RegisterSystem(originalEntity);

        _entitySystem.EarlyUpdate();
        _entitySystem.Update();
        _entitySystem.LateUpdate();

        Assert.That(
            ExecutionLog.Exists(entry => entry.EndsWith($":{deferredEntity.InstanceId}", StringComparison.Ordinal)),
            Is.False);

        ExecutionLog.Clear();
        _entitySystem.EarlyUpdate();
        _entitySystem.Update();

        Assert.That(ExecutionLog, Does.Contain($"deferredUpdate:{deferredEntity.InstanceId}"));
    }

    [Test]
    public void StaleEntityReferencesAreSkipped()
    {
        var entity = CreateEntity();
        _entitySystem.RegisterSystem(entity);
        entity.Invalidate();

        _entitySystem.EarlyUpdate();

        Assert.That(ExecutionLog, Is.Empty);
    }

    [Test]
    public void UnregisteredEntityDoesNotRunWhileOtherEntitiesRemainScheduled()
    {
        var unregisteredEntity = CreateEntity();
        var registeredEntity = CreateEntity();
        _entitySystem.RegisterSystem(unregisteredEntity);
        _entitySystem.RegisterSystem(registeredEntity);

        _entitySystem.UnregisterSystem(unregisteredEntity);
        _entitySystem.EarlyUpdate();
        _entitySystem.Update();
        _entitySystem.LateUpdate();

        Assert.That(ExecutionLog.Exists(entry =>
            entry.EndsWith($":{unregisteredEntity.InstanceId}", StringComparison.Ordinal)), Is.False);
        Assert.That(ExecutionLog.Exists(entry =>
            entry.EndsWith($":{registeredEntity.InstanceId}", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void EntitiesWithoutUpdateSystemsAreNotScheduled()
    {
        var entity = new PlainEntity();
        _entitySystem.RegisterSystem(entity);

        _entitySystem.EarlyUpdate();
        _entitySystem.Update();
        _entitySystem.LateUpdate();

        Assert.That(ExecutionLog, Is.Empty);
    }

    [Test]
    public void InvalidUpdateGroupFailsDuringSystemRegistrationWithSystemAndValue()
    {
        UseInvalidUpdateGroup = true;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _entitySystemManager.Init());

        Assert.That(exception.Message, Does.Contain(nameof(InvalidGroupUpdateSystem)));
        Assert.That(exception.Message, Does.Contain(INVALID_GROUP_VALUE.ToString()));
    }

    [Test]
    public void UpdateSystemCannotUseAnotherCategorysStageRange()
    {
        UseInvalidUpdateGroup = true;
        InvalidGroupUpdateSystem.GroupValue = EARLY_GROUP_SECOND;

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => _entitySystemManager.Init());

        Assert.That(exception.Message, Does.Contain(nameof(InvalidGroupUpdateSystem)));
        Assert.That(exception.Message, Does.Contain(EARLY_GROUP_SECOND.ToString()));
    }

    [Test]
    public void UpdateSystemCategoryMustMatchItsImplementation()
    {
        UseInvalidUpdateCategory = true;

        var exception = Assert.Throws<ArgumentException>(() => _entitySystemManager.Init());

        Assert.That(exception.Message, Does.Contain(nameof(DirectUpdateSystem)));
    }

    [Test]
    public void UpdateEntityTypeMustImplementItsCategoryConstraint()
    {
        UseInvalidEntityConstraint = true;

        var exception = Assert.Throws<ArgumentException>(() => _entitySystemManager.Init());

        Assert.That(exception.Message, Does.Contain(nameof(DirectUpdateSystem)));
        Assert.That(exception.Message, Does.Contain(nameof(PlainEntity)));
    }

    private SchedulingEntity CreateEntity()
    {
        var entity = new SchedulingEntity();
        entity.SetInstanceId(Interlocked.Increment(ref _nextInstanceId));
        return entity;
    }

    public class SchedulingEntity : Entity, IEarlyUpdate, IUpdate, ILateUpdate
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

    public class PlainEntity : Entity
    {
    }

    public class DeferredUpdateEntity : Entity, IUpdate
    {
        public void SetInstanceId(long instanceId)
        {
            InstanceId = instanceId;
        }
    }

    [EntitySystem]
    public class EarlyGroup50System : EarlyUpdateSystem<SchedulingEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)EARLY_GROUP_FIRST;

        public override int GetPriority() => EARLY_SYSTEM_PRIORITY;

        protected override void EarlyUpdate(SchedulingEntity entity)
        {
            ExecutionLog.Add($"early50:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class EarlyGroup75LowPrioritySystem : EarlyUpdateSystem<SchedulingEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)EARLY_GROUP_SECOND;

        public override int GetPriority() => LOWER_PRIORITY;

        protected override void EarlyUpdate(SchedulingEntity entity)
        {
            ExecutionLog.Add($"early75LowPriority:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class EarlyGroup75HighPrioritySystem : EarlyUpdateSystem<SchedulingEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)EARLY_GROUP_SECOND;

        public override int GetPriority() => HIGHER_PRIORITY;

        protected override void EarlyUpdate(SchedulingEntity entity)
        {
            ExecutionLog.Add($"early75HighPriority:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class EarlyDefaultGroupSystem : EarlyUpdateSystem<SchedulingEntity>
    {
        protected override void EarlyUpdate(SchedulingEntity entity)
        {
            ExecutionLog.Add($"early100:{entity.InstanceId}");
            EarlyUpdateCallback?.Invoke(entity);
        }
    }

    [EntitySystem]
    public class UpdateGroup150System : UpdateSystem<SchedulingEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)UPDATE_GROUP_CUSTOM;

        protected override void Update(SchedulingEntity entity)
        {
            ExecutionLog.Add($"update150:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class UpdateDefaultGroupSystem : UpdateSystem<SchedulingEntity>
    {
        protected override void Update(SchedulingEntity entity)
        {
            ExecutionLog.Add($"update200:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class LateGroup250System : LateUpdateSystem<SchedulingEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)LATE_GROUP_CUSTOM;

        protected override void LateUpdate(SchedulingEntity entity)
        {
            ExecutionLog.Add($"late250:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class LateDefaultGroupSystem : LateUpdateSystem<SchedulingEntity>
    {
        protected override void LateUpdate(SchedulingEntity entity)
        {
            ExecutionLog.Add($"late300:{entity.InstanceId}");
        }
    }

    [EntitySystem]
    public class InvalidGroupUpdateSystem : UpdateSystem<SchedulingEntity>
    {
        public static int GroupValue { get; set; } = INVALID_GROUP_VALUE;

        public override ESystemUpdateGroup GetUpdateGroup()
        {
            return UseInvalidUpdateGroup ? (ESystemUpdateGroup)GroupValue : base.GetUpdateGroup();
        }

        protected override void Update(SchedulingEntity entity)
        {
        }
    }

    [EntitySystem]
    public class DirectUpdateSystem : SystemObject, IUpdateSystem
    {
        public Type GetEntityType()
        {
            return UseInvalidEntityConstraint ? typeof(PlainEntity) : typeof(SchedulingEntity);
        }

        public Type GetSystemCategoryType()
        {
            return UseInvalidUpdateCategory ? typeof(IEarlyUpdateSystem) : typeof(IUpdateSystem);
        }

        public ESystemUpdateGroup GetUpdateGroup()
        {
            return ESystemUpdateGroup.Update;
        }

        public int GetPriority()
        {
            return DEFAULT_PRIORITY;
        }

        public int CompareTo(ISystem other)
        {
            return GetPriority().CompareTo(other.GetPriority());
        }

        public void Run(Entity entity)
        {
        }
    }

    [EntitySystem]
    public class DeferredUpdateSystem : UpdateSystem<DeferredUpdateEntity>
    {
        public override ESystemUpdateGroup GetUpdateGroup() => (ESystemUpdateGroup)UPDATE_GROUP_CUSTOM;

        protected override void Update(DeferredUpdateEntity entity)
        {
            ExecutionLog.Add($"deferredUpdate:{entity.InstanceId}");
        }
    }
}
