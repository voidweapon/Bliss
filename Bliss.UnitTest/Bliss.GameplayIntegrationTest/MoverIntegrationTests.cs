using System.Reflection;
using Bliss;
using Bliss.CharacterControllers;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Gameplay.GameplayAbilitySystem;
using Bliss.Physics;
using Bliss.Server;
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
    private const int UNIT_CONFIG_ID = 1;
    private const long TEST_UNIT_ID = 12001;
    private const long TEST_SCENE_ACTOR_ID = 12002;
    private const long FIRST_SHOOT_TIME = 11000;
    private const long SECOND_SHOOT_TIME = 21000;
    private const long THIRD_SHOOT_TIME = 31000;
    private const long BEFORE_FIRST_SHOOT_TIME = 10999;
    private const long UNUPDATED_VIEW_TIME = -1;
    private const int BULLET_CONFIG_ID = 1;
    private const long BULLET_ID = 404;
    private const long INITIAL_TIME = 1000;
    private const long AFTER_JUMP_GRACE_TIME = 1200;
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
    private const float NETWORK_MOVE_TIME = 0.1f;
    private const float RESIZED_CAPSULE_RADIUS = 0.75f;
    private const float RESIZED_CAPSULE_HEIGHT = 3f;
    private const float INITIAL_CAPSULE_HEIGHT = 2f;
    private const float RAYCAST_DISTANCE = 10f;
    private const float RAYCAST_ORIGIN_X = -5f;
    private const int FIRST_SIMULATION_COUNT = 1;
    private const int FIRST_INDEX = 0;
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
    private static readonly PropertyInfo frameTime =
        typeof(TimeInfo).GetProperty(nameof(TimeInfo.FrameTime))
        ?? throw new InvalidOperationException("TimeInfo.FrameTime was not found.");
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
        this.typeManager.GetTypes(typeof(EventAttribute)).Remove(typeof(GameplayEventHandler_ShootBullet_Server));
        registerSingleton.Invoke(this.typeManager, null);
        MongoRegister.Init();
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);
        RecordShot.Count = 0;
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

    /// <summary>A minimally created Unit and its dedicated layer belong to the owning World.</summary>
    [Test]
    public void MinimalLevelUnitUsesGameplayWorldOwnership()
    {
        UnitManager manager = this.world.AddComponent<UnitManager>();
        Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.Authority, ENetRole.SimulatedProxy, float3.zero, quaternion.identity);
        GameplayLayer layer = unit.GetComponent<GameplayLayer>();
        Assert.That(typeof(GamePlayEntity).IsInstanceOfType(unit), Is.True);
        Assert.That(typeof(GamePlayEntity).IsInstanceOfType(layer), Is.True);
        Assert.That(unit.GetLevelGameplayWorld(), Is.SameAs(this.world));
        Assert.That(layer.GetLevelGameplayWorld(), Is.SameAs(this.world));
        Assert.That(manager.DicEntityId2Unit.ContainsKey(TEST_UNIT_ID), Is.True);
        Assert.That(typeof(GamePlayEntity).IsInstanceOfType(unit.GetComponent<TransformComponent>()), Is.False);
        Assert.That(typeof(GamePlayEntity).IsInstanceOfType(unit.GetComponent<NetRoleComponent>()), Is.False);
        unit.Dispose();
        Assert.That(unit.IsDisposed, Is.True);
        Assert.That(layer.IsDisposed, Is.True);
        Assert.That(manager.DicEntityId2Unit, Is.Empty);
    }

    /// <summary>A dedicated movement component advances once per World tick without a Controller update.</summary>
    [Test]
    public void CharacterMovementUsesWorldFixedTicks()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.SimulatedProxy, ENetRole.Authority, float3.zero, quaternion.identity);
        CharacterMovementComponent movement = unit.AddComponent<CharacterMovementComponent>();
        Assert.That(typeof(GamePlayEntity).IsInstanceOfType(movement), Is.True);

        earlyUpdate(this.root);
        update(this.root);
        Assert.That(movement.LogicTimeStamp, Is.Zero);

        setServerTime(FIRST_TICK_TIME);
        earlyUpdate(this.root);
        update(this.root);
        Assert.That(movement.LogicTimeStamp, Is.EqualTo(GameplayWorldLogicUpdate.STEP_MILLISECONDS));

        update(this.root);
        Assert.That(movement.LogicTimeStamp, Is.EqualTo(GameplayWorldLogicUpdate.STEP_MILLISECONDS));
        unit.Dispose();
        setServerTime(DELAYED_TICK_TIME);
        earlyUpdate(this.root);
        update(this.root);
        Assert.That(movement.IsDisposed, Is.True);
    }

    /// <summary>Input is collected independently each frame and retains a buffered jump until the tick.</summary>
    [Test]
    public void InputPipelineSamplesEachEarlyUpdateAndRetainsJump()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.AutonomousProxy, ENetRole.Authority, float3.zero, quaternion.identity);
        InputIntentProcessPipeline input = unit.AddComponent<InputIntentProcessPipeline>();
        CharacterRuntimeData runtimeData = new CharacterRuntimeData();
        TestInputSource inputSource = new TestInputSource();
        input.SetRuntimeData(runtimeData);
        input.SetInputSource(inputSource);

        earlyUpdate(this.root);
        inputSource.PressJump = false;
        earlyUpdate(this.root);
        Assert.That(inputSource.MovementReadCount, Is.EqualTo(SECOND_SIMULATION_COUNT));
        Assert.That(runtimeData.IntentMovement.x, Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
        Assert.That(runtimeData.WantsToJump, Is.True);

        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(runtimeData.WantsToJump, Is.True);
    }

    /// <summary>A jump edge survives elapsed grace time until the next due World tick.</summary>
    [Test]
    public void JumpInputSurvivesUntilNextWorldTick()
    {
        long previousFrameTime = TimeInfo.Instance.FrameTime;
        try
        {
            frameTime.SetValue(TimeInfo.Instance, INITIAL_TIME);
            setServerTime(INITIAL_TIME);
            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
                ENetRole.AutonomousProxy, ENetRole.Authority, float3.zero, quaternion.identity);
            InputIntentProcessPipeline input = unit.AddComponent<InputIntentProcessPipeline>();
            CharacterRuntimeData runtimeData = new CharacterRuntimeData();
            TestInputSource inputSource = new TestInputSource();
            input.SetRuntimeData(runtimeData);
            input.SetInputSource(inputSource);
            earlyUpdate(this.root);

            inputSource.PressJump = false;
            frameTime.SetValue(TimeInfo.Instance, AFTER_JUMP_GRACE_TIME);
            earlyUpdate(this.root);
            Assert.That(runtimeData.WantsToJump, Is.True);
            update(this.root);
            Assert.That(runtimeData.WantsToJump, Is.True);
            earlyUpdate(this.root);
            Assert.That(runtimeData.WantsToJump, Is.False);
        }
        finally
        {
            frameTime.SetValue(TimeInfo.Instance, previousFrameTime);
        }
    }

    /// <summary>A jump down followed by cancellation in one sample is retained for the next tick.</summary>
    [Test]
    public void JumpEdgeIsRetainedWhenLaterInputRecordCancelsIt()
    {
        long previousFrameTime = TimeInfo.Instance.FrameTime;
        try
        {
            frameTime.SetValue(TimeInfo.Instance, INITIAL_TIME);
            setServerTime(INITIAL_TIME);
            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
                ENetRole.AutonomousProxy, ENetRole.Authority, float3.zero, quaternion.identity);
            InputIntentProcessPipeline input = unit.AddComponent<InputIntentProcessPipeline>();
            CharacterRuntimeData data = new CharacterRuntimeData { JumpConsumed = true };
            TestInputSource source = new TestInputSource { CancelAfterJump = true };
            input.SetRuntimeData(data);
            input.SetInputSource(source);
            earlyUpdate(this.root);
            Assert.That(data.JumpConsumed, Is.False);
            source.PressJump = false;
            frameTime.SetValue(TimeInfo.Instance, AFTER_JUMP_GRACE_TIME);
            earlyUpdate(this.root);
            Assert.That(data.WantsToJump, Is.True);
            update(this.root);
            Assert.That(data.WantsToJump, Is.True);
        }
        finally
        {
            frameTime.SetValue(TimeInfo.Instance, previousFrameTime);
        }
    }

    /// <summary>The server message handler applies an accepted move immediately, without a World tick.</summary>
    [Test]
    public void ServerMoveMessageAppliesBeforeNextWorldTick()
    {
        IGameplayPhysicsHandler previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        try
        {
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();
            PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
            this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
                ENetRole.Authority, ENetRole.AutonomousProxy, float3.zero, quaternion.identity);
            KinematicCharacterComponent motor = unit.AddComponent<KinematicCharacterComponent>();
            CharacterMovementComponent movement = unit.AddComponent<CharacterMovementComponent>();
            movement.KinematicCharacterCmp = motor;
            CharacterNetworkMoveData move = new CharacterNetworkMoveData { TimeStamp = NETWORK_MOVE_TIME };
            move.Location.Value = moveDirection;
            C2M_CharacterMoveData message = C2M_CharacterMoveData.Create();
            message.MoveData.Add(move);

            invokeServerMove(unit, message);

            Assert.That(unit.GetComponent<TransformComponent>().Transform.Location.x,
                Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
            Assert.That(motor.TransientPosition.x, Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
            Assert.That(movement.LogicTimeStamp, Is.Zero);
            Assert.That(movement.ServerData.CurrentClientTimeStamp,
                Is.EqualTo(NETWORK_MOVE_TIME).Within(POSITION_TOLERANCE));
        }
        finally
        {
            GameplayPhysics.GameplayPhysicsHandler = previousPhysics;
        }
    }

    /// <summary>A Level character owns real PhysX resources that are unregistered when the Unit is destroyed.</summary>
    [Test]
    public unsafe void ServerLevelCharacterOwnsNativePhysics()
    {
        IGameplayPhysicsHandler previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        try
        {
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();
            PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
            PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
                ENetRole.Authority, ENetRole.AutonomousProxy, float3.zero, quaternion.identity);
            CharacterController controller = unit.AddComponent<CharacterController>();
            GameplayRigidbody body = unit.GetComponent<GameplayRigidbody>();
            GameplayCapsuleCollider capsule = controller.KinematicCharacterComponent.Capsule;
            Assert.That((IntPtr)body.RigidBodyPtr, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That((IntPtr)capsule.ShapePtr, Is.Not.EqualTo(IntPtr.Zero));
            Assert.That(capsule.AttachedRigidbody, Is.SameAs(body));
            Assert.That(physicalScene.RigidbodyDict.GetValueByKey((IntPtr)body.RigidBodyPtr), Is.SameAs(body));
            Assert.That(physicalScene.ColliderDict.GetValueByKey((IntPtr)capsule.ShapePtr), Is.SameAs(capsule));
            Assert.That(((PxRigidActor*)body.RigidBodyPtr)->GetNbShapes(), Is.EqualTo(SINGLE_REGISTRATION));
            Assert.That((PxRigidBody_getRigidBodyFlags(body.RigidBodyPtr) & PxRigidBodyFlags.Kinematic),
                Is.EqualTo(PxRigidBodyFlags.Kinematic));
            float3 capsuleAxis = math.mul(PxShape_getLocalPose(capsule.ShapePtr).q.ToQuaternion(),
                new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS));
            Assert.That(math.abs(capsuleAxis.y), Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
            Assert.That(physicalScene.Raycast(new float3(RAYCAST_ORIGIN_X, UNIT_AXIS, ZERO_AXIS),
                moveDirection, RAYCAST_DISTANCE, GameplayLayerMask.UnitMask, out GameplayRaycastHit hit), Is.True);
            Assert.That((GameplayCollider)hit.Collider, Is.SameAs(capsule));

            capsule.Radius = RESIZED_CAPSULE_RADIUS;
            PxCapsuleGeometry radiusGeometry = *capsule.CapsuleGeometry;
            Assert.That(radiusGeometry.radius, Is.EqualTo(RESIZED_CAPSULE_RADIUS).Within(POSITION_TOLERANCE));
            Assert.That(PxTypeHelper.GetUnitCapsuleHeight(radiusGeometry.halfHeight, radiusGeometry.radius),
                Is.EqualTo(INITIAL_CAPSULE_HEIGHT).Within(POSITION_TOLERANCE));
            capsule.Height = RESIZED_CAPSULE_HEIGHT;
            PxCapsuleGeometry heightGeometry = *capsule.CapsuleGeometry;
            Assert.That(PxTypeHelper.GetUnitCapsuleHeight(heightGeometry.halfHeight, heightGeometry.radius),
                Is.EqualTo(RESIZED_CAPSULE_HEIGHT).Within(POSITION_TOLERANCE));

            CharacterNetworkMoveData move = new CharacterNetworkMoveData { TimeStamp = NETWORK_MOVE_TIME };
            move.Location.Value = moveDirection;
            C2M_CharacterMoveData message = C2M_CharacterMoveData.Create();
            message.MoveData.Add(move);
            invokeServerMove(unit, message);
            Assert.That(PxRigidActor_getGlobalPose((PxRigidActor*)body.RigidBodyPtr).p.x,
                Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));

            IntPtr bodyPointer = (IntPtr)body.RigidBodyPtr;
            IntPtr shapePointer = (IntPtr)capsule.ShapePtr;
            unit.Dispose();
            Assert.That(physicalScene.RigidbodyDict.ContainsKey(bodyPointer), Is.False);
            Assert.That(physicalScene.ColliderDict.ContainsKey(shapePointer), Is.False);
        }
        finally
        {
            GameplayPhysics.GameplayPhysicsHandler = previousPhysics;
        }
    }

    /// <summary>A missing physical scene leaves no partially initialized controller or scheduled input.</summary>
    [Test]
    public void MissingPhysicalSceneRollsBackCharacterInitialization()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.Authority, ENetRole.AutonomousProxy, float3.zero, quaternion.identity);
        Assert.Throws<InvalidOperationException>(() => unit.AddComponent<CharacterController>());
        Assert.That(unit.GetComponent<CharacterController>(), Is.Null);
        Assert.That(unit.GetComponent<InputIntentProcessPipeline>(), Is.Null);
        Assert.That(unit.GetComponent<GameplayRigidbody>(), Is.Null);
        Assert.That(unit.GetComponent<KinematicCharacterComponent>(), Is.Null);
        Assert.That(unit.GetComponent<CharacterMovementComponent>(), Is.Null);
    }

    /// <summary>A failure after native creation releases new resources without removing an existing motor.</summary>
    [Test]
    public void CharacterInitializationFailurePreservesExistingMotorAndReleasesPhysics()
    {
        IGameplayPhysicsHandler previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        try
        {
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();
            PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
            PhysicalScene physicalScene = this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
                ENetRole.Authority, ENetRole.AutonomousProxy, float3.zero, quaternion.identity);
            KinematicCharacterComponent existingMotor = unit.AddComponent<KinematicCharacterComponent>();

            Assert.Throws<Exception>(() => unit.AddComponent<CharacterController>());
            Assert.That(unit.GetComponent<CharacterController>(), Is.Null);
            Assert.That(unit.GetComponent<InputIntentProcessPipeline>(), Is.Null);
            Assert.That(unit.GetComponent<GameplayRigidbody>(), Is.Null);
            Assert.That(unit.GetComponent<KinematicCharacterComponent>(), Is.SameAs(existingMotor));
            Assert.That(physicalScene.RigidbodyDict.Keys, Is.Empty);
            Assert.That(physicalScene.ColliderDict.Keys, Is.Empty);
        }
        finally
        {
            GameplayPhysics.GameplayPhysicsHandler = previousPhysics;
        }
    }

    /// <summary>A World-driven server motor keeps its real PhysX body at its simulated position.</summary>
    [Test]
    public unsafe void ServerWorldMotorSynchronizesNativeBody()
    {
        IGameplayPhysicsHandler previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        try
        {
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();
            PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
            this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
            KinematicMoverManager manager = this.world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
                new KinematicMoverManagerSettings { AutoSimulation = true });
            manager.Enable = true;
            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
                ENetRole.Authority, ENetRole.Authority, float3.zero, quaternion.identity);
            CharacterController controller = unit.AddComponent<CharacterController>();
            KinematicCharacterComponent motor = controller.KinematicCharacterComponent;
            motor._solveMovementCollisions = false;
            motor._solveGrounding = false;
            motor.InteractiveRigidbodyHandling = false;
            controller.InputIntentProcessPipeline.SetInputSource(new TestInputSource());

            earlyUpdate(this.root);
            setServerTime(FIRST_TICK_TIME);
            update(this.root);

            float3 position = unit.GetComponent<TransformComponent>().Transform.Location;
            Assert.That(math.lengthsq(position), Is.GreaterThan(POSITION_TOLERANCE));
            PxVec3 nativePosition = PxRigidActor_getGlobalPose(
                (PxRigidActor*)unit.GetComponent<GameplayRigidbody>().RigidBodyPtr).p;
            Assert.That(nativePosition.x, Is.EqualTo(position.x).Within(POSITION_TOLERANCE));
            Assert.That(nativePosition.y, Is.EqualTo(position.y).Within(POSITION_TOLERANCE));
            Assert.That(nativePosition.z, Is.EqualTo(position.z).Within(POSITION_TOLERANCE));
        }
        finally
        {
            GameplayPhysics.GameplayPhysicsHandler = previousPhysics;
        }
    }

    /// <summary>Production Map initialization installs an active World manager before any character is created.</summary>
    [Test]
    public void ServerMapInitializationCreatesKinematicManager()
    {
        int mapId = ConfigManager.Instance.Tables.TbLevelConfig.DicSceneId2Level.Keys.First();
        Root mapRoot = (Root)rootConstructor.Invoke(new object[]
            { mapId, ROOT_ZONE, ELogicSceneType.Map, nameof(ServerMapInitializationCreatesKinematicManager) });
        int previousLogLevel = Log.LogLevel;
        ILogger previousLogger = Log.Logger;
        RecordingLogger logger = new RecordingLogger();
        ProcessMessageManager messageManager = new ProcessMessageManager();
        try
        {
            Log.LogLevel = SUPPRESS_INFO_LOG_LEVEL;
            Log.Logger = logger;
            registerSingleton.Invoke(messageManager, null);
            new RootInit_Server_Map_Invoke().Handle(new Invoke_RootInit { Root = mapRoot }).GetAwaiter().GetResult();
            GameplayWorld mapWorld = (GameplayWorld)mapRoot.Scene.GetChild<LogicScene>(GameplayWorldFactory.DEFAULT_WORLD_ID);
            KinematicMoverManager manager = mapWorld.GetComponent<KinematicMoverManager>();
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.Enable, Is.True);
            Assert.That(manager.Settings.AutoSimulation, Is.True);
            TestCargo platform = mapWorld.AddChild<TestCargo>();
            TransformComponent transform = platform.AddComponent<TransformComponent>();
            platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
            KinematicRigidbodyMover mover = platform.AddComponent<KinematicRigidbodyMover>();
            ObservingMoverController controller = new ObservingMoverController(transform);
            mover.MoverController = controller;
            earlyUpdate(mapRoot);
            setServerTime(FIRST_TICK_TIME);
            update(mapRoot);
            Assert.That(controller.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        }
        finally
        {
            mapRoot.Dispose();
            messageManager.Dispose();
            Log.LogLevel = previousLogLevel;
            Log.Logger = previousLogger;
        }
        Assert.That(logger.Errors, Is.Empty);
    }

    /// <summary>Negative and non-finite client timestamps cannot be accepted as server moves.</summary>
    [TestCase(-NETWORK_MOVE_TIME)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void ServerRejectsInvalidMovementTimestamps(float timestamp)
    {
        Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.Authority, ENetRole.AutonomousProxy, float3.zero, quaternion.identity);
        CharacterMovementComponent movement = unit.AddComponent<CharacterMovementComponent>();
        NetworkPredictionData_Server_CharacterMovement serverData = new NetworkPredictionData_Server_CharacterMovement();
        Assert.That(movement.VerifyClientTimeStamp(timestamp, serverData), Is.False);
        Assert.That(serverData.CurrentClientTimeStamp, Is.Zero);
    }

    private static void invokeServerMove(Unit unit, C2M_CharacterMoveData message)
    {
        Type handlerType = typeof(CharacterMovementComponentSystems).Assembly.GetType(
            "Bliss.MessageHandler_C2M_CharacterMoveData")
            ?? throw new InvalidOperationException("The server movement handler was not found.");
        MethodInfo handler = handlerType.GetMethod("HandleMessage", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("The server movement handler entry was not found.");
        ETTask task = (ETTask)handler.Invoke(null, new object[] { unit, message })!;
        task.GetAwaiter().GetResult();
    }

    /// <summary>Reusable and management entities do not opt into gameplay scheduling.</summary>
    [Test]
    public void OrdinaryManagersAndComponentsRetainEntityOwnership()
    {
        Type[] ordinaryTypes =
        {
            typeof(UnitManager), typeof(BulletManager), typeof(SceneActorManager),
            typeof(AoiManagerComponent), typeof(LevelPlayerSpawnPointMgr),
            typeof(LevelPlayerMgrComponent), typeof(LevelNetworkObjectCollectionComponent),
            typeof(NetRoleComponent), typeof(TransformComponent), typeof(RemoteGhost),
            typeof(ReplicationGraph), typeof(DemoGameMode), typeof(LevelGameWorldComponent),
        };
        foreach (Type type in ordinaryTypes)
        {
            Assert.That(typeof(GamePlayEntity).IsAssignableFrom(type), Is.False, type.FullName);
        }

        Assert.That(typeof(GamePlayEntity).IsAssignableFrom(typeof(AbilitySystemComponent)), Is.True);
    }

    /// <summary>A login transit Unit survives the production BSON transfer and mounts in the destination World.</summary>
    [Test]
    public void TransitUnitRoundTripsAndMountsInMapWorld()
    {
        GameplayWorld transit = GameplayWorldFactory.Create(this.root.Scene, HEALTHY_WORLD_ID,
            HEALTHY_WORLD_INSTANCE_ID, "Transit");
        Unit source = UnitHelper.CreateMinimalUnit(transit, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.Authority, ENetRole.AutonomousProxy, moveDirection, quaternion.identity);
        byte[] payload = MongoHelper.Serialize(source);
        source.Dispose();

        Unit received = (Unit)MongoHelper.Deserialize<Entity>(payload);
        UnitHelper.MountTransferredUnit(this.world, received);

        Assert.That(received.GetLevelGameplayWorld(), Is.SameAs(this.world));
        Assert.That(received.Id, Is.EqualTo(TEST_UNIT_ID));
        Assert.That(received.UnitConfigId, Is.EqualTo(UNIT_CONFIG_ID));
        Assert.That(received.GetComponent<TransformComponent>().Transform.Location.x, Is.EqualTo(UNIT_AXIS));
        Assert.That(received.GetComponent<NetRoleComponent>(), Is.Not.Null);
        Assert.That(received.GetComponent<GameplayLayer>(), Is.InstanceOf<GamePlayEntity>());
        Assert.That(received.GetComponent<GameplayLayer>().Layer, Is.EqualTo(GameplayLayerDefine.Unit));
        received.Dispose();
    }

    /// <summary>A transferred player mounts in its enabled destination Level before character initialization.</summary>
    [Test]
    public void TransitUnitMountsInDestinationLevelAndInitializesCharacter()
    {
        IGameplayPhysicsHandler previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        try
        {
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();
            PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
            this.level.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            UnitManager manager = this.world.AddComponent<UnitManager>();
            GameplayWorld transit = GameplayWorldFactory.Create(this.root.Scene, HEALTHY_WORLD_ID,
                HEALTHY_WORLD_INSTANCE_ID, "Transit");
            Unit source = UnitHelper.CreateMinimalUnit(transit, UNIT_CONFIG_ID, TEST_UNIT_ID,
                ENetRole.Authority, ENetRole.AutonomousProxy, moveDirection, quaternion.identity);
            byte[] payload = MongoHelper.Serialize(source);
            source.Dispose();
            Unit received = (Unit)MongoHelper.Deserialize<Entity>(payload);
            UnitHelper.MountTransferredUnit(this.level, received);
            received.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.OrderedMessage);
            CharacterController controller = received.AddComponent<CharacterController>();

            Assert.That(received.Scene(), Is.SameAs(this.level));
            Assert.That(received.GetLevelGameplayWorld(), Is.SameAs(this.world));
            Assert.That(manager.DicEntityId2Unit.ContainsKey(TEST_UNIT_ID), Is.True);
            Assert.That(controller.KinematicCharacterComponent.Capsule, Is.Not.Null);
            Assert.That(received.GetComponent<MailBoxComponent>(), Is.Not.Null);
            received.Dispose();
            Assert.That(manager.DicEntityId2Unit, Is.Empty);
        }
        finally
        {
            GameplayPhysics.GameplayPhysicsHandler = previousPhysics;
        }
    }

    /// <summary>A transferred Unit cannot be mounted into a prepared but disabled LevelScene.</summary>
    [Test]
    public void TransferRejectsDisabledDestinationLevel()
    {
        GameplayWorld transit = GameplayWorldFactory.Create(this.root.Scene, HEALTHY_WORLD_ID,
            HEALTHY_WORLD_INSTANCE_ID, "Transit");
        Unit source = UnitHelper.CreateMinimalUnit(transit, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.Authority, ENetRole.AutonomousProxy, moveDirection, quaternion.identity);
        byte[] payload = MongoHelper.Serialize(source);
        source.Dispose();
        Unit received = (Unit)MongoHelper.Deserialize<Entity>(payload);
        Assert.Throws<InvalidOperationException>(() => UnitHelper.MountTransferredUnit(this.level, received));
        Assert.That(received.Parent, Is.Null);
        Assert.That(received.LogicScene, Is.Null);
    }

    /// <summary>Transferred players remain routable to the offline handler in both supported scene types.</summary>
    [Test]
    public void PlayerOfflineHandlerSupportsLevelAndWorld()
    {
        MessageHandlerAttribute attribute = typeof(MassageHandler_G2M_PlayerOfflineMessage)
            .GetCustomAttribute<MessageHandlerAttribute>()
            ?? throw new InvalidOperationException("The offline handler has no scene routing attribute.");
        Assert.That(attribute.LogicSceneType.HasSameFlag(ELogicSceneType.GameplayWorld), Is.True);
        Assert.That(attribute.LogicSceneType.HasSameFlag(ELogicSceneType.Level), Is.True);
    }

    /// <summary>Smoothing is dispatched by its World and stops when the World faults.</summary>
    [Test]
    public void SmoothingUpdatesThroughWorldAndStopsOnFailure()
    {
        Unit unit = UnitHelper.CreateMinimalUnit(this.level, UNIT_CONFIG_ID, TEST_UNIT_ID,
            ENetRole.Authority, ENetRole.SimulatedProxy, float3.zero, quaternion.identity);
        SmoothTransformComponent smoothing = unit.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>(
            new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
        smoothing.ViewTimeStamp = UNUPDATED_VIEW_TIME;
        earlyUpdate(this.root);
        update(this.root);
        Assert.That(smoothing.ViewTimeStamp, Is.Not.EqualTo(UNUPDATED_VIEW_TIME));

        this.world.StopWithFailure(new InvalidOperationException("test failure"));
        smoothing.ViewTimeStamp = UNUPDATED_VIEW_TIME;
        earlyUpdate(this.root);
        update(this.root);
        Assert.That(smoothing.ViewTimeStamp, Is.EqualTo(UNUPDATED_VIEW_TIME));
    }

    /// <summary>The ShooterGear timer follows World updates, Level activation and entity disposal.</summary>
    [Test]
    public void ShooterGearTimerRunsOnlyForEnabledLiveWorld()
    {
        this.root.Scene.RemoveComponent<TimerComponent>();
        AoiManagerComponent aoiManager = this.world.AddComponent<AoiManagerComponent>();
        SceneActorManager actorManager = this.world.AddComponent<SceneActorManager>();
        SceneActorDataTypeManager typeRegistry = new SceneActorDataTypeManager();
        int previousLogLevel = Log.LogLevel;
        try
        {
            Log.LogLevel = SUPPRESS_INFO_LOG_LEVEL;
            typeRegistry.Init();
            registerSingleton.Invoke(typeRegistry, null);
            ShooterGearConfig config = new ShooterGearConfig
            {
                BulletConfigId = BULLET_CONFIG_ID,
                Rotation = quaternion.identity,
            };
            List<Entity> actors = new List<Entity>();
            LogicSceneFactory.CreateSceneActor(this.level, new List<SceneActorData>
            {
                new SceneActorData
                {
                    NetworkIdentity = TEST_SCENE_ACTOR_ID,
                    TypeIdentifier = typeof(ShooterGearConfig).FullName!.GetStableHashCode(),
                    Data = Serializer.Serialize(config),
                },
            }, ENetRole.Authority, ENetRole.SimulatedProxy, actors);
            SceneActor actor = (SceneActor)actors.Single();
            ShooterGear gear = actor.GetComponent<ShooterGear>();
            AOIEntityCmp aoi = actor.GetComponent<AOIEntityCmp>();
            Assert.That(actor, Is.InstanceOf<GamePlayEntity>());
            Assert.That(gear, Is.InstanceOf<GamePlayEntity>());
            Assert.That(aoi, Is.InstanceOf<GamePlayEntity>());
            Assert.That(actorManager.DicEntityId2SceneActor.ContainsKey(TEST_SCENE_ACTOR_ID), Is.True);
            Assert.That(aoiManager.Entries.Count, Is.EqualTo(TWO_REGISTRATIONS));
            Assert.That(gear.UpdateTimer, Is.Not.Zero);

            earlyUpdate(this.root);
            setServerTime(BEFORE_FIRST_SHOOT_TIME);
            update(this.root);
            Assert.That(RecordShot.Count, Is.Zero);
            setServerTime(FIRST_SHOOT_TIME);
            update(this.root);
            Assert.That(RecordShot.Count, Is.Zero);

            LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
            setServerTime(SECOND_SHOOT_TIME);
            update(this.root);
            Assert.That(RecordShot.Count, Is.EqualTo(FIRST_SIMULATION_COUNT));

            long timerId = gear.UpdateTimer;
            actor.Dispose();
            Assert.That(gear.UpdateTimer, Is.Zero);
            Assert.That(this.world.Timer.Cancel(ref timerId), Is.False);
            Assert.That(timerId, Is.Zero);
            Assert.That(actorManager.DicEntityId2SceneActor, Is.Empty);
            Assert.That(aoiManager.Entries, Is.Empty);
            setServerTime(THIRD_SHOOT_TIME);
            update(this.root);
            Assert.That(RecordShot.Count, Is.EqualTo(FIRST_SIMULATION_COUNT));
        }

        finally
        {
            typeRegistry.Dispose();
            Log.LogLevel = previousLogLevel;
        }
    }

    /// <summary>A faulted World does not dispatch its active ShooterGear timer.</summary>
    [Test]
    public void ShooterGearTimerStopsWhenWorldFaults()
    {
        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        SceneActor actor = this.level.AddChild<SceneActor>();
        actor.AddComponent<TransformComponent>().SetPositionAndRotation(float3.zero, quaternion.identity);
        ShooterGear gear = actor.AddComponent<ShooterGear>();
        gear.SetConfig(new ShooterGearConfig { BulletConfigId = BULLET_CONFIG_ID });
        gear.EndSceneInit();
        Assert.That(gear.UpdateTimer, Is.Not.Zero);

        this.world.StopWithFailure(new InvalidOperationException("test failure"));
        earlyUpdate(this.root);
        setServerTime(FIRST_SHOOT_TIME);
        update(this.root);
        Assert.That(RecordShot.Count, Is.Zero);
        actor.Dispose();
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
        Assert.That(bullet, Is.Not.InstanceOf<GamePlayEntity>());
        Assert.That(bullet.GetComponent<AOIEntityCmp>(), Is.InstanceOf<GamePlayEntity>());
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

    /// <summary>A World-owned manager does not simulate or interpolate a prepared Level's mover.</summary>
    [Test]
    public void WorldKinematicManagerWaitsForParticipantLevel()
    {
        KinematicMoverManager manager = this.world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
            new KinematicMoverManagerSettings { AutoSimulation = true, Interpolate = true, InterpolateMover = true });
        manager.Enable = true;
        TestCargo platform = this.level.AddChild<TestCargo>();
        TransformComponent transform = platform.AddComponent<TransformComponent>();
        platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover mover = platform.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController controller = new ObservingMoverController(transform);
        mover.MoverController = controller;
        mover.TransientPosition = moveDirection;
        manager.LastCustomInterpolationStartTime = TimeInfo.Instance.ClientNow() - ELAPSED_INTERPOLATION_MILLISECONDS;
        manager.LastCustomInterpolationDeltaTime = EXPECTED_DELTA_SECONDS;

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        lateUpdate(this.root);
        Assert.That(controller.UpdateCount, Is.Zero);
        Assert.That(transform.Transform.Location.x, Is.Zero.Within(POSITION_TOLERANCE));
        Assert.That(mover.InitialTickPosition.x, Is.Zero.Within(POSITION_TOLERANCE));
        Assert.That(mover.LatestInterpolationPosition.x, Is.Zero.Within(POSITION_TOLERANCE));

        LevelSceneFactory.Enable(this.level).GetAwaiter().GetResult();
        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(controller.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
    }

    /// <summary>Disposing one mover unregisters it without disturbing another live mover.</summary>
    [Test]
    public void DestroyedMoverUnregistersAndLeavesOtherMoverActive()
    {
        KinematicMoverManager manager = this.createKinematicManager();
        TestCargo first = this.world.AddChild<TestCargo>();
        TransformComponent firstTransform = first.AddComponent<TransformComponent>();
        first.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover firstMover = first.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController firstController = new ObservingMoverController(firstTransform);
        firstMover.MoverController = firstController;

        TestCargo second = this.world.AddChild<TestCargo>();
        TransformComponent secondTransform = second.AddComponent<TransformComponent>();
        second.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover secondMover = second.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController secondController = new ObservingMoverController(secondTransform);
        secondMover.MoverController = secondController;
        Assert.That(manager.PhysicsMovers.Count, Is.EqualTo(TWO_REGISTRATIONS));

        firstMover.Dispose();
        Assert.That(manager.PhysicsMovers.Count, Is.EqualTo(SINGLE_REGISTRATION));
        Assert.That((KinematicRigidbodyMover)manager.PhysicsMovers[FIRST_INDEX], Is.SameAs(secondMover));
        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(firstController.UpdateCount, Is.Zero);
        Assert.That(secondController.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        secondMover.Dispose();
        Assert.That(manager.PhysicsMovers, Is.Empty);
    }

    /// <summary>Removing a mover during its callback does not skip other live snapshot participants.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public void MoverRemovalDuringSimulationKeepsOtherParticipants(bool removeSelf)
    {
        KinematicMoverManager manager = this.createKinematicManager();
        TestCargo first = this.world.AddChild<TestCargo>();
        first.AddComponent<TransformComponent>();
        first.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover firstMover = first.AddComponent<KinematicRigidbodyMover>();
        TestCargo second = this.world.AddChild<TestCargo>();
        TransformComponent secondTransform = second.AddComponent<TransformComponent>();
        second.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover secondMover = second.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController secondController = new ObservingMoverController(secondTransform);
        secondMover.MoverController = secondController;
        TestCargo third = this.world.AddChild<TestCargo>();
        TransformComponent thirdTransform = third.AddComponent<TransformComponent>();
        third.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover thirdMover = third.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController thirdController = new ObservingMoverController(thirdTransform);
        thirdMover.MoverController = thirdController;
        firstMover.MoverController = new DisposingMoverController(removeSelf ? firstMover : secondMover);

        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);

        Assert.That(manager.PhysicsMovers.Count, Is.EqualTo(TWO_REGISTRATIONS));
        Assert.That(secondController.UpdateCount, Is.EqualTo(removeSelf ? FIRST_SIMULATION_COUNT : NO_REGISTRATIONS));
        Assert.That(thirdController.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
        Assert.That(thirdTransform.Transform.Location.x, Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
    }

    /// <summary>A mover registered inside a callback first runs on the following tick.</summary>
    [Test]
    public void MoverAddedDuringSimulationStartsOnNextTick()
    {
        this.createKinematicManager();
        TestCargo first = this.world.AddChild<TestCargo>();
        first.AddComponent<TransformComponent>();
        first.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover mover = first.AddComponent<KinematicRigidbodyMover>();
        AddingMoverController controller = new AddingMoverController(this.world);
        mover.MoverController = controller;
        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(controller.AddedController, Is.Not.Null);
        Assert.That(controller.AddedController!.UpdateCount, Is.Zero);
        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(controller.AddedController.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
    }

    /// <summary>A live mover unregistered and re-registered during dispatch waits for the next tick.</summary>
    [Test]
    public void MoverReregisteredDuringSimulationStartsOnNextTick()
    {
        KinematicMoverManager manager = this.createKinematicManager();
        TestCargo first = this.world.AddChild<TestCargo>();
        first.AddComponent<TransformComponent>();
        first.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover firstMover = first.AddComponent<KinematicRigidbodyMover>();
        TestCargo second = this.world.AddChild<TestCargo>();
        TransformComponent secondTransform = second.AddComponent<TransformComponent>();
        second.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
        KinematicRigidbodyMover secondMover = second.AddComponent<KinematicRigidbodyMover>();
        ObservingMoverController observer = new ObservingMoverController(secondTransform);
        secondMover.MoverController = observer;
        firstMover.MoverController = new ReregisteringMoverController(manager, secondMover);
        earlyUpdate(this.root);
        setServerTime(FIRST_TICK_TIME);
        update(this.root);
        Assert.That(observer.UpdateCount, Is.Zero);
        setServerTime(DELAYED_TICK_TIME);
        update(this.root);
        Assert.That(observer.UpdateCount, Is.EqualTo(FIRST_SIMULATION_COUNT));
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

    /// <summary>Observes the real ShooterGear event without spawning networked Bullets.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class RecordShot : AEvent<LogicScene, GameplayEvent_ShootBullet>
    {
        /// <summary>Number of observed shot events in the current test.</summary>
        public static int Count;

        /// <inheritdoc />
        protected override async ETTask Run(LogicScene scene, GameplayEvent_ShootBullet data)
        {
            Count++;
            await ETTask.CompletedTask;
        }
    }

    /// <summary>Supplies a deterministic movement input and one buffered jump edge.</summary>
    public sealed class TestInputSource : IInputSource
    {
        /// <summary>Whether the next sample contains a jump edge.</summary>
        public bool PressJump = true;
        /// <summary>Whether to add a later cancellation after the jump edge.</summary>
        public bool CancelAfterJump;
        /// <summary>Number of movement-input samples.</summary>
        public int MovementReadCount;

        /// <inheritdoc />
        public float3 GetMovementInput()
        {
            this.MovementReadCount++;
            return moveDirection;
        }

        /// <inheritdoc />
        public float3 GetLookInput() => float3.zero;
        /// <inheritdoc />
        public bool GetActionDown(EGameplayInputAction action) => this.PressJump;
        /// <inheritdoc />
        public void GetActionStatus(List<GameplayInputActionStatus> result)
        {
            if (this.PressJump)
            {
                result.Add(new GameplayInputActionStatus
                {
                    TimStamp = NO_SERVER_TIME_OFFSET,
                    Action = EGameplayInputAction.Jump,
                    Status = EInputActionStatus.Down,
                });
                if (this.CancelAfterJump)
                {
                    result.Add(new GameplayInputActionStatus
                    {
                        TimStamp = FIRST_TICK_TIME,
                        Action = EGameplayInputAction.Jump,
                        Status = EInputActionStatus.Cancel,
                    });
                }
            }

        }
        /// <inheritdoc />
        public quaternion GetCameraRotation() => quaternion.identity;
    }

    /// <summary>Disposes a chosen mover from within a real manager callback.</summary>
    public sealed class DisposingMoverController : IMoverController
    {
        private readonly KinematicRigidbodyMover target;

        /// <summary>Creates a controller that disposes the given mover.</summary>
        public DisposingMoverController(KinematicRigidbodyMover target) => this.target = target;

        /// <inheritdoc />
        public void UpdateMovement(out float3 goalPosition, out quaternion goalRotation, float deltaTime)
        {
            this.target.Dispose();
            goalPosition = moveDirection;
            goalRotation = quaternion.identity;
        }
    }

    /// <summary>Registers a platform from a manager callback.</summary>
    public sealed class AddingMoverController : IMoverController
    {
        private readonly GameplayWorld world;
        /// <summary>The controller created on the first update.</summary>
        public ObservingMoverController? AddedController { get; private set; }

        /// <summary>Creates a controller that registers a platform in the given World.</summary>
        public AddingMoverController(GameplayWorld world) => this.world = world;

        /// <inheritdoc />
        public void UpdateMovement(out float3 goalPosition, out quaternion goalRotation, float deltaTime)
        {
            if (this.AddedController == null)
            {
                TestCargo platform = this.world.AddChild<TestCargo>();
                TransformComponent transform = platform.AddComponent<TransformComponent>();
                platform.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(default);
                KinematicRigidbodyMover mover = platform.AddComponent<KinematicRigidbodyMover>();
                this.AddedController = new ObservingMoverController(transform);
                mover.MoverController = this.AddedController;
            }
            goalPosition = moveDirection;
            goalRotation = quaternion.identity;
        }
    }

    /// <summary>Re-registers one live mover during the first manager callback.</summary>
    public sealed class ReregisteringMoverController : IMoverController
    {
        private readonly KinematicMoverManager manager;
        private readonly KinematicRigidbodyMover mover;
        private bool completed;

        /// <summary>Creates a controller for the given manager and mover.</summary>
        public ReregisteringMoverController(KinematicMoverManager manager, KinematicRigidbodyMover mover)
        {
            this.manager = manager;
            this.mover = mover;
        }

        /// <inheritdoc />
        public void UpdateMovement(out float3 goalPosition, out quaternion goalRotation, float deltaTime)
        {
            if (!this.completed)
            {
                this.completed = true;
                this.manager.UnregisterPhysicsMover(this.mover);
                this.manager.RegisterPhysicsMover(this.mover);
            }
            goalPosition = moveDirection;
            goalRotation = quaternion.identity;
        }
    }

    private sealed class RecordingLogger : ILogger
    {
        internal readonly List<string> Errors = new List<string>();
        public void Trace(string message) { }
        public void Warning(string message) { }
        public void Info(string message) { }
        public void Debug(string message) { }
        public void Error(string message) => this.Errors.Add(message);
        public void Error(Exception exception) => this.Errors.Add(exception.ToString());
        public void Trace(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message) { }
        public void Warning(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message) { }
        public void Info(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message) { }
        public void Debug(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message) { }
        public void Error(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message) => this.Errors.Add(message.ToStringAndClear());
    }
}
