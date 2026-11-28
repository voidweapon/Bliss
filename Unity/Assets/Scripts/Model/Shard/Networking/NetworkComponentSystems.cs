using System;
using System.Net;
using System.Threading;
using Bliss.Core;
using Bliss.Core.Network;

namespace Bliss
{
    [EntitySystemOf(typeof(NetworkComponent))]
    public static partial class NetworkComponentSystem
    {
        private const int MaxProcessOpPerTick = 10000000;
        
        #region Entity Systems

        private static void Awake(this NetworkComponent self, NetworkClientParam param1)
        {
            self.OwnerRoot = self.Root();
            NetworkProfiler.Create();
            self.IsStop = false;
            self.NetDriver = self.CreateNetDriver(param1.DriverType);
            self.NetDriver.InitClient(param1.AddressFamily);
            self.InitNetDriver(self.NetDriver);

            if (param1.UseNetThread)
            {
                self.NetThread = new Thread(self.NetThreadUpdate);
                self.NetThread.Start();
            }
        }
        
        private static void Awake(this NetworkComponent self, NetworkServerParam param1)
        {
            self.OwnerRoot = self.Root();
            if (self.LogicScene.SceneType == ELogicSceneType.Gate || self.LogicScene.SceneType == ELogicSceneType.Realm
                || self.LogicScene.SceneType == ELogicSceneType.Main)
            {
                RootInitializationGateComponent gate =
                    self.RootScene().GetComponent<RootInitializationGateComponent>();
                gate?.AllowSystem(typeof(NetworkComponent_EarlyUpdateSystem));
                if (self.LogicScene.SceneType == ELogicSceneType.Main)
                {
                    gate?.AllowSystemAfterFailure(typeof(NetworkComponent_EarlyUpdateSystem));
                }
            }

            NetworkProfiler.Create();
            self.IsStop = false;
            self.NetDriver = self.CreateNetDriver(param1.DriverType);
            self.NetDriver.InitServer(param1.BindEndPoint);
            self.InitNetDriver(self.NetDriver);
            if (param1.UseNetThread)
            {
                self.NetThread = new Thread(self.NetThreadUpdate);
                self.NetThread.Start();
            }
        }
        
        private static void Destroy(this NetworkComponent self)
        {
            self.IsStop = true;
            self.NetThread.Join(1000);
            if (self.NetDriver != null)
            {
                self.NetDriver.OnAcceptConnection -= self.OnNetThreadAcceptConnection;
                self.NetDriver.OnConnectionReceive -= self.OnNetThreadConnectionReceive;
                self.NetDriver.OnConnectionError -= self.OnNetThreadConnectionError;
            }
            self.NetDriver = null;
            NetworkProfiler.Instance?.Dispose();
        }
        
        private static void EarlyUpdate(this NetworkComponent self)
        {
            self.ProcessMainThreadOperation();
        }
        
