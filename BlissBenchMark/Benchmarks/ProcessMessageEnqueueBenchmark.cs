using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using ET;

namespace BlissBenchMark;

/// <summary>Measures production internal-message admission with representative single and batched workloads.</summary>
[MemoryDiagnoser]
public class ProcessMessageEnqueueBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const int SINGLE_MESSAGE_BATCH_SIZE = 1;
    private const int MULTI_MESSAGE_BATCH_SIZE = 16;
    private const int PROCESS_ID = 1;
    private const int SUPPRESS_LOG_LEVEL = 5;

    private static readonly ConstructorInfo rootConstructor =
        typeof(Root).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) }, null)
        ?? throw new InvalidOperationException("Root constructor was not found.");
    private static readonly MethodInfo registerSingleton =
        typeof(ASingleton).GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Singleton registration method was not found.");

    private readonly List<ProcessMessageInfo> pendingMessages = new List<ProcessMessageInfo>(MULTI_MESSAGE_BATCH_SIZE);
    private readonly PingMessage message = new PingMessage();
    private ProcessMessageManager messageManager = null!;
    private Options options = null!;
    private Root targetRoot = null!;
    private ActorAddress targetAddress;
    private int previousLogLevel;

    /// <summary>Creates a live Root route and prepares reusable message and queue-drain state.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.previousLogLevel = Log.LogLevel;
        Log.LogLevel = SUPPRESS_LOG_LEVEL;
        this.options = new Options { Process = PROCESS_ID };
        registerSingleton.Invoke(this.options, null);
        this.targetRoot = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Map, nameof(ProcessMessageEnqueueBenchmark) });
        this.targetAddress = new ActorAddress(this.targetRoot.RootAddress);
        this.messageManager = new ProcessMessageManager();
        this.messageManager.Register(this.targetRoot);
    }

    /// <summary>Measures one successful enqueue followed by draining the target queue.</summary>
    [Benchmark(Baseline = true)]
    public void EnqueueSingleMessage()
    {
        this.enqueueAndDrain(SINGLE_MESSAGE_BATCH_SIZE);
    }

    /// <summary>Measures a batch of sixteen successful enqueues followed by one queue drain.</summary>
    [Benchmark(OperationsPerInvoke = MULTI_MESSAGE_BATCH_SIZE)]
    public void EnqueueMessageBatch()
    {
        this.enqueueAndDrain(MULTI_MESSAGE_BATCH_SIZE);
    }

    /// <summary>Unregisters the target route and restores the previous logging level.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (this.targetRoot != null)
        {
            this.messageManager.Unregister(this.targetRoot);
            this.targetRoot.Dispose();
        }

        this.messageManager?.Dispose();
        this.options?.Dispose();
        Log.LogLevel = this.previousLogLevel;
    }

    private void enqueueAndDrain(int batchSize)
    {
        for (int messageIndex = 0; messageIndex < batchSize; messageIndex++)
        {
            this.messageManager.Enqueue(this.targetRoot.RootAddress, this.targetAddress, this.message);
        }

        this.messageManager.Fetch(this.targetRoot.Id, this.pendingMessages, batchSize);
        if (this.pendingMessages.Count != batchSize)
        {
            throw new InvalidOperationException("The enqueue benchmark did not drain its complete batch.");
        }

        this.pendingMessages.Clear();
    }
}
