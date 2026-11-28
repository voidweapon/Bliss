using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Bliss;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Core.Network;
using Bliss.Timer;
using ET;

namespace BlissBenchMark;

/// <summary>Measures cross-process MessageSender RPC and one-way traffic through Main over loopback UDP.</summary>
[MemoryDiagnoser]
public class MainInternalMessageGatewayBenchmark
{
    private const int PROCESS_ID = 1;
    private const int REMOTE_PROCESS_ID = 2;
    private const int ROOT_ID = 101;
    private const int REMOTE_ROOT_ID = 900;
    private const int MAIN_ROOT_ID = 0;
    private const int ROOT_ZONE = 1;
    private const int SINGLE_MESSAGE = 1;
    private const int BATCH_MESSAGES = 16;
    private const int SUPPRESS_LOG_LEVEL = 5;
    private const int RPC_TIMEOUT_MS = 10000;
    private const int ROUND_TRIP_TIMEOUT_SECONDS = 10;
    private const int PUMP_POLL_INTERVAL_MS = 1;
    private const int NO_ERROR = 0;
    private const string LOGIC_ASSEMBLY_NAME = "Logic";
    private const string INTERNAL_ADDRESS_READER = "ReadInternalAddress";
    private const string PAYLOAD_DESERIALIZER = "DeserializePayload";

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

    private readonly List<ETTask<IResponse>> pendingCalls = new();
    private Root mainRoot = null!;
    private Root callerRoot = null!;
    private MessageSender sender = null!;
    private UdpNetDriver peer = null!;
    private string previousDirectory = null!;
    private int peerOneWayMessageCount;
    private int previousLogLevel;

    /// <summary>Number of messages sent in each measured batch.</summary>
    [Params(SINGLE_MESSAGE, BATCH_MESSAGES)]
    public int BatchSize { get; set; }

    /// <summary>Creates Main, caller and UDP peer endpoints and verifies both gateway routes.</summary>
    [GlobalSetup]
    public void Initialize()
    {
        this.previousDirectory = Directory.GetCurrentDirectory();
        this.changeToServerDirectory();
        this.previousLogLevel = Log.LogLevel;
        Log.LogLevel = SUPPRESS_LOG_LEVEL;
        TimeInfo.Instance.Init();
        TimeInfo.Instance.Update();

        World.Instance.AddSingleton<TypeManager, List<Assembly>>(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(Session).Assembly,
            typeof(MessageSender).Assembly,
            Assembly.Load(LOGIC_ASSEMBLY_NAME),
            typeof(MainInternalMessageGatewayBenchmark).Assembly,
        });
        World.Instance.AddSingleton<EntitySystemManager>();
        World.Instance.AddSingleton<EventSystem>();
        MemoryPackSerializerRegister.Init();
        World.Instance.AddSingleton<MessageOpcodeManager>();
        World.Instance.AddSingleton<MessageDispatcher>();
        World.Instance.AddSingletonNoInit<ProcessMessageManager>();
        World.Instance.AddSingleton<LogMsg>();
        World.Instance.AddSingleton<ConfigManager>();

        Options options = new()
        {
            Process = PROCESS_ID,
        };
        typeof(Options).GetProperty("InternalMessageForwardTimeoutMS")?.SetValue(options, RPC_TIMEOUT_MS);
        registerSingleton.Invoke(options, null);

        int[] ports = getAvailableUdpPorts(3);
        this.peer = new UdpNetDriver();
        this.peer.InitServer(new IPEndPoint(IPAddress.Loopback, ports[0]));
        this.peer.OnConnectionReceive += this.onPeerReceive;
        ConfigManager.Instance.Tables.TbServerProcess.DataMap.Add(REMOTE_PROCESS_ID,
            new ServerProcess(JsonDocument.Parse(
                $$"""{"id":{{REMOTE_PROCESS_ID}},"MachineId":1,"Port":{{ports[0]}}}""").RootElement));

        this.mainRoot = (Root)rootConstructor.Invoke(new object[]
            { MAIN_ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(MainInternalMessageGatewayBenchmark) });
        this.addRootComponents(this.mainRoot, ports[1]);

        this.callerRoot = (Root)rootConstructor.Invoke(new object[]
            { ROOT_ID, ROOT_ZONE, ELogicSceneType.Gate, nameof(MainInternalMessageGatewayBenchmark) });
        this.callerRoot.Scene.AddComponent<TimerComponent>();
        this.callerRoot.Scene.AddComponent<ProcessInnerMessageProxy>();
        this.callerRoot.Scene.AddComponent<NetworkComponent, NetworkServerParam>(createNetworkParam(ports[2]));
        this.sender = this.callerRoot.Scene.AddComponent<MessageSender>();