        private static ANetDriver CreateNetDriver(this NetworkComponent self, ENetDriverType  driverType)
        {
            switch (driverType)
            {
                case ENetDriverType.Tcp:
                {
                    return new TcpNetDriver();
                }
                case ENetDriverType.Udp:
                {
                    return new UdpNetDriver();
                }
                case ENetDriverType.PrimarySecondaryIp:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private static void InitNetDriver(this NetworkComponent self, ANetDriver netDriver)
        {
            netDriver.OnAcceptConnection += self.OnNetThreadAcceptConnection;
            netDriver.OnConnectionReceive += self.OnNetThreadConnectionReceive;
            netDriver.OnConnectionError += self.OnNetThreadConnectionError;
        }
        
        #endregion

        #region Main Thread

        public static Session CreateConnection(this NetworkComponent self, IPEndPoint remoteEndPoint)
        {
            if (!self.CheckDispose())
            {
                return null;
            }
            
            uint connectionId = self.NetDriver.CreateLocalConnectionId();
            var session = self.AddChildWithId<Session, NetworkComponent>(connectionId, self);
            session.RemoteEndPoint = remoteEndPoint;
            session.IsInternal = self.LogicScene.SceneType == ELogicSceneType.Main;
            
            self.NetThreadOpQueue.Enqueue(new NetThreadOperation()
            {
                OpType = ENetThreadOpType.CreateConnection,
                ConnectionId = connectionId,
                RemoteEndPoint = remoteEndPoint
            });
            
             return session;
        }
        
        public static void RemoveConnection(this NetworkComponent self, uint connectionId, int errorCode)
        {
            if (!self.CheckDispose())
            {
                return;
            }
            
            self.NetThreadOpQueue.Enqueue(new NetThreadOperation()
            {
                OpType = ENetThreadOpType.DestroyConnection,
                ConnectionId = connectionId,
                ErrorCode = errorCode,
            });
        }
        
        public static void Send(this NetworkComponent self, uint connectionId, SocketBufferRef bufferRef)
        {
            if (!self.CheckDispose())
            {
                return;
            }
            
            self.NetThreadOpQueue.Enqueue(new NetThreadOperation()
            {
                OpType = ENetThreadOpType.SendMessage,
                ConnectionId = connectionId,
                BufferRef = bufferRef,
            });
        }

        public static void Send(this NetworkComponent self, uint connectionId, SocketBufferRef bufferRef, bool isReliable)
        {
            if (!self.CheckDispose())
            {
                return;
            }
            
            self.NetThreadOpQueue.Enqueue(new NetThreadOperation()
            {
                OpType = ENetThreadOpType.SendMessage,
                ConnectionId = connectionId,
                BufferRef = bufferRef,
                IsReliable = isReliable,
            });
        }

        private static void OnAcceptConnection(this NetworkComponent self, uint connectionId, IPEndPoint remoteEndPoint)
        {
            var session = self.AddChildWithId<Session, NetworkComponent>(connectionId, self);
            session.RemoteEndPoint = remoteEndPoint;
            session.IsInternal = self.LogicScene.SceneType == ELogicSceneType.Main;
            
            EventSystem.Instance.Push(self.LogicScene, new Event_OnAcceiptConnection()
            {
                Session = session,
            });
        }
        
        private static void OnConnectionError(this NetworkComponent self, uint connectionId, int errorCode)
        {
            Session session = self.GetChild<Session>(connectionId);
            if (session == null)
            {
                return;
            }
            
            session.ErrorCode = errorCode;
            session.Dispose();
        }

        private static void OnReceive(this NetworkComponent self, uint connectionId, SocketBufferRef bufferRef,
            bool receivedBeforeRootReady, RootInitializationMessagePolicy initializationMessagePolicy,
            RootMessageAdmission targetAdmission)
        {
            Session session = self.GetChild<Session>(connectionId);
            if (session == null)
            {
                bufferRef.Dispose();
                return;
            }
            
            //解析消息
            MessageObject message = null;
            ActorAddress targetAddress = default;
            try
            {
                if (session.IsInternal)
                {
                    targetAddress = MessageSerializer.ReadInternalAddress(bufferRef);
                }
                (ushort _, MessageObject msg) = MessageSerializer.NetStreamToMessage(bufferRef);
                message = msg;
            }
            catch (Exception e)
            {
                Log.Error($"解析消息失败:{e}");
                return;
            }
            finally
            {
                bufferRef.Dispose();
            }
            
            LogMsg.Instance.Debug(self, message);
            
            //分发消息
            EventSystem.Instance.Invoke((long)self.LogicScene.SceneType, new Invoke_OnReceiveMessage()
            {
                Session = session,
                TargetAddress = targetAddress,
                Message = message,
                ReceivedBeforeRootReady = receivedBeforeRootReady,
                InitializationMessagePolicy = initializationMessagePolicy,
                TargetAdmission = targetAdmission,
            });
        }
        
        private static void ProcessMainThreadOperation(this NetworkComponent self)
        {
            int processCount = 0;
            while (self.MainThreadOpQueue.TryDequeue(out MainThreadOperations operation))
            {
                switch (operation.OpType)
                {
                    case EMainTreadOpType.AcceptConnection:
                        self.OnAcceptConnection(operation.ConnectionId, operation.RemoteEndPoint);
                        break;
                    case EMainTreadOpType.OnReceiveMessage:
                        self.OnReceive(operation.ConnectionId, operation.BufferRef, operation.ReceivedBeforeRootReady,
                            operation.InitializationMessagePolicy, operation.TargetAdmission);
                        break;
                    case EMainTreadOpType.OnConnectionError:
                        self.OnConnectionError(operation.ConnectionId, operation.ErrorCode);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                if (++processCount > MaxProcessOpPerTick)
                {
                    Log.Warning($"Processed the limit of {MaxProcessOpPerTick} message this tick");
                    break;
                }
            }
        }

        private static bool CheckDispose(this NetworkComponent self)
        {
            if (self.IsStop || self.IsDisposed || self.NetThread == null)
            {
                return false;
            }
            return true;
        }
        #endregion

        #region Net Thread
        private static void NetThreadUpdate(this NetworkComponent self)
        {
            while (true)
            {
                if (self.IsStop)
                {
                    break;
                }
                
                if (self.NetDriver != null)
                {
                    self.ProcessNetThreadOperation();
                    self.NetDriver.Update();
                }
 
                Thread.Sleep(1);
            }

            if (self.NetDriver != null)
            {
                self.NetDriver.Dispose();
            }
        }

        private static void ProcessNetThreadOperation(this NetworkComponent self)
        {
            while (self.NetThreadOpQueue.TryDequeue(out var netThreadOperation))
            {
                switch (netThreadOperation.OpType)
                {
                    case ENetThreadOpType.CreateConnection: 
                        self.NetDriver.CreateRemoteConnection(netThreadOperation.ConnectionId, netThreadOperation.RemoteEndPoint);
                        break;
                    case ENetThreadOpType.DestroyConnection:
                        self.NetDriver.RemoveConnection(netThreadOperation.ConnectionId, netThreadOperation.ErrorCode);
                        break;
                    case ENetThreadOpType.SendMessage: 
                        self.NetDriver.Send(netThreadOperation.ConnectionId, netThreadOperation.BufferRef,  netThreadOperation.IsReliable);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        

        private static void OnNetThreadAcceptConnection(this NetworkComponent self, uint connectionId, IPEndPoint remoteEndPoint)
        {
            self.MainThreadOpQueue.Enqueue(new MainThreadOperations()
            {
                OpType = EMainTreadOpType.AcceptConnection,
                ConnectionId = connectionId,
                RemoteEndPoint = remoteEndPoint,
            });
        }
        
        private static void OnNetThreadConnectionReceive(this NetworkComponent self, uint connectionId, SocketBufferRef skBuffRef)
        {
            // Preserve admission at receipt even if the Root becomes ready before the main thread drains the queue.
            RootInitializationSnapshot snapshot = self.OwnerRoot.InitializationSnapshot;
            ERootInitializationState state = snapshot.State;
            RootMessageAdmission targetAdmission = null;
            if (self.LogicScene.SceneType == ELogicSceneType.Main
                && skBuffRef.Target.TrueLength >= sizeof(int) + sizeof(int) + sizeof(long))
            {
                ReadOnlySpan<byte> header = skBuffRef.Target.GetBuffer().AsSpan(skBuffRef.Target.DataPosition,
                    sizeof(int) + sizeof(int) + sizeof(long));
                header.ReadInt(out _).ReadInt(out int targetRoot);
                targetAdmission = ProcessMessageManager.Instance.CaptureAdmission(targetRoot);
            }
            self.MainThreadOpQueue.Enqueue(new MainThreadOperations()
            {
                OpType = EMainTreadOpType.OnReceiveMessage,
                ConnectionId = connectionId,
                BufferRef = skBuffRef,
                ReceivedBeforeRootReady = state == ERootInitializationState.Initializing
                    || state == ERootInitializationState.Failed,
                InitializationMessagePolicy = snapshot.MessagePolicy,
                TargetAdmission = targetAdmission,
            });
        }
        
        private static void OnNetThreadConnectionError(this NetworkComponent self, uint connectionId, int errorCode)
        {
            self.MainThreadOpQueue.Enqueue(new MainThreadOperations()
            {
                OpType = EMainTreadOpType.OnConnectionError,
                ConnectionId = connectionId,
                ErrorCode = errorCode,
            });
        }
        #endregion
    }
}