using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Core.Network;
using ET;

namespace BlissBenchMark;

/// <summary>Measures real UDP traffic through the ready Gate session-message route.</summary>
[MemoryDiagnoser]
public class ExternalMessageIngressBenchmark
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const int SUPPRESS_LOG_LEVEL = 5;
    private const int SINGLE_MESSAGE = 1;
    private const int BATCH_MESSAGES = 16;
    private const int ROUND_TRIP_TIMEOUT_SECONDS = 5;
    private readonly Action<Root> earlyUpdate =
        (typeof(Root).GetMethod("EarlyUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Root.EarlyUpdate was not found."))
        .CreateDelegate<Action<Root>>();
    private Root root = null!;
    private UdpNetDriver client = null!;
    private uint connectionId;
    private int receivedCount;
    private int previousLogLevel;

    /// <summary>Number of reliable messages sent in each measured batch.</summary>
    [Params(SINGLE_MESSAGE, BATCH_MESSAGES)]
    public int BatchSize { get; set; }

    /// <summary>Creates a Gate route and verifies a UDP round trip before measuring.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.previousLogLevel = Log.LogLevel;
        Log.LogLevel = SUPPRESS_LOG_LEVEL;
        TimeInfo.Instance.Init();
        TimeInfo.Instance.Update();
        TypeManager types = World.Instance.AddSingleton<TypeManager, List<Assembly>>(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(Session).Assembly,
            typeof(InvokeHandler_OnReceiveMessage_Gate).Assembly,
            typeof(ExternalMessageIngressBenchmark).Assembly,
        });
        types.GetTypes(typeof(EventAttribute)).Clear();
        types.GetTypes(typeof(InvokeAttribute)).RemoveWhere(type => type != typeof(InvokeHandler_OnReceiveMessage_Gate));
        types.GetTypes(typeof(SessionMessageHandlerAttribute)).RemoveWhere(type => type != typeof(BenchmarkExternalPingHandler));
        World.Instance.AddSingleton<EntitySystemManager>();
        World.Instance.AddSingleton<EventSystem>();
        MemoryPackSerializerRegister.Init();
        World.Instance.AddSingleton<MessageOpcodeManager>();
        World.Instance.AddSingleton<SessionMessageDispatcher>();
        World.Instance.AddSingleton<LogMsg>();

        int port;
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            port = ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        ConstructorInfo constructor = typeof(Root).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(int), typeof(int), typeof(ELogicSceneType), typeof(string) }, null)
            ?? throw new InvalidOperationException("Root constructor was not found.");
        this.root = (Root)constructor.Invoke(new object[] { ROOT_ID, ROOT_ZONE, ELogicSceneType.Gate, "benchmark" });
        this.root.Scene.AddComponent<NetworkComponent, NetworkServerParam>(new NetworkServerParam
        {
            BindEndPoint = new IPEndPoint(IPAddress.Loopback, port),
            DriverType = ENetDriverType.Udp,
            UseNetThread = true,
        });
        this.client = new UdpNetDriver();
        this.client.InitClient(AddressFamily.InterNetwork);
        this.client.OnConnectionReceive += this.onReceive;
        this.connectionId = this.client.CreateLocalConnectionId();
        this.client.CreateRemoteConnection(this.connectionId, new IPEndPoint(IPAddress.Loopback, port));
        this.RoundTrip();
    }

    /// <summary>Sends a batch and waits until every production-routed response reaches the UDP peer.</summary>
    [Benchmark]
    public void RoundTrip()
    {
        int target = this.receivedCount + this.BatchSize;
        for (int index = 0; index < this.BatchSize; index++)
        {
            this.client.Send(this.connectionId,
                MessageSerializer.MessageToNetStream(this.client, new PingMessage { PacketId = index }), true);
        }

        long start = Stopwatch.GetTimestamp();
        while (this.receivedCount < target)
        {
            TimeInfo.Instance.Update();
            this.client.Update();
            this.earlyUpdate(this.root);
            if (Stopwatch.GetElapsedTime(start) > TimeSpan.FromSeconds(ROUND_TRIP_TIMEOUT_SECONDS))
            {
                throw new TimeoutException("The UDP benchmark did not receive every response.");
            }
            Thread.Sleep(1);
        }
    }

    /// <summary>Stops the owned network thread and releases the benchmark registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.root?.Dispose();
        this.client?.Dispose();
        World.Instance.Dispose();
        Log.LogLevel = this.previousLogLevel;
    }

    private void onReceive(uint connectionId, SocketBufferRef buffer)
    {
        try
        {
            if (MessageSerializer.NetStreamToMessage(buffer).Item2 is not PongMessage)
            {
                throw new InvalidOperationException("The Gate returned an unexpected message.");
            }
            this.receivedCount++;
        }
        finally
        {
            buffer.Dispose();
        }
    }
}

/// <summary>Responds to benchmark pings through the production session dispatcher.</summary>
[SessionMessageHandler(ELogicSceneType.Gate)]
public sealed class BenchmarkExternalPingHandler : SessionMessageHandler<PingMessage>
{
    /// <inheritdoc />
    protected override async ETTask Run(Session session, PingMessage message)
    {
        session.Send(new PongMessage { PacketId = message.PacketId });
        await ETTask.CompletedTask;
    }
}
