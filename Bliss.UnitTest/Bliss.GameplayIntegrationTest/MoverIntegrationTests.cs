using System.Reflection;
using Bliss;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Physics;
using Bliss.Timer;
using ET;
using MagicPhysX;
using NUnit.Framework;
using Unity.Mathematics;
using static MagicPhysX.NativeMethods;

namespace Bliss.GameplayIntegrationTest;

/// <summary>Exercises Mover simulation through actual Root, World and LevelScene lifecycles.</summary>
[NonParallelizable]
public class MoverIntegrationTests
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int LEVEL_ID = 303;
    private const int MOVER_CONFIG_ID = 1;
    private const int BULLET_CONFIG_ID = 1;
    private const long BULLET_ID = 404;
    private const long INITIAL_TIME = 1000;
    private const long FIRST_TICK_TIME = 1050;
    private const long DELAYED_TICK_TIME = 1500;
    private const long NO_SERVER_TIME_OFFSET = 0;
    private const float UNIT_AXIS = 1f;
    private const float ZERO_AXIS = 0f;
    private const float EXPECTED_FIRST_POSITION = 0.5f;
    private const float EXPECTED_SECOND_POSITION = 1f;
    private const float INITIAL_POSITION = 0f;
    private const float UNOBSERVED_POSITION = -1f;
    private const float POSITION_TOLERANCE = 0.0001f;
    private const float EXPECTED_DELTA_SECONDS = 0.05f;
    private const int FIRST_SIMULATION_COUNT = 1;
    private const int SECOND_SIMULATION_COUNT = 2;
    private const int NO_REGISTRATIONS = 0;
    private const int SINGLE_REGISTRATION = 1;
    private const int TWO_REGISTRATIONS = 2;
    private const long ELAPSED_INTERPOLATION_MILLISECONDS = 1000;
    private const float PHYSICS_INITIAL_HEIGHT = 10f;
    private const float PHYSICS_IDENTITY_W = 1f;
    private const float PHYSICS_FIRST_TICK_VELOCITY = -0.49f;
    private const float PHYSICS_SECOND_TICK_VELOCITY = -0.98f;
    private const long NEXT_DELAYED_TICK_TIME = 1550;
    private const int FIRST_MANAGED_ROOT_ID = 501;
    private const int SECOND_MANAGED_ROOT_ID = 502;
    private const int CHILD_ROOT_ID = 503;
    private const long HEALTHY_WORLD_ID = 504;
    private const long HEALTHY_WORLD_INSTANCE_ID = 10002;
    private const int SUPPRESS_INFO_LOG_LEVEL = 4;
    private const long FAULTED_WORLD_TIMER_INTERVAL = 200;
    private static readonly float3 moveDirection = new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS);
    private static readonly Action<Entity, float> recordLogicCallback = recordLogicUpdate;
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
    private static readonly Action<Root> lateUpdate =
        (typeof(Root).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.LateUpdate was not found."))
        .CreateDelegate<Action<Root>>();

    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private ConfigManager configManager = null!;
    private MoverHandlerManager handlerManager = null!;
    private Root root = null!;
    private GameplayWorld world = null!;
    private LogicScene level = null!;
    private string previousDirectory = null!;

    /// <summary>Creates production registries and a LevelScene without external physics resources.</summary>
    [SetUp]
    public void SetUp()
    {
        this.previousDirectory = Directory.GetCurrentDirectory();
        string directory = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(directory, "ServerRes", "Configs")))
        {
            directory = Directory.GetParent(directory)?.FullName
                ?? throw new InvalidOperationException("ServerRes/Configs was not found.");
        }

        Directory.SetCurrentDirectory(Path.Combine(directory, "Server"));
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(MoverComponentSystem).Assembly,
            typeof(MoverIntegrationTests).Assembly,
        });
        // The server handlers require external physics assets; the fixture supplies empty Level resource handlers.
        this.typeManager.GetTypes(typeof(EventAttribute)).Remove(typeof(EventHandler_AfterCreateLevelScene_Server));
        this.typeManager.GetTypes(typeof(EventAttribute)).Remove(typeof(EventHandler_EnableLevelScene_Server));
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);
        this.configManager = new ConfigManager();
        this.configManager.Init();
        registerSingleton.Invoke(this.configManager, null);
        this.handlerManager = new MoverHandlerManager();
        this.handlerManager.Init();
        registerSingleton.Invoke(this.handlerManager, null);
        setServerTime(INITIAL_TIME);
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(MoverIntegrationTests) });
        this.root.Scene.AddComponent<TimerComponent>();
        this.world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        this.level = LevelSceneFactory.Create(
            this.world, this.world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
    }

    /// <summary>Releases production registries and restores the working directory.</summary>
    [TearDown]
    public void TearDown()
    {
        this.root?.Dispose();
        this.handlerManager?.Dispose();
        this.configManager?.Dispose();
        this.eventSystem?.Dispose();
        this.entitySystemManager?.Dispose();
        this.typeManager?.Dispose();
        TimeInfo.Instance.ServerMinusClientTime = NO_SERVER_TIME_OFFSET;
        Directory.SetCurrentDirectory(this.previousDirectory);
    }

    /// <summary>Delayed Root updates advance a LevelScene Mover once with a 50 ms step.</summary>
    [Test]
    public void LevelMoverAdvancesByOneFixedStepPerDueTick()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        TestCargo cargo = this.level.AddChild<TestCargo>();
        TransformComponent transform = cargo.AddComponent<TransformComponent>();
        MoverComponent component = cargo.AddComponent<MoverComponent>();
        long moverId = component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo
        {
            TargetDir = moveDirection,
        });
        Assert.That(component.GetChild<Mover>(moverId), Is.Not.Null);
        earlyUpdate(this.root);
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.Zero.Within(POSITION_TOLERANCE));

        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.EqualTo(EXPECTED_FIRST_POSITION).Within(POSITION_TOLERANCE));

        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.EqualTo(EXPECTED_SECOND_POSITION).Within(POSITION_TOLERANCE));
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.EqualTo(EXPECTED_SECOND_POSITION).Within(POSITION_TOLERANCE));
    }

    /// <summary>A prepared but disabled LevelScene does not advance its Movers.</summary>
    [Test]
    public void PreparedLevelMoverWaitsUntilLevelIsEnabled()
    {
        TestCargo cargo = this.level.AddChild<TestCargo>();
        TransformComponent transform = cargo.AddComponent<TransformComponent>();
        MoverComponent component = cargo.AddComponent<MoverComponent>();
        component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo
        {
            TargetDir = moveDirection,
        });
        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.Zero.Within(POSITION_TOLERANCE));

        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.EqualTo(EXPECTED_FIRST_POSITION).Within(POSITION_TOLERANCE));
    }

    /// <summary>A Mover shares its movement priority in registration order.</summary>
    [TestCase(true, EXPECTED_FIRST_POSITION)]
    [TestCase(false, INITIAL_POSITION)]
    public void MovementPriorityRespectsRegistrationOrder(bool moverFirst, float observedPosition)
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        TestCargo cargo = this.level.AddChild<TestCargo>();
        TransformComponent transform = cargo.AddComponent<TransformComponent>();
        float observation = UNOBSERVED_POSITION;
        if (!moverFirst)
        {
            this.world.LogicUpdate.Register(cargo, nameof(observation), GameplayWorldLogicPriority.MOVEMENT,
                _ => observation = transform.Transform.Location.x);
        }

        MoverComponent component = cargo.AddComponent<MoverComponent>();
        component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo
        {
            TargetDir = moveDirection,
        });
        if (moverFirst)
        {
            this.world.LogicUpdate.Register(cargo, nameof(observation), GameplayWorldLogicPriority.MOVEMENT,
                _ => observation = transform.Transform.Location.x);
        }

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);

        Assert.That(observation, Is.EqualTo(observedPosition).Within(POSITION_TOLERANCE));
        Assert.That(transform.Transform.Location.x, Is.EqualTo(EXPECTED_FIRST_POSITION).Within(POSITION_TOLERANCE));
    }

    /// <summary>Disposing and replacing a Mover during dispatch skips the old entry until the next tick.</summary>
    [Test]
    public void DestroyedMoverIsSkippedAndReplacementStartsOnNextTick()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        TestCargo cargo = this.level.AddChild<TestCargo>();
        TransformComponent transform = cargo.AddComponent<TransformComponent>();
        MoverComponent? component = null;
        Mover? oldMover = null;
        MoverStageRuntime? oldStage = null;
        bool replaced = false;
        this.world.LogicUpdate.Register(cargo, nameof(replaced), GameplayWorldLogicPriority.MOVEMENT, _ =>
        {
            if (replaced)
            {
                return;
            }

            replaced = true;
            component!.Dispose();
            component = cargo.AddComponent<MoverComponent>();
            component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo
            {
                TargetDir = moveDirection,
            });
        });
        component = cargo.AddComponent<MoverComponent>();
        long moverId = component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo
        {
            TargetDir = moveDirection,
        });
        oldMover = component.GetChild<Mover>(moverId);
        oldStage = oldMover.CurrentStage;

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(oldMover.IsDisposed, Is.True);
        Assert.That(oldStage.IsDisposed, Is.True);
        Assert.That(transform.Transform.Location.x, Is.Zero.Within(POSITION_TOLERANCE));

        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.EqualTo(EXPECTED_FIRST_POSITION).Within(POSITION_TOLERANCE));
    }

    /// <summary>The existing Bullet factory creates a Mover hierarchy associated with its Level's World.</summary>
    [Test]
    public void BulletFactoryCreatesWorldOwnedMoverHierarchy()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        this.world.AddComponent<AoiManagerComponent>();
        Bullet bullet = BulletHelper.CreateBullet(
            this.level, BULLET_CONFIG_ID, BULLET_ID, ENetRole.Authority, ENetRole.SimulatedProxy,
            float3.zero, quaternion.identity,
            new GameplayTargetInfo { TargetDir = moveDirection },
            isRaiseEvent: false);
        MoverComponent component = bullet.GetComponent<MoverComponent>();
        Mover mover = (Mover)component.Children.Values.Single();
        MoverStageRuntime stage = mover.CurrentStage;
        Assert.That(component.GetLevelGameplayWorld(), Is.SameAs(this.world));
        Assert.That(mover.GetLevelGameplayWorld(), Is.SameAs(this.world));
        Assert.That(stage.GetLevelGameplayWorld(), Is.SameAs(this.world));

        // Bullet's legacy collision timer is migrated separately and requires server physics.
        this.root.Scene.GetComponent<TimerComponent>().CancelTimer(ref bullet.LogicUpdateTimer);
        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(bullet.GetComponent<TransformComponent>().Transform.Location.x,
            Is.EqualTo(EXPECTED_FIRST_POSITION).Within(POSITION_TOLERANCE));
    }

    /// <summary>A World-owned Mover runs without a Root Timer component.</summary>
    [Test]
    public void WorldMoverDoesNotRequireRootTimer()
    {
        this.root.Scene.RemoveComponent<TimerComponent>();
        TestCargo cargo = this.world.AddChild<TestCargo>();
        TransformComponent transform = cargo.AddComponent<TransformComponent>();
        MoverComponent component = cargo.AddComponent<MoverComponent>();
        component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo { TargetDir = moveDirection });

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(transform.Transform.Location.x, Is.EqualTo(EXPECTED_FIRST_POSITION).Within(POSITION_TOLERANCE));
    }

    /// <summary>Destroying the World during dispatch stops its remaining Mover callbacks.</summary>
    [Test]
    public void WorldDestructionStopsPendingMovers()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        TestCargo cargo = this.level.AddChild<TestCargo>();
        TransformComponent transform = cargo.AddComponent<TransformComponent>();
        this.world.LogicUpdate.Register(cargo, nameof(WorldDestructionStopsPendingMovers),
            GameplayWorldLogicPriority.MOVEMENT, _ => this.world.Dispose());
        MoverComponent component = cargo.AddComponent<MoverComponent>();
        component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo { TargetDir = moveDirection });

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(component.IsDisposed, Is.True);
        Assert.That(transform.Transform.Location.x, Is.Zero.Within(POSITION_TOLERANCE));
    }

    /// <summary>Physics runs at its World priority before later same-priority participants.</summary>
    [Test]
    public unsafe void PhysicalSimulationRunsAtWorldFinalPriority()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        PxVec3 position = new PxVec3 { y = PHYSICS_INITIAL_HEIGHT };
        PxQuat rotation = new PxQuat { w = PHYSICS_IDENTITY_W };
        PxTransform transform = PxTransform_new_5(&position, &rotation);
        PxRigidDynamic* actor = PxPhysics_createRigidDynamic_mut(physicalWorld.Physics, &transform);
        Assert.That((nint)actor, Is.Not.EqualTo(IntPtr.Zero));
        try
        {
            PxScene_addActor_mut(physicalScene.PxScene, (PxActor*)actor, null);
            physicalScene.StartUpdates();
            physicalScene.StartUpdates();
            TestCargo observerOwner = this.level.AddChild<TestCargo>();
            PhysicalObserver gameplayObserver = new PhysicalObserver((nint)actor);
            PhysicalObserver observer = new PhysicalObserver((nint)actor);
            this.world.LogicUpdate.Register(observerOwner, nameof(gameplayObserver),
                GameplayWorldLogicPriority.MOVEMENT, gameplayObserver.Observe);
            this.world.LogicUpdate.Register(observerOwner, nameof(PhysicalSimulationRunsAtWorldFinalPriority),
                GameplayWorldLogicPriority.PHYSICS, observer.Observe);

            earlyUpdate(this.root);
            setServerTime(FIRST_TICK_TIME);
            update(this.root);
            Assert.That(gameplayObserver.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
            Assert.That(gameplayObserver.Height, Is.EqualTo(PHYSICS_INITIAL_HEIGHT));
            Assert.That(observer.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
            Assert.That(observer.Height, Is.LessThan(PHYSICS_INITIAL_HEIGHT));
            Assert.That(PxRigidBody_getLinearVelocity((PxRigidBody*)actor).y,
                Is.EqualTo(PHYSICS_FIRST_TICK_VELOCITY).Within(POSITION_TOLERANCE));
        }
        finally
        {
            PxRigidActor_release_mut((PxRigidActor*)actor);
        }
    }

    /// <summary>A missing native scene fails the World tick explicitly instead of being silently skipped.</summary>
    [Test]
    public unsafe void MissingNativePhysicalSceneFailsWorldTick()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        this.root.Scene.RemoveComponent<TimerComponent>();
        physicalScene.StartUpdates();
        PxScene* nativeScene = physicalScene.PxScene;
        try
        {
            physicalScene.PxScene = null;
            earlyUpdate(this.root);
            setServerTime(FIRST_TICK_TIME);
            Exception exception = Assert.Catch(() => update(this.root))!;
            Assert.That(exception.ToString(), Does.Contain("The physical scene has no native PhysX scene."));
        }
        finally
        {
            physicalScene.PxScene = nativeScene;
        }
    }

    /// <summary>A physical failure stops its World without losing the Root or other Worlds.</summary>
    [Test]
    public unsafe void PhysicalFailureStopsOnlyItsWorld()
    {
        int previousLogLevel = Log.LogLevel;
        Log.LogLevel = SUPPRESS_INFO_LOG_LEVEL;
        RootManager rootManager = new RootManager();
        rootManager.Init();
        registerSingleton.Invoke(rootManager, null);
        PhysicalScene? physicalScene = null;
        PxScene* nativeScene = null;
        try
        {
            rootManager.Create(FIRST_MANAGED_ROOT_ID, ROOT_ZONE, ELogicSceneType.None, nameof(FIRST_MANAGED_ROOT_ID))
                .GetAwaiter().GetResult();
            Root firstRoot = TestRootInit.LastCreatedRoot!;
            Root child = firstRoot.CreateChild(CHILD_ROOT_ID, ELogicSceneType.None, nameof(CHILD_ROOT_ID));
            rootManager.Create(SECOND_MANAGED_ROOT_ID, ROOT_ZONE, ELogicSceneType.None, nameof(SECOND_MANAGED_ROOT_ID))
                .GetAwaiter().GetResult();
            Root secondRoot = TestRootInit.LastCreatedRoot!;

            GameplayWorld failedWorld = GameplayWorldFactory.Create(child.Scene, WORLD_ID, WORLD_INSTANCE_ID, "failed");
            GameplayWorld healthyWorld = GameplayWorldFactory.Create(
                child.Scene, HEALTHY_WORLD_ID, HEALTHY_WORLD_INSTANCE_ID, "healthy");
            GameplayWorld otherRootWorld = GameplayWorldFactory.Create(
                secondRoot.Scene, WORLD_ID, WORLD_INSTANCE_ID, "other");
            LogicScene level = LevelSceneFactory.Create(
                failedWorld, failedWorld, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
            LevelSceneFactory.Enable(level).GetAwaiter().GetResult();
            PhysicalWorldComponent physicalWorld = child.Scene.AddComponent<PhysicalWorldComponent>();
            physicalScene = level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
            physicalScene.StartUpdates();
            TestLogicEntity failedParticipant = failedWorld.AddChild<TestLogicEntity>();
            TestLogicEntity healthyParticipant = healthyWorld.AddChild<TestLogicEntity>();
            TestLogicEntity otherRootParticipant = otherRootWorld.AddChild<TestLogicEntity>();
            TestWorldUpdateProbe failedProbe = failedWorld.AddChild<TestWorldUpdateProbe>();
            TestWorldUpdateProbe healthyProbe = healthyWorld.AddChild<TestWorldUpdateProbe>();
            TestTimerCounter timerCounter = new TestTimerCounter();
            failedWorld.Timer.ScheduleRepeated(FAULTED_WORLD_TIMER_INTERVAL, timerCounter.Increment);
            failedParticipant.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);
            healthyParticipant.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);
            otherRootParticipant.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);

            nativeScene = physicalScene.PxScene;
            physicalScene.PxScene = null;
            World.Instance.EarlyUpdate();
            World.Instance.EarlyUpdate();
            World.Instance.EarlyUpdate();
            setServerTime(FIRST_TICK_TIME);
            Exception exception = Assert.Catch(World.Instance.Update)!;
            Assert.That(exception.ToString(), Does.Contain("The physical scene has no native PhysX scene."));
            Assert.That(failedWorld.IsFaulted, Is.True);
            Assert.That(failedWorld.Failure!.Message, Is.EqualTo("The physical scene has no native PhysX scene."));
            Assert.That(failedWorld.IsDisposed, Is.False);
            Assert.That(failedParticipant.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
            int failedEarlyCount = failedProbe.EarlyCount;
            TestTimerRegistration registration = new TestTimerRegistration(failedWorld, timerCounter);
            Assert.Throws<InvalidOperationException>(registration.Schedule);

            World.Instance.EarlyUpdate();
            setServerTime(DELAYED_TICK_TIME);
            Assert.DoesNotThrow(World.Instance.Update);
            World.Instance.LateUpdate();
            Assert.That(failedParticipant.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
            Assert.That(timerCounter.Count, Is.Zero);
            Assert.That(failedProbe.EarlyCount, Is.EqualTo(failedEarlyCount));
            Assert.That(failedProbe.UpdateCount, Is.Zero);
            Assert.That(failedProbe.LateCount, Is.Zero);
            Assert.That(healthyProbe.UpdateCount, Is.GreaterThan(NO_REGISTRATIONS));
            Assert.That(healthyProbe.LateCount, Is.GreaterThan(NO_REGISTRATIONS));
            failedWorld.Updater.EarlyUpdate();
            failedWorld.Updater.Update();
            failedWorld.Updater.LateUpdate();
            Assert.That(failedProbe.EarlyCount, Is.EqualTo(failedEarlyCount));
            Assert.That(failedProbe.UpdateCount, Is.Zero);
            Assert.That(failedProbe.LateCount, Is.Zero);
            Assert.That(healthyParticipant.UpdateCount, Is.GreaterThan(NO_REGISTRATIONS));
            Assert.That(otherRootParticipant.UpdateCount, Is.GreaterThan(NO_REGISTRATIONS));

            physicalScene.PxScene = nativeScene;
            World.Instance.EarlyUpdate();
            setServerTime(NEXT_DELAYED_TICK_TIME);
            Assert.DoesNotThrow(World.Instance.Update);
            Assert.That(failedParticipant.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
            Assert.That(timerCounter.Count, Is.Zero);
            Assert.That(failedWorld.IsFaulted, Is.True);
        }
        finally
        {
            if (physicalScene != null)
            {
                physicalScene.PxScene = nativeScene;
            }

            rootManager.Dispose();
            Log.LogLevel = previousLogLevel;
        }
    }

    /// <summary>A stopped physical scene does not advance until it is restarted.</summary>
    [Test]
    public unsafe void StoppedPhysicalSceneCanRestart()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        PxVec3 position = new PxVec3 { y = PHYSICS_INITIAL_HEIGHT };
        PxQuat rotation = new PxQuat { w = PHYSICS_IDENTITY_W };
        PxTransform transform = PxTransform_new_5(&position, &rotation);
        PxRigidDynamic* actor = PxPhysics_createRigidDynamic_mut(physicalWorld.Physics, &transform);
        Assert.That((nint)actor, Is.Not.EqualTo(IntPtr.Zero));
        try
        {
            PxScene_addActor_mut(physicalScene.PxScene, (PxActor*)actor, null);
            physicalScene.StartUpdates();
            physicalScene.StopUpdates();
            earlyUpdate(this.root);
            setServerTime(FIRST_TICK_TIME);
            update(this.root);
            Assert.That(PxRigidActor_getGlobalPose((PxRigidActor*)actor).p.y, Is.EqualTo(PHYSICS_INITIAL_HEIGHT));

            physicalScene.StartUpdates();
            setServerTime(DELAYED_TICK_TIME);
            update(this.root);
            Assert.That(PxRigidActor_getGlobalPose((PxRigidActor*)actor).p.y, Is.LessThan(PHYSICS_INITIAL_HEIGHT));
        }
        finally
        {
            PxRigidActor_release_mut((PxRigidActor*)actor);
        }
    }

    /// <summary>Removing the primary entry directly does not prevent physical simulation restarting.</summary>
    [Test]
    public unsafe void UnregisteredPhysicalSceneCanRestart()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        PxVec3 position = new PxVec3 { y = PHYSICS_INITIAL_HEIGHT };
        PxQuat rotation = new PxQuat { w = PHYSICS_IDENTITY_W };
        PxTransform transform = PxTransform_new_5(&position, &rotation);
        PxRigidDynamic* actor = PxPhysics_createRigidDynamic_mut(physicalWorld.Physics, &transform);
        Assert.That((nint)actor, Is.Not.EqualTo(IntPtr.Zero));
        try
        {
            PxScene_addActor_mut(physicalScene.PxScene, (PxActor*)actor, null);
            physicalScene.StartUpdates();
            physicalScene.UnregisterLogicUpdate();
            physicalScene.StartUpdates();
            earlyUpdate(this.root);
            setServerTime(FIRST_TICK_TIME);
            update(this.root);
            Assert.That(PxRigidActor_getGlobalPose((PxRigidActor*)actor).p.y, Is.LessThan(PHYSICS_INITIAL_HEIGHT));
        }
        finally
        {
            PxRigidActor_release_mut((PxRigidActor*)actor);
        }
    }

    /// <summary>A World-level removal of the primary entry does not prevent physical simulation restarting.</summary>
    [Test]
    public unsafe void WorldUnregistrationDoesNotBlockPhysicalRestart()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        PxVec3 position = new PxVec3 { y = PHYSICS_INITIAL_HEIGHT };
        PxQuat rotation = new PxQuat { w = PHYSICS_IDENTITY_W };
        PxTransform transform = PxTransform_new_5(&position, &rotation);
        PxRigidDynamic* actor = PxPhysics_createRigidDynamic_mut(physicalWorld.Physics, &transform);
        Assert.That((nint)actor, Is.Not.EqualTo(IntPtr.Zero));
        try
        {
            PxScene_addActor_mut(physicalScene.PxScene, (PxActor*)actor, null);
            physicalScene.StartUpdates();
            Assert.That(this.world.LogicUpdate.Unregister(physicalScene, physicalScene.GetType().FullName!), Is.True);
            physicalScene.StartUpdates();
            earlyUpdate(this.root);
            setServerTime(FIRST_TICK_TIME);
            update(this.root);
            Assert.That(PxRigidActor_getGlobalPose((PxRigidActor*)actor).p.y, Is.LessThan(PHYSICS_INITIAL_HEIGHT));
        }
        finally
        {
            PxRigidActor_release_mut((PxRigidActor*)actor);
        }
    }

    /// <summary>A different primary callback cannot be mistaken for physical simulation.</summary>
    [Test]
    public void PhysicalSceneRejectsAnotherPrimaryCallback()
    {
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        physicalScene.RegisterLogicUpdate(GameplayWorldLogicPriority.PHYSICS, recordLogicCallback);

        Assert.Throws<InvalidOperationException>(physicalScene.StartUpdates);
        physicalScene.StopUpdates();
        Assert.That(physicalScene.HasLogicUpdate(recordLogicCallback), Is.True);
    }

    /// <summary>A replacement World entry is not removed by stale primary-entry tracking.</summary>
    [Test]
    public void PhysicalSceneDoesNotRemoveReplacedWorldEntry()
    {
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        physicalScene.StartUpdates();
        string entryName = physicalScene.GetType().FullName!;
        Assert.That(this.world.LogicUpdate.Unregister(physicalScene, entryName), Is.True);
        this.world.LogicUpdate.Register(physicalScene, entryName, GameplayWorldLogicPriority.PHYSICS, recordLogicCallback);

        physicalScene.StopUpdates();
        physicalScene.UnregisterLogicUpdate();
        Assert.That(this.world.LogicUpdate.Unregister(physicalScene, entryName), Is.True);
    }

    /// <summary>Prepared levels stay frozen and delayed physics ticks use one fixed 50 ms step.</summary>
    [Test]
    public unsafe void PreparedPhysicalSceneWaitsAndDelayedTicksDoNotCatchUp()
    {
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        PxVec3 position = new PxVec3 { y = PHYSICS_INITIAL_HEIGHT };
        PxQuat rotation = new PxQuat { w = PHYSICS_IDENTITY_W };
        PxTransform transform = PxTransform_new_5(&position, &rotation);
        PxRigidDynamic* actor = PxPhysics_createRigidDynamic_mut(physicalWorld.Physics, &transform);
        Assert.That((nint)actor, Is.Not.EqualTo(IntPtr.Zero));
        try
        {
            PxScene_addActor_mut(physicalScene.PxScene, (PxActor*)actor, null);
            physicalScene.StartUpdates();
            earlyUpdate(this.root);
            setServerTime(FIRST_TICK_TIME);
            update(this.root);
            Assert.That(PxRigidBody_getLinearVelocity((PxRigidBody*)actor).y,
                Is.Zero.Within(POSITION_TOLERANCE));

            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            setServerTime(DELAYED_TICK_TIME);
            update(this.root);
            update(this.root);
            Assert.That(PxRigidBody_getLinearVelocity((PxRigidBody*)actor).y,
                Is.EqualTo(PHYSICS_FIRST_TICK_VELOCITY).Within(POSITION_TOLERANCE));

            setServerTime(NEXT_DELAYED_TICK_TIME);
            update(this.root);
            Assert.That(PxRigidBody_getLinearVelocity((PxRigidBody*)actor).y,
                Is.EqualTo(PHYSICS_SECOND_TICK_VELOCITY).Within(POSITION_TOLERANCE));
        }
        finally
        {
            PxRigidActor_release_mut((PxRigidActor*)actor);
        }
    }

    /// <summary>The actual Kinematic manager and Mover follow their shared registration order.</summary>
    [TestCase(true, INITIAL_POSITION)]
    [TestCase(false, EXPECTED_FIRST_POSITION)]
    public void KinematicManagerAndMoverRunInRegistrationOrder(bool managerFirst, float observedPosition)
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        if (managerFirst)
        {
            this.createKinematicManager();
        }

        TestCargo cargo = this.level.AddChild<TestCargo>();
        TransformComponent cargoTransform = cargo.AddComponent<TransformComponent>();
        MoverComponent component = cargo.AddComponent<MoverComponent>();
        component.LaunchMover(MOVER_CONFIG_ID, new GameplayTargetInfo { TargetDir = moveDirection });

        if (!managerFirst)
        {
            this.createKinematicManager();
        }

        TestCargo platform = this.level.AddChild<TestCargo>();
        TransformComponent platformTransform = platform.AddComponent<TransformComponent>();
        platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover platformMover = platform.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController controller = new ObservingMoverController(cargoTransform);
        platformMover.MoverController = controller;

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);

        Assert.That(controller.ObservedPosition, Is.EqualTo(observedPosition).Within(POSITION_TOLERANCE));
        Assert.That(controller.ObservedDelta, Is.EqualTo(EXPECTED_DELTA_SECONDS).Within(POSITION_TOLERANCE));
        Assert.That(controller.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        Assert.That(platformTransform.Transform.Location.x, Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
        Assert.That(cargoTransform.Transform.Location.x, Is.EqualTo(EXPECTED_FIRST_POSITION).Within(POSITION_TOLERANCE));

        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        update(this.root);
        Assert.That(controller.ObservedDelta, Is.EqualTo(EXPECTED_DELTA_SECONDS).Within(POSITION_TOLERANCE));
        Assert.That(controller.UpdateCount, Is.EqualTo(SECOND_SIMULATION_COUNT));
        Assert.That(cargoTransform.Transform.Location.x, Is.EqualTo(EXPECTED_SECOND_POSITION).Within(POSITION_TOLERANCE));
    }

    /// <summary>Removing a manager during dispatch prevents its pending simulation.</summary>
    [Test]
    public void DestroyedKinematicManagerDoesNotSimulate()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        TestCargo cargo = this.level.AddChild<TestCargo>();
        TransformComponent cargoTransform = cargo.AddComponent<TransformComponent>();
        KinematicMoverManager? manager = null;
        this.world.LogicUpdate.Register(cargo, nameof(DestroyedKinematicManagerDoesNotSimulate),
            GameplayWorldLogicPriority.MOVEMENT, _ => manager!.Dispose());
        manager = this.createKinematicManager();
        TestCargo platform = this.level.AddChild<TestCargo>();
        platform.AddComponent<TransformComponent>();
        platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover platformMover = platform.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController controller = new ObservingMoverController(cargoTransform);
        platformMover.MoverController = controller;

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(manager.IsDisposed, Is.True);
        Assert.That(controller.ObservedPosition, Is.EqualTo(UNOBSERVED_POSITION));

        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(controller.ObservedPosition, Is.EqualTo(UNOBSERVED_POSITION));
    }

    /// <summary>Kinematic simulation respects its enable settings without requiring a Root Timer.</summary>
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void KinematicSimulationRespectsEnableSettings(bool autoSimulation, bool enabled)
    {
        this.root.Scene.RemoveComponent<TimerComponent>();
        KinematicMoverManager manager = this.world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
            new KinematicMoverManagerSettings { AutoSimulation = autoSimulation });
        manager.Enable = enabled;
        TestCargo platform = this.world.AddChild<TestCargo>();
        TransformComponent transform = platform.AddComponent<TransformComponent>();
        platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover platformMover = platform.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController controller = new ObservingMoverController(transform);
        platformMover.MoverController = controller;

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(controller.UpdateCount, Is.Zero);

        manager.Settings.AutoSimulation = true;
        manager.Enable = true;
        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(controller.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        Assert.That(controller.ObservedDelta, Is.EqualTo(EXPECTED_DELTA_SECONDS).Within(POSITION_TOLERANCE));
    }

    /// <summary>World LateUpdate continues to interpolate Kinematic movers after a fixed tick.</summary>
    [Test]
    public void WorldLateUpdateRunsKinematicInterpolation()
    {
        KinematicMoverManager manager = this.world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
            new KinematicMoverManagerSettings
            {
                AutoSimulation = true,
                Interpolate = true,
                InterpolateMover = true,
            });
        manager.Enable = true;
        TestCargo platform = this.world.AddChild<TestCargo>();
        TransformComponent transform = platform.AddComponent<TransformComponent>();
        platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover mover = platform.AddComponent<KinematicRigidbodyMover>();
        mover.MoverController = new ObservingMoverController(transform);

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        manager.LastCustomInterpolationStartTime = TimeInfo.Instance.ClientNow() - ELAPSED_INTERPOLATION_MILLISECONDS;
        lateUpdate(this.root);

        Assert.That(mover.LatestInterpolationPosition.x, Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
    }

    /// <summary>Kinematic simulation and interpolation wait for their LevelScene to be enabled.</summary>
    [Test]
    public void PreparedLevelKinematicManagerWaitsUntilEnabled()
    {
        KinematicMoverManager manager = this.level.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
            new KinematicMoverManagerSettings
            {
                AutoSimulation = true,
                Interpolate = true,
                InterpolateMover = true,
            });
        manager.Enable = true;
        TestCargo platform = this.level.AddChild<TestCargo>();
        TransformComponent transform = platform.AddComponent<TransformComponent>();
        platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover mover = platform.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController controller = new ObservingMoverController(transform);
        mover.MoverController = controller;
        mover.TransientPosition = moveDirection;
        manager.RegisterPhysicsMover(mover);
        manager.LastCustomInterpolationStartTime = TimeInfo.Instance.ClientNow() - ELAPSED_INTERPOLATION_MILLISECONDS;
        manager.LastCustomInterpolationDeltaTime = EXPECTED_DELTA_SECONDS;

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        lateUpdate(this.root);
        Assert.That(controller.UpdateCount, Is.Zero);
        Assert.That(mover.LatestInterpolationPosition.x, Is.Zero.Within(POSITION_TOLERANCE));

        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(controller.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
    }

    /// <summary>A shared static callback receives each live owner and the fixed delta.</summary>
    [Test]
    public void PrimaryLogicUpdatePassesOwnerToSharedStaticCallback()
    {
        TestLogicEntity first = this.world.AddChild<TestLogicEntity>();
        TestLogicEntity second = this.level.AddChild<TestLogicEntity>();
        first.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);
        second.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);

        Assert.That(first.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        Assert.That(second.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        Assert.That(first.ObservedDelta, Is.EqualTo(EXPECTED_DELTA_SECONDS));
        Assert.That(second.ObservedDelta, Is.EqualTo(EXPECTED_DELTA_SECONDS));
    }

    /// <summary>Invalid or duplicate registration cannot replace the existing primary callback.</summary>
    [Test]
    public void PrimaryLogicUpdateRejectsNullAndDuplicateCallbacks()
    {
        TestLogicEntity entity = this.world.AddChild<TestLogicEntity>();
        Assert.Throws<ArgumentNullException>(() =>
            entity.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, null!));
        entity.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);
        Assert.Throws<InvalidOperationException>(() =>
            entity.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback));
        Assert.That(activeRegistrationCount(this.world), Is.EqualTo(SINGLE_REGISTRATION));
        entity.Dispose();
        Assert.Throws<ObjectDisposedException>(() =>
            entity.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback));
    }

    /// <summary>Movement entities declare no behavior and their systems cache noncapturing callbacks.</summary>
    [TestCase(typeof(MoverComponent), typeof(MoverComponentSystem))]
    [TestCase(typeof(KinematicMoverManager), typeof(KinematicMoverManagerSystems))]
    public void MovementEntitiesKeepBehaviorInSystems(Type entityType, Type systemType)
    {
        Assert.That(entityType.GetMethods(BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly), Is.Empty);
        FieldInfo callbackField = systemType.GetField(
            "logicUpdateCallback", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The System callback cache was not found.");
        Delegate callback = (Delegate)callbackField.GetValue(null)!;
        Assert.That(callbackField.IsInitOnly, Is.True);
        Assert.That(callback.Target, Is.Null);
        Assert.That(callback.Method.IsStatic, Is.True);
    }

    /// <summary>Business entities rely on the base lifecycle to remove registrations immediately.</summary>
    [Test]
    public void BaseDisposalImmediatelyUnregistersMoverAndKinematicManager()
    {
        TestCargo cargo = this.world.AddChild<TestCargo>();
        MoverComponent mover = cargo.AddComponent<MoverComponent>();
        KinematicMoverManager manager = this.createKinematicManager();
        Assert.That(activeRegistrationCount(this.world), Is.EqualTo(TWO_REGISTRATIONS));

        mover.Dispose();
        mover.Dispose();
        Assert.That(activeRegistrationCount(this.world), Is.EqualTo(SINGLE_REGISTRATION));
        manager.Dispose();
        manager.Dispose();
        Assert.That(activeRegistrationCount(this.world), Is.EqualTo(NO_REGISTRATIONS));
    }

    /// <summary>Pooled entries switch between explicit-owner and legacy callbacks without retaining the old callback.</summary>
    [Test]
    public void RecycledEntriesSupportBothCallbackForms()
    {
        TestLogicEntity first = this.world.AddChild<TestLogicEntity>();
        first.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);
        first.Dispose();

        TestCargo legacyOwner = this.world.AddChild<TestCargo>();
        List<float> legacyDeltas = new List<float>();
        this.world.LogicUpdate.Register(legacyOwner, nameof(legacyDeltas),
            GameplayWorldLogicPriority.MOVEMENT, legacyDeltas.Add);
        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(legacyDeltas, Is.EqualTo(new[] { EXPECTED_DELTA_SECONDS }));
        Assert.That(this.world.LogicUpdate.Unregister(legacyOwner, nameof(legacyDeltas)), Is.True);

        TestLogicEntity second = this.world.AddChild<TestLogicEntity>();
        second.RegisterLogicUpdate(GameplayWorldLogicPriority.MOVEMENT, recordLogicCallback);
        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(second.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        Assert.That(second.ObservedDelta, Is.EqualTo(EXPECTED_DELTA_SECONDS));
        Assert.That(legacyDeltas.Count, Is.EqualTo(FIRST_SIMULATION_COUNT));
    }

    private static void recordLogicUpdate(Entity entity, float deltaTime)
    {
        TestLogicEntity participant = (TestLogicEntity)entity;
        participant.UpdateCount++;
        participant.ObservedDelta = deltaTime;
    }

    private static int activeRegistrationCount(GameplayWorld world)
    {
        FieldInfo entries = typeof(GameplayWorldLogicUpdate)
            .GetField("entriesByKey", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The scheduler registration dictionary was not found.");
        return ((System.Collections.IDictionary)entries.GetValue(world.LogicUpdate)!).Count;
    }

    private KinematicMoverManager createKinematicManager()
    {
        KinematicMoverManager manager = this.world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
            new KinematicMoverManagerSettings { AutoSimulation = true });
        manager.Enable = true;
        return manager;
    }

    private static void setServerTime(long time) =>
        TimeInfo.Instance.ServerMinusClientTime = time - TimeInfo.Instance.FrameTime;

    /// <summary>Ordinary cargo representing the current Bullet ownership path.</summary>
    public sealed class TestCargo : Entity
    {
    }

    /// <summary>Initializes test Roots without production network resources.</summary>
    [Invoke((long)ELogicSceneType.None)]
    public sealed class TestRootInit : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        /// <summary>Most recently initialized test Root.</summary>
        public static Root? LastCreatedRoot { get; private set; }

        /// <inheritdoc />
        public override async ETTask Handle(Invoke_RootInit args)
        {
            LastCreatedRoot = args.Root;
            await ETTask.CompletedTask;
        }
    }

    private sealed class TestTimerCounter
    {
        internal int Count { get; private set; }

        internal void Increment() => this.Count++;
    }

    private sealed class TestTimerRegistration
    {
        private readonly GameplayWorld world;
        private readonly TestTimerCounter counter;

        internal TestTimerRegistration(GameplayWorld world, TestTimerCounter counter)
        {
            this.world = world;
            this.counter = counter;
        }

        internal void Schedule() => this.world.Timer.ScheduleRepeated(FAULTED_WORLD_TIMER_INTERVAL, this.counter.Increment);
    }

    /// <summary>Data-only participant exercising the shared gameplay entity lifecycle.</summary>
    public sealed class TestLogicEntity : GamePlayEntity
    {
        /// <summary>Number of fixed updates received by this participant.</summary>
        public int UpdateCount;

        /// <summary>Delta received by this participant's most recent fixed update.</summary>
        public float ObservedDelta;
    }

    /// <summary>Observes ordinary World update phases.</summary>
    public sealed class TestWorldUpdateProbe : GamePlayEntity, IEarlyUpdate, IUpdate, ILateUpdate
    {
        /// <summary>Number of EarlyUpdate calls.</summary>
        public int EarlyCount;

        /// <summary>Number of Update calls.</summary>
        public int UpdateCount;

        /// <summary>Number of LateUpdate calls.</summary>
        public int LateCount;
    }

    /// <summary>Records the probe's EarlyUpdate calls.</summary>
    [EntitySystem]
    public sealed class TestWorldEarlyProbeSystem : EarlyUpdateSystem<TestWorldUpdateProbe>
    {
        /// <inheritdoc />
        protected override void EarlyUpdate(TestWorldUpdateProbe entity) => entity.EarlyCount++;
    }

    /// <summary>Records the probe's Update calls.</summary>
    [EntitySystem]
    public sealed class TestWorldUpdateProbeSystem : UpdateSystem<TestWorldUpdateProbe>
    {
        /// <inheritdoc />
        protected override void Update(TestWorldUpdateProbe entity) => entity.UpdateCount++;
    }

    /// <summary>Records the probe's LateUpdate calls.</summary>
    [EntitySystem]
    public sealed class TestWorldLateProbeSystem : LateUpdateSystem<TestWorldUpdateProbe>
    {
        /// <inheritdoc />
        protected override void LateUpdate(TestWorldUpdateProbe entity) => entity.LateCount++;
    }

    private sealed class PhysicalObserver
    {
        private readonly nint actor;

        internal PhysicalObserver(nint actor)
        {
            this.actor = actor;
        }

        internal float Height { get; private set; } = PHYSICS_INITIAL_HEIGHT;
        internal int UpdateCount { get; private set; }

        internal unsafe void Observe(float deltaTime)
        {
            this.UpdateCount++;
            this.Height = PxRigidActor_getGlobalPose((PxRigidActor*)this.actor).p.y;
        }
    }

    /// <summary>Observes cargo while supplying a deterministic Kinematic platform goal.</summary>
    public sealed class ObservingMoverController : IMoverController
    {
        private readonly TransformComponent cargo;

        /// <summary>Creates a controller observing the supplied cargo.</summary>
        /// <param name="cargo">The transform to observe during platform simulation.</param>
        public ObservingMoverController(TransformComponent cargo)
        {
            this.cargo = cargo;
        }

        /// <summary>The cargo position observed in the last simulation.</summary>
        public float ObservedPosition { get; private set; } = UNOBSERVED_POSITION;

        /// <summary>The delta observed in the last simulation.</summary>
        public float ObservedDelta { get; private set; }

        /// <summary>The number of completed controller updates.</summary>
        public int UpdateCount { get; private set; }

        /// <inheritdoc />
        public void UpdateMovement(out float3 goalPosition, out quaternion goalRotation, float deltaTime)
        {
            this.ObservedPosition = this.cargo.Transform.Location.x;
            this.ObservedDelta = deltaTime;
            this.UpdateCount++;
            goalPosition = moveDirection;
            goalRotation = quaternion.identity;
        }
    }

    /// <summary>Provides a LevelScene preparation boundary without external physical assets.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class PrepareLevel : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        protected override async ETTask Run(LogicScene scene, Event_AfterCreateLevelScene data) =>
            await ETTask.CompletedTask;
    }

    /// <summary>Provides the LevelScene enable boundary for the isolated fixture.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class EnableLevel : AEvent<LogicScene, Event_EnableLevelScene>
    {
        protected override async ETTask Run(LogicScene scene, Event_EnableLevelScene data) =>
            await ETTask.CompletedTask;
    }
}
