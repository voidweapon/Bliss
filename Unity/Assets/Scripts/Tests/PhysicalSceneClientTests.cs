using System;
using System.Collections.Generic;
using System.Reflection;
using Bliss;
using Bliss.Core;
using Bliss.Physics;
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

    private static void registerSingleton(ASingleton singleton)
    {
        MethodInfo register = typeof(ASingleton).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Singleton registration method was not found.");
        register.Invoke(singleton, null);
    }

    /// <summary>Supplies the external scene-resource preparation boundary for the isolated fixture.</summary>
    [Event(ELogicSceneType.Level)]
    public sealed class PrepareLevel : AEvent<LogicScene, Event_AfterCreateLevelScene>
    {
        protected override async ETTask Run(LogicScene scene, Event_AfterCreateLevelScene data) =>
            await ETTask.CompletedTask;
    }
}