        this.forwardRpcBatch();
        this.forwardOneWayBatch();
    }

    /// <summary>Sends RPC batches through MessageSender and waits for payload responses over UDP.</summary>
    [Benchmark]
    public void RpcRoundTripBatch() => this.forwardRpcBatch();

    /// <summary>Sends one-way batches through MessageSender and verifies UDP receipt.</summary>
    [Benchmark]
    public void OneWayBatch() => this.forwardOneWayBatch();

    /// <summary>Releases both Root network drivers, UDP peer and benchmark registries.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.callerRoot?.Dispose();
        this.mainRoot?.Dispose();
        this.peer?.Dispose();
        World.Instance.Dispose();
        Log.LogLevel = this.previousLogLevel;
        if (this.previousDirectory != null)
        {
            Directory.SetCurrentDirectory(this.previousDirectory);
        }
    }

    private void forwardRpcBatch()
    {
        this.pendingCalls.Clear();
        for (int index = 0; index < this.BatchSize; index++)
        {
            this.pendingCalls.Add(this.sender.Call(new ActorAddress(REMOTE_PROCESS_ID, REMOTE_ROOT_ID),
                new ObjectAddRequest(), RPC_TIMEOUT_MS, needException: false));
        }

        Stopwatch timeout = Stopwatch.StartNew();
        while (this.hasPendingCalls())
        {
            this.pump();
            if (timeout.Elapsed > TimeSpan.FromSeconds(ROUND_TRIP_TIMEOUT_SECONDS))
            {
                throw new TimeoutException("The Main gateway RPC benchmark did not complete its batch.");
            }
            Thread.Sleep(PUMP_POLL_INTERVAL_MS);
        }

        foreach (ETTask<IResponse> call in this.pendingCalls)
        {
            IResponse response = call.GetAwaiter().GetResult();
            if (response is not ObjectAddResponse || response.Error != NO_ERROR)
            {
                throw new InvalidOperationException("The Main gateway RPC benchmark returned an unexpected response.");
            }
        }
    }

    private void forwardOneWayBatch()
    {
        int targetCount = this.peerOneWayMessageCount + this.BatchSize;
        for (int index = 0; index < this.BatchSize; index++)
        {
            this.sender.Send(new ActorAddress(REMOTE_PROCESS_ID, REMOTE_ROOT_ID), new PingMessage
            {
                PacketId = index,
            });
        }

        Stopwatch timeout = Stopwatch.StartNew();
        while (this.peerOneWayMessageCount < targetCount)
        {
            this.pump();
            if (timeout.Elapsed > TimeSpan.FromSeconds(ROUND_TRIP_TIMEOUT_SECONDS))
            {
                throw new TimeoutException("The Main gateway one-way benchmark did not receive its batch.");
            }
            Thread.Sleep(PUMP_POLL_INTERVAL_MS);
        }
    }

    private bool hasPendingCalls()
    {
        foreach (ETTask<IResponse> call in this.pendingCalls)
        {
            if (!call.IsCompleted)
            {
                return true;
            }
        }

        return false;
    }

    private void pump()
    {
        TimeInfo.Instance.Update();
        this.peer.Update();
        earlyUpdate(this.mainRoot);
        update(this.mainRoot);
        earlyUpdate(this.callerRoot);
        update(this.callerRoot);
    }

    private void onPeerReceive(uint connectionId, SocketBufferRef buffer)
    {
        try
        {
            ActorAddress returnAddress = default;
            MethodInfo? readAddress = typeof(MessageSerializer).GetMethod(INTERNAL_ADDRESS_READER,
                BindingFlags.Public | BindingFlags.Static);
            if (readAddress != null)
            {
                returnAddress = (ActorAddress)readAddress.Invoke(null, new object[] { buffer })!;
            }

            MessageObject message = MessageSerializer.NetStreamToMessage(buffer).Item2;
            if (message.GetType() == typeof(PingMessage) || message.GetType().Name == "InternalMessageSendEnvelope")
            {
                this.peerOneWayMessageCount++;
                return;
            }

            if (message is not IRequest request)
            {
                throw new InvalidOperationException("The UDP peer received an unsupported benchmark message.");
            }

            if (message.GetType().Name == "InternalMessageCallEnvelope")
            {
                this.sendGatewayRpcResponse(connectionId, returnAddress, message);
                return;
            }

            IResponse response = MessageOpcodeManager.Instance.CreateResponseBy(
                request.GetType(), request.RpcId, NO_ERROR);
            this.peer.Send(connectionId,
                MessageSerializer.MessageToNetStream(this.peer, (MessageObject)response), true);
        }
        finally
        {
            buffer.Dispose();
        }
    }

    private void sendGatewayRpcResponse(uint connectionId, ActorAddress returnAddress, MessageObject envelope)
    {
        Type envelopeType = envelope.GetType();
        ushort payloadOpcode = (ushort)envelopeType.GetProperty("PayloadOpcode")!.GetValue(envelope)!;
        byte[] payload = (byte[])envelopeType.GetProperty("Payload")!.GetValue(envelope)!;
        MethodInfo deserializePayload = typeof(MessageSerializer).GetMethod(
            PAYLOAD_DESERIALIZER, new[] { typeof(ushort), typeof(byte[]) })
            ?? throw new InvalidOperationException("MessageSerializer.DeserializePayload was not found.");
        MessageObject request = (MessageObject)deserializePayload.Invoke(null, new object[] { payloadOpcode, payload })!;
        IResponse businessResponse = MessageOpcodeManager.Instance.CreateResponseBy(
            request.GetType(), ((IRequest)request).RpcId, NO_ERROR);
        MessageOpcodeManager.Instance.TryGetOpcodeTypeBy(businessResponse.GetType(), out ushort responseOpcode);
        Type envelopeResponseType = typeof(ServerProcess).Assembly.GetType("Bliss.InternalMessageCallResponse")
            ?? throw new InvalidOperationException("InternalMessageCallResponse was not found.");
        MessageObject envelopeResponse = (MessageObject)Activator.CreateInstance(envelopeResponseType)!;
        envelopeResponseType.GetProperty("PayloadOpcode")!.SetValue(envelopeResponse, responseOpcode);
        envelopeResponseType.GetProperty("Payload")!.SetValue(envelopeResponse,
            MessageSerializer.Serialize((MessageObject)businessResponse));
        envelopeResponseType.GetProperty("Error")!.SetValue(envelopeResponse, businessResponse.Error);
        envelopeResponseType.GetProperty("RpcId")!.SetValue(envelopeResponse,
            envelopeType.GetProperty("RpcId")!.GetValue(envelope));

        MethodInfo? addressedSerializer = typeof(MessageSerializer).GetMethod(nameof(MessageSerializer.MessageToNetStream),
            new[] { typeof(ANetDriver), typeof(ActorAddress), typeof(MessageObject) });
        SocketBufferRef responseBuffer = addressedSerializer == null
            ? MessageSerializer.MessageToNetStream(this.peer, envelopeResponse)
            : (SocketBufferRef)addressedSerializer.Invoke(null,
                new object[] { this.peer, returnAddress, envelopeResponse })!;
        this.peer.Send(connectionId, responseBuffer, true);
    }

    private void addRootComponents(Root root, int port)
    {
        root.Scene.AddComponent<NetworkEntityManagerComponent>();
        root.Scene.AddComponent<TimerComponent>();
        root.Scene.AddComponent<ProcessInnerMessageProxy>();
        root.Scene.AddComponent<MessageSender>();
        root.Scene.AddComponent<MailBoxComponent, EMailBoxType>(EMailBoxType.UnOrderedMessage);
        root.Scene.AddComponent<NetworkComponent, NetworkServerParam>(createNetworkParam(port));
    }

    private static NetworkServerParam createNetworkParam(int port)
    {
        return new NetworkServerParam
        {
            BindEndPoint = new IPEndPoint(IPAddress.Loopback, port),
            DriverType = ENetDriverType.Udp,
            UseNetThread = true,
        };
    }

    private void changeToServerDirectory()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "ServerRes", "Configs")))
        {
            directory = directory.Parent;
        }

        if (directory == null)
        {
            throw new DirectoryNotFoundException("ServerRes/Configs was not found.");
        }

        Directory.SetCurrentDirectory(Path.Combine(directory.FullName, "Server"));
    }

    private static int[] getAvailableUdpPorts(int count)
    {
        List<Socket> reservations = new(count);
        int[] ports = new int[count];
        try
        {
            for (int index = 0; index < count; index++)
            {
                Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                reservations.Add(socket);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                ports[index] = ((IPEndPoint)socket.LocalEndPoint!).Port;
            }

            return ports;
        }
        finally
        {
            foreach (Socket socket in reservations)
            {
                socket.Dispose();
            }
        }
    }
}
