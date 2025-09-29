using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bliss.Core.Network
{
    /// <summary>
    /// 1. 管理网络连接
    /// 2. 可配置仿真网络延迟、丢包等
    /// 3. 支持网络统计
    /// </summary>
    public abstract class ANetDriver : IDisposable
    {
        public event Action<uint, IPEndPoint> OnAcceptConnection;
        public event Action<uint, SocketBufferRef> OnConnectionReceive;
        public event Action<uint, int> OnConnectionError;
        
        /// <summary>
        /// key: connectionId -> value: ANetConnection
        /// </summary>
        protected Dictionary<uint, ANetConnection> _dicId2Connection = new Dictionary<uint, ANetConnection>();
        
        protected long CreateTime;
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// MS
        /// </summary>
        internal long TimeNow => TimeInfo.Instance.ClientNow() - CreateTime;

        public ANetDriver()
        {
            CreateTime = TimeInfo.Instance.ClientNow();
        }
        
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            
            OnDispose();
        }

        protected abstract void OnDispose();

        public abstract void InitClient(AddressFamily addressFamily = AddressFamily.InterNetwork);
        public abstract void InitServer(IPEndPoint endPoint);
        

        public void CreateRemoteConnection(uint connectionId,IPEndPoint remoteEndPoint)
        {
            CreateConnection(connectionId, remoteEndPoint);
        }
        
        public abstract void RemoveConnection(uint connectionId, int error = 0);
        
        public bool TryGetConnection(uint connectionId, out ANetConnection connection)
        {
            return _dicId2Connection.TryGetValue(connectionId, out connection);
        }
        
        
        protected abstract void CreateConnection(uint connectionId, IPEndPoint remoteEndPoint);
        
        /// <summary>
        /// 用指定连接发送数据
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="socketBufferRef"></param>
        public abstract void Send(uint connectionId, SocketBufferRef socketBufferRef);


        internal virtual void Input(ANetConnection connection, SocketBufferRef socketBufferRef)
        {
            OnConnectionReceive?.Invoke(connection.ConnectionId, socketBufferRef);
        }

        internal void ReportAcceptConnection(uint connectionId, IPEndPoint remoteEndPoint)
        {
            OnAcceptConnection?.Invoke(connectionId, remoteEndPoint);
        }

        internal void ReportConnectionError(uint connectionId, int error)
        {
            OnConnectionError?.Invoke(connectionId, error);
        }
        
        public virtual void Update()
        {
            
        }

        #region NetworkGuid
        
        public uint CreateLocalConnectionId()
        {
            return CreateConnectChannelId(false);
        }
        
        internal uint CreateRemoteConnectionId()
        {
            return CreateConnectChannelId(true);
        }

        private uint _networkGuidIndex = 0;
        private uint CreateConnectChannelId(bool isRemote)
        {
            lock (this)
            {
                uint index = ++_networkGuidIndex;
                return  index << 1 | (isRemote ? 1u : 0u);
            }
        }

        #endregion

        #region Buffer Pool

        private ConcurrentDictionary<int, ConcurrentQueue<SocketBuffer>> _bufferPool = new ConcurrentDictionary<int, ConcurrentQueue<SocketBuffer>>();
        
        public SocketBufferRef RentBuffer(int sizeHint)
        {
            if (sizeHint <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint), "size hint must be greater than zero");
            }
            int size = GetSize(sizeHint);

            if (!_bufferPool.TryGetValue(size, out ConcurrentQueue<SocketBuffer> bufferPool))
            {
                bufferPool = new ConcurrentQueue<SocketBuffer>();
                _bufferPool.TryAdd(size, bufferPool);
            }

            if (!bufferPool.TryDequeue(out var buff))
            {
                buff =  new SocketBuffer(size);
            }
            
            return new SocketBufferRef(size, buff, this);
        }
        
        internal void ReturnBuffer(int size, SocketBuffer buffer)
        {
            if (_bufferPool.TryGetValue(size, out ConcurrentQueue<SocketBuffer> bufferPool))
            {
                if (bufferPool.Count >= 16)
                {
                    return;
                }
                
                buffer.Clear();
                bufferPool.Enqueue(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private  static int GetSize(int sizeHint)
        {
            //[2^4, 2^30]
            sizeHint = System.Math.Clamp(sizeHint, 0x10, 0x40000000);
            int index = (int)System.Math.Log((uint)sizeHint - 1 | 15, 2) - 3;
            return 16 << index;
        }

        #endregion
        
    }
}