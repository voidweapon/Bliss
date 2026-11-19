using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bliss;
using Bliss.CharacterControllers;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Debugger.GameDebugStats;
using Bliss.Physics;
using Bliss.Timer;
using Bliss.ViewLogic.Config;
using Bliss.ViewLogic.Physics;
using Bliss.ViewModel;
using ET;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

/// <summary>Exercises the client physical scene through the Unity query adapter.</summary>
public class PhysicalSceneClientTests
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int LEVEL_ID = 303;
    private const int DESTINATION_LEVEL_ID = 304;
    private const int TEST_LAYER = 30;
    private const int OTHER_LAYER = 29;
    private const int TEST_LAYER_MASK = 1 << TEST_LAYER;
    private const int OTHER_LAYER_MASK = 1 << OTHER_LAYER;
    private const int RESULT_CAPACITY = 4;
    private const int EXPECTED_HIT_COUNT = 1;
    private const float TARGET_X = 5f;
    private const float CAST_RADIUS = 0.25f;
    private const float MAX_DISTANCE = 10f;
    private const float SHORT_DISTANCE = 1f;
    private const float EXPECTED_POINT_X = 4.5f;
    private const float POSITION_TOLERANCE = 0.01f;
    private const float ZERO_AXIS = 0f;
    private const float UNIT_AXIS = 1f;
    private const int UNIT_CONFIG_ID = 1;
    private const long UNIT_ID = 404;
    private const int BULLET_CONFIG_ID = 1;
    private const long BULLET_ID = 405;
    private const long EXPIRED_LIFETIME_MILLISECONDS = 0;
    private const long BULLET_REMAINING_LIFETIME_MILLISECONDS = 1000;
    private const int SUPPRESS_INFO_LOG_LEVEL = 4;
    private const long UNUPDATED_VIEW_TIME = -1;
    private const long INITIAL_TICK_TIME = 1000;
    private const long FIRST_TICK_TIME = 1050;
    private const int FIRST_INPUT_TIME = 1;
    private const int TWO_INPUT_SAMPLES = 2;
    private const float UNSIMULATED_POSITION = -10f;
    private TypeManager typeManager;
    private EntitySystemManager entitySystemManager;
    private EventSystem eventSystem;
    private Root root;
    private GameObject target;

    /// <summary>Creates a real World and an isolated Unity collider.</summary>
    [SetUp]
    public void SetUp()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(PhysicalSceneSystems).Assembly,
            typeof(InvokeHandler_SpereCastNonAlloc).Assembly,
            typeof(EventHandler_AfterCreateLevelScene).Assembly,
            typeof(PhysicalSceneClientTests).Assembly,
        });
        this.typeManager.GetTypes(typeof(EventAttribute)).Remove(typeof(EventHandler_AfterCreateLevelScene));
        this.typeManager.GetTypes(typeof(EventAttribute)).Remove(typeof(EventHandler_AfterCreateGameplayWorld));
        this.typeManager.GetTypes(typeof(InvokeAttribute)).Remove(typeof(InvokeHandler_ConfigManagerLoadFile));
        this.typeManager.GetTypes(typeof(InvokeAttribute)).Remove(typeof(EventHanlder_GameplayRigidbodyAwake));
        this.typeManager.GetTypes(typeof(InvokeAttribute)).Remove(typeof(EventHanlder_GameplayRigidbodyDestroy));
        this.typeManager.GetTypes(typeof(InvokeAttribute)).Remove(typeof(EventHanlder_GameplayCapsuleColliderAwake));
        this.typeManager.GetTypes(typeof(InvokeAttribute)).Remove(typeof(EventHanlder_GameplayCapsuleColliderDestroy));
        registerSingleton(this.typeManager);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton(this.entitySystemManager);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton(this.eventSystem);

        ConstructorInfo constructor = typeof(Root).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) }, null)
            ?? throw new InvalidOperationException("Root constructor was not found.");
        this.root = (Root)constructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(PhysicalSceneClientTests) });
        this.target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        this.target.layer = TEST_LAYER;
        this.target.transform.position = new Vector3(TARGET_X, ZERO_AXIS, ZERO_AXIS);
        UnityEngine.Physics.SyncTransforms();
    }

    /// <summary>Releases the fixture's scene, collider and singleton registries.</summary>
    [TearDown]
    public void TearDown()
    {
        this.root?.Dispose();
        if (this.target != null)
        {
            UnityEngine.Object.DestroyImmediate(this.target);
        }

        this.eventSystem?.Dispose();
        this.entitySystemManager?.Dispose();
        this.typeManager?.Dispose();
    }

    /// <summary>The prepared LevelScene owns a query adapter preserving Unity collision filters.</summary>
    [Test]
    public void LevelPhysicalSceneQueriesUnityCollider()
    {
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        LogicScene level = LevelSceneFactory.Create(
            world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
        PhysicalScene physicalScene = level.GetComponent<PhysicalScene>();
        Assert.That(physicalScene, Is.Not.Null);
        Assert.That(physicalScene.GetLevelGameplayWorld(), Is.SameAs(world));
        GameplayRaycastHit[] hits = new GameplayRaycastHit[RESULT_CAPACITY];

        int count = physicalScene.SpereCastNonAlloc(
            float3.zero, CAST_RADIUS, new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS), MAX_DISTANCE, TEST_LAYER_MASK, hits);
        Assert.That(count, Is.EqualTo(EXPECTED_HIT_COUNT));
        Assert.That(hits[0].point.x, Is.EqualTo(EXPECTED_POINT_X).Within(POSITION_TOLERANCE));
        Assert.That(physicalScene.SpereCastNonAlloc(
            float3.zero, CAST_RADIUS, new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS), MAX_DISTANCE, OTHER_LAYER_MASK, hits), Is.Zero);
        Assert.That(physicalScene.SpereCastNonAlloc(
            float3.zero, CAST_RADIUS, new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS), SHORT_DISTANCE, TEST_LAYER_MASK, hits), Is.Zero);
    }

    /// <summary>Core's Unity assembly has no runtime references to gameplay or view assemblies.</summary>
    [Test]
    public void CoreAssemblyDoesNotReferenceGameplayOrViewAssemblies()
    {
        string modelName = typeof(GameplayWorld).Assembly.GetName().Name;
        string logicName = typeof(GameplaySubtreeMigration).Assembly.GetName().Name;
        string viewModelName = typeof(GameObjectViewComponent).Assembly.GetName().Name;
        string viewLogicName = typeof(GameplaySubtreeMigrationClientResources).Assembly.GetName().Name;
        foreach (AssemblyName reference in typeof(Entity).Assembly.GetReferencedAssemblies())
        {
            Assert.That(reference.Name, Is.Not.EqualTo(modelName).And.Not.EqualTo(logicName)
                .And.Not.EqualTo(viewModelName).And.Not.EqualTo(viewLogicName));
        }
    }

    /// <summary>A client collider cannot migrate separately from its gameplay rigidbody.</summary>
    [Test]
    public void MigrationRejectsColliderWithoutItsRigidbody()
    {
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        LogicScene source = LevelSceneFactory.Create(
            world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
        LogicScene destination = LevelSceneFactory.Create(
            world, world, DESTINATION_LEVEL_ID, DESTINATION_LEVEL_ID, nameof(DESTINATION_LEVEL_ID))
            .GetAwaiter().GetResult();
        Unit unit = UnitHelper.CreateMinimalUnit(source, UNIT_CONFIG_ID, UNIT_ID,
            ENetRole.Authority, ENetRole.SimulatedProxy, float3.zero, quaternion.identity);
        GameplayRigidbody body = unit.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(
            new GameplayRigidbodyInitParams());
        GameplayCapsuleCollider collider = unit.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(
            new CapsuleColliderInitParams());

        Assert.That(collider.AttachedRigidbody, Is.SameAs(body));
        Assert.Throws<InvalidOperationException>(
            () => GameplaySubtreeMigration.MigrateAsync(collider, destination).GetAwaiter().GetResult());
        Assert.That(collider.Parent, Is.SameAs(unit));
        Assert.That(collider.Scene(), Is.SameAs(source));
    }

    /// <summary>A scene-owned Unity view and its collider are not silently left behind during migration.</summary>
    [Test]
    public void MigrationRejectsSceneOwnedUnityView()
    {
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        LogicScene source = LevelSceneFactory.Create(
            world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
        LogicScene destination = LevelSceneFactory.Create(
            world, world, DESTINATION_LEVEL_ID, DESTINATION_LEVEL_ID, nameof(DESTINATION_LEVEL_ID))
            .GetAwaiter().GetResult();
        Unit unit = UnitHelper.CreateMinimalUnit(source, UNIT_CONFIG_ID, UNIT_ID,
            ENetRole.Authority, ENetRole.SimulatedProxy, float3.zero, quaternion.identity);
        GameObjectViewComponent view = unit.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(
            new GameObjectViewComponentInitParam { View = this.target });
        try
        {
            Assert.Throws<InvalidOperationException>(
                () => GameplaySubtreeMigration.MigrateAsync(unit, destination).GetAwaiter().GetResult());
            Assert.That(unit.Parent, Is.SameAs(source));
            Assert.That(unit.Scene(), Is.SameAs(source));
            GameplayRaycastHit[] hits = new GameplayRaycastHit[RESULT_CAPACITY];
            Assert.That(source.GetComponent<PhysicalScene>().SpereCastNonAlloc(
                float3.zero, CAST_RADIUS, new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS),
                MAX_DISTANCE, TEST_LAYER_MASK, hits), Is.EqualTo(EXPECTED_HIT_COUNT));
        }
        finally
        {
            view.View = null;
        }
    }

    /// <summary>A Global-hosted client body and collider survive migration and source LevelScene disposal.</summary>
    [Test]
    public void MigrationPreservesGlobalUnityPhysics()
    {
        GameObject globalRoot = new GameObject(nameof(MigrationPreservesGlobalUnityPhysics));
        GameObject entitiesRoot = new GameObject(nameof(ViewGlobalComponent.EntitiesTransform));
        ViewEntityMapManager viewMap = new ViewEntityMapManager();
        GameObjectViewComponent view = null;
        try
        {
            entitiesRoot.transform.SetParent(globalRoot.transform);
            this.target.transform.SetParent(entitiesRoot.transform);
            this.target.GetComponent<BoxCollider>().enabled = false;
            Rigidbody unityBody = this.target.AddComponent<Rigidbody>();
            unityBody.isKinematic = true;
            CapsuleCollider unityCollider = this.target.AddComponent<CapsuleCollider>();
            UnityEngine.Physics.SyncTransforms();
            viewMap.Init();
            registerSingleton(viewMap);
            ViewGlobalComponent global = new ViewGlobalComponent
            {
                GlobalTransform = globalRoot.transform,
                EntitiesTransform = entitiesRoot.transform,
            };
            this.root.Scene.AddComponent(global);

            GameplayWorld world = GameplayWorldFactory.Create(
                this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
            LogicScene source = LevelSceneFactory.Create(
                world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
            LogicScene destination = LevelSceneFactory.Create(
                world, world, DESTINATION_LEVEL_ID, DESTINATION_LEVEL_ID, nameof(DESTINATION_LEVEL_ID))
                .GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(source, UNIT_CONFIG_ID, UNIT_ID,
                ENetRole.Authority, ENetRole.SimulatedProxy, float3.zero, quaternion.identity);
            view = unit.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(
                new GameObjectViewComponentInitParam { View = this.target });
            GameplayRigidbody body = unit.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(
                new GameplayRigidbodyInitParams { IsKinematic = true });
            GameplayCapsuleCollider collider = unit.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(
                new CapsuleColliderInitParams());
            viewMap.MapCollider(collider, unityCollider);

            GameplaySubtreeMigration.MigrateAsync(unit, destination).GetAwaiter().GetResult();
            source.Dispose();

            Assert.That(unit.Parent, Is.SameAs(destination));
            Assert.That(body.IsDisposed, Is.False);
            Assert.That(collider.IsDisposed, Is.False);
            Assert.That(unityCollider.attachedRigidbody, Is.SameAs(unityBody));
            GameplayRaycastHit[] hits = new GameplayRaycastHit[RESULT_CAPACITY];
            Assert.That(destination.GetComponent<PhysicalScene>().SpereCastNonAlloc(
                float3.zero, CAST_RADIUS, new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS),
                MAX_DISTANCE, TEST_LAYER_MASK, hits), Is.EqualTo(EXPECTED_HIT_COUNT));
        }
        finally
        {
            if (view != null)
            {
                view.View = null;
            }

            this.target.transform.SetParent(null);
            UnityEngine.Object.DestroyImmediate(globalRoot);
            viewMap.Dispose();
        }
    }

    /// <summary>A collider whose Unity rigidbody disagrees with gameplay ownership cannot migrate.</summary>
    [Test]
    public void MigrationRejectsMismatchedUnityRigidbody()
    {
        GameObject globalRoot = new GameObject(nameof(MigrationRejectsMismatchedUnityRigidbody));
        GameObject entitiesRoot = new GameObject(nameof(ViewGlobalComponent.EntitiesTransform));
        ViewEntityMapManager viewMap = new ViewEntityMapManager();
        GameObjectViewComponent view = null;
        try
        {
            entitiesRoot.transform.SetParent(globalRoot.transform);
            this.target.transform.SetParent(entitiesRoot.transform);
            this.target.AddComponent<Rigidbody>().isKinematic = true;
            CapsuleCollider unityCollider = this.target.AddComponent<CapsuleCollider>();
            viewMap.Init();
            registerSingleton(viewMap);
            this.root.Scene.AddComponent(new ViewGlobalComponent
            {
                GlobalTransform = globalRoot.transform,
                EntitiesTransform = entitiesRoot.transform,
            });
            GameplayWorld world = GameplayWorldFactory.Create(
                this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
            LogicScene source = LevelSceneFactory.Create(
                world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
            LogicScene destination = LevelSceneFactory.Create(
                world, world, DESTINATION_LEVEL_ID, DESTINATION_LEVEL_ID, nameof(DESTINATION_LEVEL_ID))
                .GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateMinimalUnit(source, UNIT_CONFIG_ID, UNIT_ID,
                ENetRole.Authority, ENetRole.SimulatedProxy, float3.zero, quaternion.identity);
            view = unit.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(
                new GameObjectViewComponentInitParam { View = this.target });
            GameplayCapsuleCollider collider = unit.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(
                new CapsuleColliderInitParams());
            viewMap.MapCollider(collider, unityCollider);

            Assert.Throws<InvalidOperationException>(
                () => GameplaySubtreeMigration.MigrateAsync(unit, destination).GetAwaiter().GetResult());
            Assert.That(unit.Parent, Is.SameAs(source));
            Assert.That(unityCollider.attachedRigidbody, Is.Not.Null);
        }
        finally
        {
            if (view != null)
            {
                view.View = null;
            }

            this.target.transform.SetParent(null);
            UnityEngine.Object.DestroyImmediate(globalRoot);
            viewMap.Dispose();
        }
    }

    /// <summary>Client Units and smoothing components use their LevelScene's GameplayWorld.</summary>
    [Test]
    public void LevelUnitSmoothingFollowsWorldLifecycle()
    {
        GameplayWorld world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        LogicScene level = LevelSceneFactory.Create(
            world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
        Unit unit = UnitHelper.CreateMinimalUnit(level, UNIT_CONFIG_ID, UNIT_ID,
            ENetRole.AutonomousProxy, ENetRole.Authority, float3.zero, quaternion.identity);
        SmoothTransformComponent smoothing = unit.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>(
            new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
        AOIEntityCmp aoi = unit.AddComponent<AOIEntityCmp>();
        SceneActor actor = level.AddChild<SceneActor>();
        Assert.That(unit, Is.InstanceOf<GamePlayEntity>());
        Assert.That(unit.GetComponent<GameplayLayer>(), Is.InstanceOf<GamePlayEntity>());
        Assert.That(aoi, Is.InstanceOf<GamePlayEntity>());
        Assert.That(actor, Is.InstanceOf<GamePlayEntity>());
        Assert.That(unit.GetComponent<TransformComponent>(), Is.Not.InstanceOf<GamePlayEntity>());
        Assert.That(smoothing.GetLevelGameplayWorld(), Is.SameAs(world));

        Action<Root> earlyUpdate = rootUpdate("EarlyUpdate");
        Action<Root> update = rootUpdate("Update");
        smoothing.ViewTimeStamp = UNUPDATED_VIEW_TIME;
        earlyUpdate(this.root);
        update(this.root);
        Assert.That(smoothing.ViewTimeStamp, Is.Not.EqualTo(UNUPDATED_VIEW_TIME));

        world.StopWithFailure(new InvalidOperationException("test failure"));
        smoothing.ViewTimeStamp = UNUPDATED_VIEW_TIME;
        earlyUpdate(this.root);
        update(this.root);
        Assert.That(smoothing.ViewTimeStamp, Is.EqualTo(UNUPDATED_VIEW_TIME));
        unit.Dispose();
        Assert.That(smoothing.IsDisposed, Is.True);
        Assert.That(aoi.IsDisposed, Is.True);
    }

    /// <summary>The production client World factory installs the managers required by gameplay factories.</summary>
    [Test]
    public void ClientWorldFactoryCreatesAoiManager()
    {
        RecordWorldCreated.Count = 0;
        GameplayWorld world = MapHelper.CreateGameplayWorld(this.root.Scene).GetAwaiter().GetResult();
        Assert.That(world.GetComponent<AoiManagerComponent>(), Is.Not.Null);
        Assert.That(world.GetComponent<UnitManager>(), Is.Not.Null);
        Assert.That(world.GetComponent<BulletManager>(), Is.Not.Null);
        Assert.That(RecordWorldCreated.Count, Is.EqualTo(EXPECTED_HIT_COUNT));
    }

    /// <summary>The client creates a replicated Bullet DTO, skipping expiry and registering live Bullets under its World.</summary>
    [Test]
    public void ClientBulletDtoCreatesWorldAoiAndSmoothing()
    {
        int previousLogLevel = Log.LogLevel;
        ConfigManager configManager = new ConfigManager();
        MoverHandlerManager moverHandlers = new MoverHandlerManager();
        try
        {
            Log.LogLevel = SUPPRESS_INFO_LOG_LEVEL;
            configManager.Init();
            registerSingleton(configManager);
            moverHandlers.Init();
            registerSingleton(moverHandlers);
            this.root.Scene.AddComponent<NetworkEntityManagerComponent>();
            this.root.Scene.AddComponent<TimerComponent>();
            GameplayWorld world = MapHelper.CreateGameplayWorld(this.root.Scene).GetAwaiter().GetResult();
            LogicScene level = LevelSceneFactory.Create(
                world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
            DTOBulletInfo expiredBulletInfo = DTOBulletInfo.Create();
            expiredBulletInfo.ConfigId = BULLET_CONFIG_ID;
            expiredBulletInfo.RemainingLifetimeMS = EXPIRED_LIFETIME_MILLISECONDS;
            Assert.That(expiredBulletInfo.TryCreateBullet(level, out Bullet expiredBullet), Is.False);
            Assert.That(expiredBullet, Is.Null);
            expiredBulletInfo.Dispose();

            SceneActor shooterHost = level.AddChild<SceneActor>();
            ShooterGear shooter = shooterHost.AddComponent<ShooterGear>();
            shooter.SetConfig(new ShooterGearConfig { BulletConfigId = BULLET_CONFIG_ID });
            DTOBulletInfo bulletInfo = DTOBulletInfo.Create();
            bulletInfo.BulletId = BULLET_ID;
            bulletInfo.ConfigId = BULLET_CONFIG_ID;
            bulletInfo.Position = float3.zero;
            bulletInfo.Rotation = quaternion.identity;
            bulletInfo.LocalRole = ENetRole.Authority;
            bulletInfo.RemoteRole = ENetRole.SimulatedProxy;
            bulletInfo.RemainingLifetimeMS = BULLET_REMAINING_LIFETIME_MILLISECONDS;
            bulletInfo.ShooterEntityId = shooterHost.Id;
            bulletInfo.TargetInfo = DTOGameplayTargetInfo.Create();
            bulletInfo.TargetInfo.Direction = new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS);
            Assert.That(bulletInfo.TryCreateBullet(level, out Bullet bullet), Is.True);
            AoiManagerComponent aoiManager = world.GetComponent<AoiManagerComponent>();
            Assert.That(bullet, Is.InstanceOf<GamePlayEntity>());
            Assert.That(bullet.GetComponent<AOIEntityCmp>(), Is.InstanceOf<GamePlayEntity>());
            Assert.That(bullet.GetComponent<SmoothTransformComponent>(), Is.InstanceOf<GamePlayEntity>());
            Assert.That(aoiManager.Entries.Count, Is.EqualTo(EXPECTED_HIT_COUNT));
            bullet.Dispose();
            bulletInfo.Dispose();
            Assert.That(aoiManager.Entries, Is.Empty);
        }
        finally
        {
            moverHandlers.Dispose();
            configManager.Dispose();
            Log.LogLevel = previousLogLevel;
        }
    }

    /// <summary>The client Unit completion factory registers AOI and smoothing under its World.</summary>
    [Test]
    public void ClientUnitFactoryCompletesUnderWorld()
    {
        int previousLogLevel = Log.LogLevel;
        IGameplayPhysicsHandler previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        ConfigManager configManager = new ConfigManager();
        try
        {
            Log.LogLevel = SUPPRESS_INFO_LOG_LEVEL;
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Client();
            configManager.Init();
            registerSingleton(configManager);
            GameplayWorld world = MapHelper.CreateGameplayWorld(this.root.Scene).GetAwaiter().GetResult();
            LogicScene level = LevelSceneFactory.Create(
                world, world, LEVEL_ID, LEVEL_ID, nameof(LEVEL_ID)).GetAwaiter().GetResult();
            Unit unit = UnitHelper.CreateUnit(level, UNIT_CONFIG_ID, UNIT_ID,
                ENetRole.AutonomousProxy, ENetRole.Authority, float3.zero, quaternion.identity,
                true, false);
            AoiManagerComponent aoiManager = world.GetComponent<AoiManagerComponent>();
            Bliss.CharacterControllers.CharacterController character =
                unit.GetComponent<Bliss.CharacterControllers.CharacterController>();
            Assert.That(character, Is.Not.Null);
            Assert.That(character.KinematicCharacterComponent, Is.Not.Null);
            Assert.That(character.KinematicCharacterComponent.Capsule, Is.Not.Null);
            Assert.That(character.MovementComponent, Is.Not.Null);
            Assert.That(world.GetComponent<KinematicMoverManager>().CharacterMotors.Count, Is.EqualTo(EXPECTED_HIT_COUNT));
            Assert.That(unit.GetComponent<MailBoxComponent>(), Is.Not.Null);
            Assert.That(unit.GetComponent<AOIEntityCmp>(), Is.InstanceOf<GamePlayEntity>());
            Assert.That(unit.GetComponent<SmoothTransformComponent>(), Is.InstanceOf<GamePlayEntity>());
            Assert.That(aoiManager.Entries.Count, Is.EqualTo(EXPECTED_HIT_COUNT));
            var input = character.InputIntentProcessPipeline;
            var movement = character.MovementComponent;
            var motor = character.KinematicCharacterComponent;
            var capsule = motor.Capsule;
            character.Dispose();
            Assert.That(input.IsDisposed, Is.True);
            Assert.That(movement.IsDisposed, Is.True);
            Assert.That(motor.IsDisposed, Is.True);
            Assert.That(capsule.IsDisposed, Is.True);
            Assert.That(unit.GetComponent<GameplayRigidbody>(), Is.Null);
            Assert.That(world.GetComponent<KinematicMoverManager>().CharacterMotors, Is.Empty);
            unit.Dispose();
            Assert.That(aoiManager.Entries, Is.Empty);
        }
        finally
        {
            configManager.Dispose();
            GameplayPhysics.GameplayPhysicsHandler = previousPhysics;
            Log.LogLevel = previousLogLevel;
        }
    }

    /// <summary>Client motor simulation uses the latest sampled input only for eligible network roles.</summary>
    [TestCase(ENetRole.AutonomousProxy, ENetRole.Authority, true)]
    [TestCase(ENetRole.Authority, ENetRole.Authority, true)]
    [TestCase(ENetRole.SimulatedProxy, ENetRole.Authority, false)]
    public void ClientCharacterUsesWorldInputAndRoleFilteredTicks(
        ENetRole localRole, ENetRole remoteRole, bool shouldSimulate)
    {
        IGameplayPhysicsHandler previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        long previousOffset = TimeInfo.Instance.ServerFrameTime() - TimeInfo.Instance.FrameTime;
        GameDebugStates debugStates = new GameDebugStates();
        try
        {
            debugStates.Init();
            registerSingleton(debugStates);
            GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Client();
            TimeInfo.Instance.ServerMinusClientTime = INITIAL_TICK_TIME - TimeInfo.Instance.FrameTime;
            GameplayWorld world = GameplayWorldFactory.Create(
                this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
            KinematicMoverManager manager = world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
                new KinematicMoverManagerSettings { AutoSimulation = true, Interpolate = true, InterpolateMotor = true });
            manager.Enable = true;
            Unit unit = UnitHelper.CreateMinimalUnit(world, UNIT_CONFIG_ID, UNIT_ID,
                localRole, remoteRole, float3.zero, quaternion.identity);
            unit.AddComponent<SmoothTransformComponent, SmoothTransformComponentInitParam>(
                new SmoothTransformComponentInitParam(ENetworkSmoothingMode.Linear));
            Bliss.CharacterControllers.CharacterController controller =
                unit.AddComponent<Bliss.CharacterControllers.CharacterController>();
            // This fixture does not start a client session or exercise network transmission.
            controller.MovementComponent.bReplicateMovement = false;
            KinematicCharacterComponent motor = controller.KinematicCharacterComponent;
            motor._solveMovementCollisions = false;
            motor._solveGrounding = false;
            motor.InteractiveRigidbodyHandling = false;
            motor.InitialSimulationPosition = new float3(UNSIMULATED_POSITION, ZERO_AXIS, ZERO_AXIS);
            motor.InitialTickPosition = motor.InitialSimulationPosition;
            TestInputSource input = new TestInputSource();
            controller.InputIntentProcessPipeline.SetInputSource(input);

            Action<Root> earlyUpdate = rootUpdate("EarlyUpdate");
            Action<Root> update = rootUpdate("Update");
            earlyUpdate(this.root);
            input.PressJump = false;
            earlyUpdate(this.root);
            Assert.That(input.SampleCount, Is.EqualTo(TWO_INPUT_SAMPLES));
            Assert.That(controller.RuntimeData.IntentMovement.x, Is.EqualTo(UNIT_AXIS).Within(POSITION_TOLERANCE));
            Assert.That(controller.RuntimeData.WantsToJump, Is.True);

            TimeInfo.Instance.ServerMinusClientTime = FIRST_TICK_TIME - TimeInfo.Instance.FrameTime;
            update(this.root);
            Assert.That(controller.MovementComponent.LogicTimeStamp, Is.EqualTo(GameplayWorldLogicUpdate.STEP_MILLISECONDS));
            if (shouldSimulate)
            {
                Assert.That(motor.InitialSimulationPosition.x, Is.Zero.Within(POSITION_TOLERANCE));
                Assert.That(motor.InitialTickPosition.x, Is.Zero.Within(POSITION_TOLERANCE));
                Assert.That(controller.RuntimeData.JumpConsumed, Is.True);
            }
            else
            {
                Assert.That(motor.InitialSimulationPosition.x, Is.EqualTo(UNSIMULATED_POSITION));
                Assert.That(motor.InitialTickPosition.x, Is.EqualTo(UNSIMULATED_POSITION));
                Assert.That(controller.RuntimeData.JumpConsumed, Is.False);
            }
        }
        finally
        {
            debugStates.Dispose();
            GameplayPhysics.GameplayPhysicsHandler = previousPhysics;
            TimeInfo.Instance.ServerMinusClientTime = previousOffset;
        }
    }

    private static Action<Root> rootUpdate(string name)
    {
        return (Action<Root>)(typeof(Root).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Root.{name} was not found."))
            .CreateDelegate(typeof(Action<Root>));
    }

    private static void registerSingleton(ASingleton singleton)
    {
        MethodInfo register = typeof(ASingleton).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Singleton registration method was not found.");
        register.Invoke(singleton, null);
    }

    /// <summary>Supplies a deterministic movement sample and a single jump edge.</summary>
    private sealed class TestInputSource : IInputSource
    {
        public bool PressJump = true;
        public int SampleCount;

        public float3 GetMovementInput()
        {
            this.SampleCount++;
            return new float3(UNIT_AXIS, ZERO_AXIS, ZERO_AXIS);
        }

        public float3 GetLookInput() => float3.zero;
        public bool GetActionDown(EGameplayInputAction action) => this.PressJump;
        public quaternion GetCameraRotation() => quaternion.identity;

        public void GetActionStatus(List<GameplayInputActionStatus> result)
        {
            if (this.PressJump)
            {
                result.Add(new GameplayInputActionStatus
                {
                    TimStamp = FIRST_INPUT_TIME,
                    Action = EGameplayInputAction.Jump,
                    Status = EInputActionStatus.Down,
                });
            }
        }
    }

    /// <summary>Supplies the external scene-resource preparation boundary for the isolated fixture.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class PrepareLevel : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        protected override async ETTask Run(LogicScene scene, Event_AfterCreateLevelScene data) =>
            await ETTask.CompletedTask;
    }

    /// <summary>Observes the client factory's World-created event without loading assets.</summary>
    [Event(ELogicSceneType.GameplayWorld)]
    public sealed class RecordWorldCreated : AEvent<LogicScene, Event_AfterCreateGameplayWorld>
    {
        /// <summary>Number of World creation events observed by the current test.</summary>
        public static int Count;

        /// <inheritdoc />
        protected override async ETTask Run(LogicScene scene, Event_AfterCreateGameplayWorld data)
        {
            Count++;
            await ETTask.CompletedTask;
        }

        /// <summary>Loads the project's actual generated client configuration files for the fixture.</summary>
        [Invoke(ConfigManager.InvokeType_LoadFile)]
        public sealed class LoadTestConfig : AInvokeHandler<ConfigManager.LoadFile, string>
        {
            /// <summary>Reads the requested configuration file from the Unity project.</summary>
            /// <param name="args">Requested configuration path.</param>
            /// <returns>The configuration JSON.</returns>
            public override string Handle(ConfigManager.LoadFile args) => File.ReadAllText(args.FileName);
        }

        /// <summary>Replaces view-backed rigidbody creation for the asset-free fixture.</summary>
        [Invoke(PhysicsInvokeType.RigidbodyAwake)]
        public sealed class PrepareTestRigidbody : AInvokeHandler<GameplayRigidbodyAwakeParams>
        {
            /// <inheritdoc />
            public override void Handle(GameplayRigidbodyAwakeParams args) { }
        }

        /// <summary>Replaces view-backed rigidbody destruction for the asset-free fixture.</summary>
        [Invoke(PhysicsInvokeType.RigidbodyDestroy)]
        public sealed class ReleaseTestRigidbody : AInvokeHandler<GameplayRigidbodyDestroyParams>
        {
            /// <inheritdoc />
            public override void Handle(GameplayRigidbodyDestroyParams args) { }
        }

        /// <summary>Replaces view-backed capsule creation for the asset-free fixture.</summary>
        [Invoke(PhysicsInvokeType.CapsuleColliderAwake)]
        public sealed class PrepareTestCapsule : AInvokeHandler<GameplayCapsuleColliderAwakeParams>
        {
            /// <inheritdoc />
            public override void Handle(GameplayCapsuleColliderAwakeParams args) { }
        }

        /// <summary>Replaces view-backed capsule destruction for the asset-free fixture.</summary>
        [Invoke(PhysicsInvokeType.CapsuleColliderDestroy)]
        public sealed class ReleaseTestCapsule : AInvokeHandler<GameplayCapsuleColliderDestroyParams>
        {
            /// <inheritdoc />
            public override void Handle(GameplayCapsuleColliderDestroyParams args) { }
        }
    }
}
