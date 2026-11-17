using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Unity.Mathematics;

namespace BlissBenchMark;

/// <summary>
/// Measures AOI registration in sparse and dense worlds at representative entry counts.
/// </summary>
[MemoryDiagnoser]
public class AoiRegistrationBenchmark
{
    private const int SMALL_EXISTING_ENTRY_COUNT = 100;
    private const int LARGE_EXISTING_ENTRY_COUNT = 1000;
    private const int ROOT_ID = 9001;
    private const int ROOT_ZONE = 1;
    private const long FIRST_ENTITY_ID = 1;
    private const int AOI_TAG = 1;
    private const float AOI_RADIUS = 10f;
    private const float SPARSE_ENTITY_SPACING = 100f;
    private const long INVALID_IDENTIFIER = 0;
    private static readonly MethodInfo typeManagerRegisterMethod =
        typeof(TypeManager).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private static readonly MethodInfo entitySystemManagerRegisterMethod =
        typeof(EntitySystemManager).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");
    private static readonly ConstructorInfo rootConstructor = typeof(Root).GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic,
        null,
        new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) },
        null) ?? throw new InvalidOperationException("Root constructor was not found.");

    private TypeManager typeManager = null!;
    private EntitySystemManager entitySystemManager = null!;
    private Root root = null!;
    private AoiManagerComponent aoiManager = null!;
    private AoiRegisterInfo candidateRegisterInfo;
    private long registeredIdentifier;

    /// <summary>Number of entries present before the measured registration.</summary>
    [Params(SMALL_EXISTING_ENTRY_COUNT, LARGE_EXISTING_ENTRY_COUNT)]
    public int ExistingEntryCount { get; set; }

    /// <summary>Whether the existing entries overlap the candidate's visibility area.</summary>
    [Params(false, true)]
    public bool DenseWorld { get; set; }

    /// <summary>Initializes the production entity and update-system registries.</summary>
    [GlobalSetup]
    public void InitializeEntitySystems()
    {
        this.typeManager = new TypeManager();
        this.typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(AoiManagerComponent).Assembly,
            typeof(AoiRegistrationBenchmark).Assembly,
        });
        typeManagerRegisterMethod.Invoke(this.typeManager, null);

        this.entitySystemManager = new EntitySystemManager();
        this.entitySystemManager.Init();
        entitySystemManagerRegisterMethod.Invoke(this.entitySystemManager, null);
    }

    /// <summary>Creates the existing AOI population outside the measured operation.</summary>
    [IterationSetup]
    public void SetupWorld()
    {
        this.root = this.createRoot();
        this.aoiManager = new AoiManagerComponent();
        for (int entityIndex = 0; entityIndex < this.ExistingEntryCount; entityIndex++)
        {
            long entityId = FIRST_ENTITY_ID + entityIndex;
            float3 position = this.DenseWorld
                ? float3.zero
                : new float3((entityIndex + 1) * SPARSE_ENTITY_SPACING, 0f, 0f);
            AoiManagerComponentSystems.Register(this.aoiManager, AoiRegisterInfo.CreateSphere(
                this.createEntity(entityId),
                AOI_TAG,
                position,
                AOI_RADIUS));
        }

        AoiManagerComponentSystems.SetVisibilityCallbacks(
            this.aoiManager,
            onVisibilityChanged,
            onVisibilityChanged);
        this.candidateRegisterInfo = AoiRegisterInfo.CreateSphere(
            this.createEntity(FIRST_ENTITY_ID + this.ExistingEntryCount),
            AOI_TAG,
            float3.zero,
            AOI_RADIUS);
        this.registeredIdentifier = INVALID_IDENTIFIER;
    }

    /// <summary>Releases the measured entry outside the measured operation.</summary>
    [IterationCleanup]
    public void CleanupWorld()
    {
        if (this.registeredIdentifier != INVALID_IDENTIFIER)
        {
            AoiManagerComponentSystems.Unregister(this.aoiManager, this.registeredIdentifier);
        }

        this.aoiManager = null!;
        this.root?.Dispose();
        this.root = null!;
    }

    /// <summary>Releases global registries after all measurements.</summary>
    [GlobalCleanup]
    public void DisposeEntitySystems()
    {
        this.root?.Dispose();
        this.entitySystemManager.Dispose();
        this.typeManager.Dispose();
    }

    /// <summary>Measures adding one AOI entry to the prepared world.</summary>
    [Benchmark]
    public long RegisterOneEntity()
    {
        this.registeredIdentifier =
            AoiManagerComponentSystems.Register(this.aoiManager, this.candidateRegisterInfo);
        return this.registeredIdentifier;
    }

    private AoiRegistrationEntity createEntity(long entityId)
    {
        return this.root.Scene.AddChildWithId<AoiRegistrationEntity>(entityId);
    }

    private Root createRoot()
    {
        return (Root)rootConstructor.Invoke(new object[]
        {
            ROOT_ID,
            ROOT_ZONE,
            ELogicSceneType.Main,
            nameof(AoiRegistrationBenchmark),
        });
    }

    private static void onVisibilityChanged(long observerIdentifier, long targetIdentifier, int targetTag)
    {
    }

    /// <summary>Minimal valid entity reference used by the AOI benchmark.</summary>
    public sealed class AoiRegistrationEntity : Entity
    {
    }
}
