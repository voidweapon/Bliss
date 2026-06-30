using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Bliss.Core;
using Bliss.Core.Network;

namespace Bliss
{
    public class NetworkComponent : Entity, IAwake<NetworkClientParam>, IAwake<NetworkServerParam>, IEarlyUpdate, IDestroy
    {
        public ANetDriver NetDriver;
        public Thread NetThread;
        public bool IsStop;
        public ConcurrentQueue<NetThreadOperation> NetThreadOpQueue =  new ConcurrentQueue<NetThreadOperation>();
        public ConcurrentQueue<MainThreadOperations> MainThreadOpQueue =  new ConcurrentQueue<MainThreadOperations>();
    }


    public enum ENetDriverType
    {
        Tcp,
        Udp,
        
        /// <summary>
        /// Udp First
        /// Tcp Backup
        /// </summary>
        PrimarySecondaryIp
    }

    public struct NetworkClientParam
    {
        public AddressFamily  AddressFamily;
        public ENetDriverType DriverType;
        public bool UseNetThread;
    }

    public struct NetworkServerParam
    {
        public IPEndPoint BindEndPoint;
        public ENetDriverType DriverType;
        public bool UseNetThread;

    }

    public enum ENetThreadOpType
    {
        CreateConnection,
        DestroyConnection,
        SendMessage,
    }

    public enum EMainTreadOpType
    {
        AcceptConnection,
        OnConnectionError,
        OnReceiveMessage,
        ConnectionStatSample,
    }

    public struct NetThreadOperation
    {
        public ENetThreadOpType OpType;
        public uint ConnectionId;
        public int ErrorCode;
        public IPEndPoint RemoteEndPoint;
        public SocketBufferRef BufferRef;
        public bool IsReliable;
    }

    public struct MainThreadOperations
    {
        public EMainTreadOpType OpType;
        public uint ConnectionId;
        public int ErrorCode;
        public IPEndPoint RemoteEndPoint;
        public SocketBufferRef BufferRef;
        public bool IsReliable; 
    }

    public struct Invoke_OnReceiveMessage
    {
        public Session Session;
        public ActorAddress TargetAddress;
        public object Message;
    }
    
    public struct Event_OnAcceiptConnection
    {
        public Session Session;
    }
}