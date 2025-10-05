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
        /// <param name="isReliable"></param>
        public abstract void Send(uint connectionId, SocketBufferRef socketBufferRef, bool isReliable);


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

            ConcurrentQueue<SocketBuffer> bufferPool = _bufferPool.GetOrAdd(size, NewPool);

            ConcurrentQueue<SocketBuffer> NewPool(int _)
            {
                return new ConcurrentQueue<SocketBuffer>();
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
        
        
        #region 网络统计数据
        /// <summary>
        /// 上次收集网络统计数据时间戳
        /// </summary>
        public long LastStatUpdateTime { get; internal set; }

        /// <summary>
        /// 收集网络统计数据时间间隔 MS
        /// </summary>
        public long StatPeriod { get; internal set; } = 1000L;
        
        /// <summary>
        /// 本次采样间隔内接收到的字节数
        /// </summary>
        public uint InBytes { get; internal set; }
        
        /// <summary>
        /// 接收到的总字节数
        /// </summary>
        public uint InTotalBytes { get; internal set; }
        
        /// <summary>
        /// 本次采样间隔内发送的字节数
        /// </summary>
        public uint OutBytes { get; internal set; }
        
        /// <summary>
        /// 发送的总字节数
        /// </summary>
        public uint OutTotalBytes { get; internal set; }
        
        /// <summary>
        /// 本次采样间隔内收到的包数量
        /// </summary>
        public uint InPackets { get; internal set; }
        
        /// <summary>
        /// 收到的总包数量
        /// </summary>
        public uint InTotalPackets { get; internal set; }
        
        /// <summary>
        /// 本次采样间隔内收到的包数量
        /// </summary>
        public uint OutPackets { get; internal set; }
        
        /// <summary>
        /// 收到的总包数量
        /// </summary>
        public uint OutTotalPackets { get; internal set; }
        
        /// <summary>
        /// 每秒收到的字节数
        /// </summary>
        public uint InBytesPerSecond { get; internal set; }
        
        /// <summary>
        /// 每秒发送的字节数
        /// </summary>
        public uint OutBytesPerSecond { get; internal set; }

        private void UpdateNetworkStats()
        {
            long timeNow = TimeNow;
            
            if (timeNow > LastStatUpdateTime + StatPeriod)
            {
                float intervalS = (timeNow - LastStatUpdateTime) / 1000f;

                InBytesPerSecond = (uint)System.Math.Floor(InBytes / intervalS);
                OutBytesPerSecond = (uint)System.Math.Floor(OutBytes / intervalS);


                InBytes = 0;
                OutBytes = 0;
                InPackets = 0;
                OutPackets = 0;
                LastStatUpdateTime = timeNow;
            }

            foreach (var kv in _dicId2Connection)
            {
                var connection = kv.Value;
                connection.UpdateNetworkStats();
            }
        }
        #endregion
    }
}