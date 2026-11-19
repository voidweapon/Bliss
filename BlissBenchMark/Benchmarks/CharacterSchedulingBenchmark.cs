using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.CharacterControllers;
using Bliss.CharacterMovement;
using Bliss.Core;
using Bliss.Physics;
using MagicPhysX;
using Unity.Mathematics;
using static MagicPhysX.NativeMethods;

namespace BlissBenchMark;

/// <summary>Measures real Root-driven character input and movement scheduling without collision simulation.</summary>
[MemoryDiagnoser]
public class CharacterSchedulingBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long WORLD_ID = 202;
    private const long WORLD_INSTANCE_ID = 10001;
    private const int SMALL_COUNT = 100;
    private const int LARGE_COUNT = 1000;
    private const int UNIT_CONFIG_ID = 1;
    private const long FIRST_UNIT_ID = 1;
    private const long FIRST_TRANSIENT_UNIT_ID = 100001;
    private const long INITIAL_TIME = 1000;
    private const long TICK_MILLISECONDS = 50;
    private const long NO_SERVER_OFFSET = 0;
    private const float UNIT_AXIS = 1f;
    private const float POSITION_TOLERANCE = 0.0001f;
    private static readonly ConstructorInfo rootConstructor =
        typeof(Root).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) }, null)
        ?? throw new InvalidOperationException("Root constructor was not found.");
    private static readonly MethodInfo registerSingleton =
        typeof(ASingleton).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private static readonly PropertyInfo frameTime =
        typeof(TimeInfo).GetProperty(nameof(TimeInfo.FrameTime))
        ?? throw new InvalidOperationException("FrameTime was not found.");
    private static readonly Action<Root> earlyUpdate =
        (typeof(Root).GetMethod("EarlyUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.EarlyUpdate was not found."))
        .CreateDelegate<Action<Root>>();
    private static readonly Action<Root> update =
        (typeof(Root).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.Update was not found."))
        .CreateDelegate<Action<Root>>();
    private readonly List<CharacterController> controllers = new();
    private readonly BenchmarkInputSource input = new();
    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private EventSystem eventSystem = null!;
    private Root root = null!;
    private GameplayWorld world = null!;
    private PhysicalScene physicalScene = null!;
    private KinematicMoverManager manager = null!;
    private IGameplayPhysicsHandler previousPhysics = null!;

    /// <summary>Number of characters processed in each frame.</summary>
    [Params(SMALL_COUNT, LARGE_COUNT)]
    public int CharacterCount { get; set; }

    /// <summary>Initializes production systems and verifies that the measured work actually runs.</summary>
    [GlobalSetup]
    public unsafe void Initialize()
    {
        this.previousPhysics = GameplayPhysics.GameplayPhysicsHandler;
        GameplayPhysics.GameplayPhysicsHandler = new GameplayPhysicsHandler_Server();
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorld).Assembly,
            typeof(CharacterControllerSystems).Assembly,
            typeof(CharacterSchedulingBenchmark).Assembly,
        });
        registerSingleton.Invoke(this.typeManager, null);
        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        registerSingleton.Invoke(this.entitySystemManager, null);
        this.eventSystem = new EventSystem();
        this.eventSystem.Init();
        registerSingleton.Invoke(this.eventSystem, null);
        this.Setup();
        this.Frame();
        if (this.input.ReadCount != this.CharacterCount)
        {
            throw new InvalidOperationException("The Root did not collect every character's input.");
        }

        foreach (CharacterController controller in this.controllers)
        {
            if (controller.MovementComponent.LogicTimeStamp != TICK_MILLISECONDS)
            {
                throw new InvalidOperationException("The Root did not advance every character's movement timestamp.");
            }
        }
        this.CreateAndDispose();
        if (this.physicalScene.RigidbodyDict.Keys.Count != this.CharacterCount ||
            this.physicalScene.ColliderDict.Keys.Count != this.CharacterCount)
        {
            throw new InvalidOperationException("The transient characters did not release their PhysX mappings.");
        }
        this.Setup();
        this.FrameWithMotors();
        foreach (CharacterController controller in this.controllers)
        {
            float3 position = controller.Parent.GetComponent<TransformComponent>().Transform.Location;
            PxVec3 nativePosition = PxRigidActor_getGlobalPose((PxRigidActor*)controller.OwnedRigidbody.RigidBodyPtr).p;
            if (position.y <= 0f || math.abs(nativePosition.y - position.y) > POSITION_TOLERANCE)
            {
                throw new InvalidOperationException("The motor did not move its native body.");
            }
        }
    }

    /// <summary>Creates characters and prepares a due frame outside the measurement.</summary>
    [IterationSetup]
    public void Setup()
    {
        this.root?.Dispose();
        this.controllers.Clear();
        this.input.ReadCount = 0;
        this.input.Move = false;
        TimeInfo.Instance.ServerMinusClientTime = NO_SERVER_OFFSET;
        frameTime.SetValue(TimeInfo.Instance, INITIAL_TIME);
        this.root = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(CharacterSchedulingBenchmark) });
        PhysicalWorldComponent physicalWorld = this.root.Scene.AddComponent<PhysicalWorldComponent>();
        this.world = GameplayWorldFactory.Create(
            this.root.Scene, WORLD_ID, WORLD_INSTANCE_ID, nameof(GameplayWorld));
        this.physicalScene = this.world.AddComponent<PhysicalScene, PhysicalWorldComponent>(physicalWorld);
        this.manager = this.world.AddComponent<KinematicMoverManager, KinematicMoverManagerSettings>(
            new KinematicMoverManagerSettings { AutoSimulation = true });
        for (int index = 0; index < this.CharacterCount; index++)
        {
            Unit unit = UnitHelper.CreateMinimalUnit(this.world, UNIT_CONFIG_ID, FIRST_UNIT_ID + index,
                ENetRole.Authority, ENetRole.Authority, float3.zero, quaternion.identity);
            CharacterController controller = unit.AddComponent<CharacterController>();
            controller.KinematicCharacterComponent._solveMovementCollisions = false;
            controller.KinematicCharacterComponent._solveGrounding = false;
            controller.KinematicCharacterComponent.InteractiveRigidbodyHandling = false;
            controller.InputIntentProcessPipeline.SetInputSource(this.input);
            this.controllers.Add(controller);
        }

        frameTime.SetValue(TimeInfo.Instance, INITIAL_TIME + TICK_MILLISECONDS);
    }

    /// <summary>Runs input collection and a due World tick through the Root.</summary>
    [Benchmark]
    public void Frame()
    {
        earlyUpdate(this.root);
        update(this.root);
    }

    /// <summary>Runs input and fixed-step motors, including native pose synchronization, without collision solving.</summary>
    [Benchmark]
    public void FrameWithMotors()
    {
        this.manager.Enable = true;
        this.input.Move = true;
        earlyUpdate(this.root);
        update(this.root);
    }

    /// <summary>Creates and destroys real PhysX-backed characters sequentially alongside existing characters.</summary>
    [Benchmark]
    public void CreateAndDispose()
    {
        for (int index = 0; index < this.CharacterCount; index++)
        {
            Unit unit = UnitHelper.CreateMinimalUnit(this.world, UNIT_CONFIG_ID, FIRST_TRANSIENT_UNIT_ID + index,
                ENetRole.Authority, ENetRole.Authority, float3.zero, quaternion.identity);
            unit.AddComponent<CharacterController>();
            unit.Dispose();
        }
    }

    /// <summary>Releases gameplay entities and restores singleton state.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.root?.Dispose();
        this.eventSystem.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
        GameplayPhysics.GameplayPhysicsHandler = this.previousPhysics;
        TimeInfo.Instance.ServerMinusClientTime = NO_SERVER_OFFSET;
    }

    /// <summary>Supplies deterministic input without depending on Unity input devices.</summary>
    public sealed class BenchmarkInputSource : IInputSource
    {
        /// <summary>Number of movement-input reads.</summary>
        public int ReadCount;
        /// <summary>Whether to provide a movement input.</summary>
        public bool Move;

        /// <inheritdoc />
        public float3 GetMovementInput()
        {
            this.ReadCount++;
            return this.Move ? new float3(UNIT_AXIS, 0f, 0f) : float3.zero;
        }

        /// <inheritdoc />
        public float3 GetLookInput() => float3.zero;

        /// <inheritdoc />
        public bool GetActionDown(EGameplayInputAction action) => false;

        /// <inheritdoc />
        public void GetActionStatus(List<GameplayInputActionStatus> result)
        {
            if (this.Move)
            {
                result.Add(new GameplayInputActionStatus
                {
                    TimStamp = INITIAL_TIME,
                    Action = EGameplayInputAction.Jump,
                    Status = EInputActionStatus.Down,
                });
            }
        }

        /// <inheritdoc />
        public quaternion GetCameraRotation() => quaternion.identity;
    }

}
