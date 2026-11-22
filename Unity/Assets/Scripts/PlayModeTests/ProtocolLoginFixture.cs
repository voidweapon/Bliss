using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Bliss;
using Bliss.Core;
using Bliss.Core.EventSystem;
using Bliss.Core.Network;
using ET;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

internal static class ProtocolLoginFixture
{
    internal const int REALM_PORT = 30300;
    private const long GATE_TOKEN = 112233;
    private const long PLAYER_ID = 12345;
    private const string ACCOUNT = "protocol-test";
    private const string PASSWORD = "test-password";
    private const float TIMEOUT_SECONDS = 15f;
    private static LogicScene clientScene;
    private static int loginEvents;
    private static RunResources activeRun;

    internal static IEnumerator RunRejectedLogin()
    {
        return RunLogin(true);
    }

    internal static IEnumerator RunSuccessfulLogin()
    {
        return RunLogin(false);
    }

    private static IEnumerator RunLogin(bool rejectGate)
    {
        FieldInfo worldField = typeof(World).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("World instance field was not found.");
        if (worldField.GetValue(null) != null || UnityEngine.Object.FindObjectsByType<Init>(FindObjectsSortMode.None).Length != 0)
        {
            throw new InvalidOperationException("An active World or production Init already exists; the test cannot take ownership.");
        }

        RunResources resources = new RunResources();
        activeRun = resources;
        UdpNetDriver realm;
        UdpNetDriver gate;
        World world;
        ETCancellationToken cancellation = resources.cancellation;
        ETTask login = null;
        Thread networkThread = null;
        Session session = null;
        try
        {
            resources.cleanupObject = new GameObject("ProtocolPlayModeCleanup");
            resources.cleanupObject.AddComponent<ProtocolFixtureCleanup>().resources = resources;
            Log.Logger = new UnityLogger();
            realm = new UdpNetDriver();
            resources.realm = realm;
            realm.InitServer(new IPEndPoint(IPAddress.Loopback, REALM_PORT));
            int gatePort;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                gatePort = ((IPEndPoint)socket.LocalEndPoint).Port;
            }
            gate = new UdpNetDriver();
            resources.gate = gate;
            gate.InitServer(new IPEndPoint(IPAddress.Loopback, gatePort));

            bool realmReceived = false;
            bool gateReceived = false;
            UdpNetDriver realmDriver = realm;
            UdpNetDriver gateDriver = gate;
            // The callbacks are owned by these drivers and capture only this test run's state.
            realm.OnConnectionReceive += (connectionId, buffer) =>
            {
                try
                {
                    var (_, message) = MessageSerializer.NetStreamToMessage(buffer);
                    C2R_Login request = message as C2R_Login
                        ?? throw new InvalidOperationException($"Unexpected Realm message: {message.GetType()}");
                    Assert.That(request.Account, Is.EqualTo(ACCOUNT));
                    Assert.That(request.Password, Is.EqualTo(PASSWORD));
                    realmReceived = true;
                    realmDriver.Send(connectionId, MessageSerializer.MessageToNetStream(realmDriver, new R2C_Login
                    {
                        RpcId = request.RpcId,
                        GateToken = GATE_TOKEN,
                        GateAddress = $"127.0.0.1:{gatePort}",
                    }), true);
                }
                finally
                {
                    buffer.Dispose();
                }
            };
            gate.OnConnectionReceive += (connectionId, buffer) =>
            {
                try
                {
                    var (_, message) = MessageSerializer.NetStreamToMessage(buffer);
                    C2G_Login request = message as C2G_Login
                        ?? throw new InvalidOperationException($"Unexpected Gate message: {message.GetType()}");
                    Assert.That(request.GateToken, Is.EqualTo(GATE_TOKEN));
                    gateReceived = true;
                    gateDriver.Send(connectionId, MessageSerializer.MessageToNetStream(gateDriver, new G2C_Login
                    {
                        RpcId = request.RpcId,
                        PlayerId = PLAYER_ID,
                        Error = rejectGate ? ErrorCode.ERR_CONNECT_GATE_KEY_ERROR : ErrorCode.ERR_Success,
                    }), true);
                }
                finally
                {
                    buffer.Dispose();
                }
            };

            world = World.Instance;
            resources.world = world;
            world.AddSingleton<RootManager>();
            world.AddSingleton(new Options { AppName = "ProtocolPlayModeTests" });
            TimeInfo.Instance.Init();
            TimeInfo.Instance.Update();
            TypeManager types = world.AddSingleton<TypeManager, List<Assembly>>(new List<Assembly>
            {
                typeof(Entity).Assembly,
                typeof(Player).Assembly,
                typeof(LoginHelper).Assembly,
                typeof(ProtocolLoginFixture).Assembly,
            });
            types.GetTypes(typeof(EventAttribute)).RemoveWhere(type => type != typeof(ProtocolLoginEvent));
            types.GetTypes(typeof(InvokeAttribute)).RemoveWhere(type =>
                type != typeof(ProtocolRootInit) && type != typeof(InvokeHandler_OnReceiveMessage));
            world.AddSingleton<EntitySystemManager>();
            world.AddSingleton<Bliss.EventSystem>();
            MemoryPackSerializerRegister.Init();
            world.AddSingleton<MessageOpcodeManager>();
            world.AddSingleton<LogMsg>();
            world.AddSingleton<GameNetworkManager>();
            clientScene = null;
            loginEvents = 0;
            ETTask<int> create = RootManager.Instance.Create(1, ELogicSceneType.Client, "ProtocolTest");
            Assert.That(create.IsCompleted, Is.True);
            create.GetResult();
            Assert.That(clientScene, Is.Not.Null);
            NetworkComponent network = clientScene.AddComponent<NetworkComponent, NetworkClientParam>(new NetworkClientParam
            {
                AddressFamily = AddressFamily.InterNetwork,
                DriverType = ENetDriverType.Udp,
                UseNetThread = true,
            });
            networkThread = network.NetThread;
            resources.networkThread = networkThread;
            if (rejectGate)
            {
                LogAssert.Expect(LogType.Error, new Regex("登录失败: " + ErrorCode.ERR_CONNECT_GATE_KEY_ERROR));
            }
            login = LoginHelper.Login(clientScene, ACCOUNT, PASSWORD, cancellation);
            resources.login = login;
            float deadline = Time.realtimeSinceStartup + TIMEOUT_SECONDS;
            while (!login.IsCompleted && Time.realtimeSinceStartup < deadline)
            {
                realm.Update();
                gate.Update();
                world.EarlyUpdate();
                world.Update();
                world.LateUpdate();
                yield return null;
            }
            Assert.That(login.IsCompleted, Is.True, "Login did not finish before the deadline.");
            login.GetResult();
            Assert.That(realmReceived, Is.True);
            Assert.That(gateReceived, Is.True);
            if (rejectGate)
            {
                Assert.That(clientScene.GetComponent<ClientSessionComponent>(), Is.Null);
                Assert.That(clientScene.Children.Values.OfType<Player>(), Is.Empty);
                Assert.That(network.ChildCount, Is.Zero);
                Assert.That(loginEvents, Is.Zero);
            }
            else
            {
                Assert.That(clientScene.GetComponent<ClientSessionComponent>(), Is.Not.Null);
                session = (Session)clientScene.GetComponent<ClientSessionComponent>().Session;
                Assert.That(session, Is.Not.Null, "The client session reference must be valid.");
                Assert.That(session.IsDisposed, Is.False, "The client session must still be live.");
                Assert.That(clientScene.Children.Values.OfType<Player>().Single().Account, Is.EqualTo(ACCOUNT));
                Assert.That(loginEvents, Is.EqualTo(1));
            }
        }
        finally
        {
            resources.Dispose();
        }
        Assert.That(networkThread.IsAlive, Is.False, "The client network thread did not stop.");
        if (session != null)
        {
            Assert.That(session.IsDisposed, Is.True);
        }
    }

    internal static void DisposeActiveRun()
    {
        activeRun?.Dispose();
    }

    private static void Cleanup(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            // Keep releasing the remaining owned resources without masking a test failure.
            Debug.LogException(exception);
        }
    }

    internal sealed class RunResources : IDisposable
    {
        internal readonly ETCancellationToken cancellation = new ETCancellationToken();
        internal readonly Bliss.ILogger previousLogger = Log.Logger;
        internal UdpNetDriver realm;
        internal UdpNetDriver gate;
        internal World world;
        internal ETTask login;
        internal Thread networkThread;
        internal GameObject cleanupObject;
        private bool disposed;

        /// <summary>Releases only resources owned by this test run.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            if (ReferenceEquals(activeRun, this))
            {
                activeRun = null;
            }
            if (login != null && !login.IsCompleted) Cleanup(cancellation.Cancel);
            if (world != null) Cleanup(world.Dispose);
            if (gate != null) Cleanup(gate.Dispose);
            if (realm != null) Cleanup(realm.Dispose);
            if (networkThread != null && networkThread.IsAlive)
            {
                Debug.LogError("The protocol test network thread did not stop.");
            }
            if (cleanupObject != null) Cleanup(DestroyCleanupObject);
            Log.Logger = previousLogger;
            clientScene = null;
        }

        private void DestroyCleanupObject()
        {
            UnityEngine.Object.DestroyImmediate(cleanupObject);
        }
    }

    /// <summary>Creates the isolated client Root without running the production boot chain.</summary>
    [Invoke((long)ELogicSceneType.Client)]
    public class ProtocolRootInit : AInvokeHandler<Invoke_RootInit, ETTask>
    {
        /// <summary>Captures the client scene for the fixture.</summary>
        public override ETTask Handle(Invoke_RootInit args)
        {
            clientScene = args.Root.Scene;
            ETTask result = ETTask.Create();
            result.SetResult();
            return result;
        }
    }

    internal sealed class ProtocolFixtureCleanup : MonoBehaviour
    {
        internal ProtocolLoginFixture.RunResources resources;

        private void OnDestroy()
        {
            if (resources != null)
            {
                resources.cleanupObject = null;
                resources.Dispose();
            }
        }
    }

    /// <summary>Observes the real login-success event without loading production UI.</summary>
    [Event(ELogicSceneType.Client)]
    public class ProtocolLoginEvent : AEvent<LogicScene, Event_LoginSuccess>
    {
        protected override async ETTask Run(LogicScene scene, Event_LoginSuccess eventInfo)
        {
            loginEvents++;
            await ETTask.CompletedTask;
        }
    }
}
