using System;

namespace Bliss.Core.Network
{
    /// <summary>
    /// 1. 连接管理 维护与远端（客户端或服务器）的网络连接状态（如 Open、Closed、Pending），管理连接的初始化、关闭、超时检测、重连等。
    /// 2. 数据包收发与可靠性
    /// 3. 网络统计与分析 统计延迟（Lag/AvgLag）、抖动（Jitter）、带宽、丢包等网络性能数据。
    /// </summary>
    public abstract class ANetConnection : IDisposable
    {

        #region 网络统计数据
        /// <summary>
        /// 平均Lag
        /// </summary>
        public float AvgLag { get; protected set; }
        
        /// <summary>
        /// Total accumulated lag values during the current StatPeriod
        /// </summary>
        public double LagAccumulate { get; protected set; }
        
        /// <summary>
        ///  Nb of stats accumulated in LagAcc
        /// </summary>
        public int LagCount { get; protected set; }
        
        /// <summary>
        /// Jitter represents the average time divergence of all sent packets.
        /// Ex:
        /// If the time between the sending and the reception of packets is always 100ms; the jitter will be 0.
        /// If the time difference is either 150ms or 100ms, the jitter will tend towards 50ms.
        /// </summary>
        public float AverageJitterInMS  { get; protected set; }
        #endregion
        
        public uint ConnectionId {get; protected set;}

        public ANetDriver NetDriver { get; protected set; }
        
        public ENetConnectionState State { get; protected set; }
        
        protected bool IsDisposed = false;

        internal int Error;

        /// <summary>
        /// 发送数据
        /// </summary>
        public virtual void Send(SocketBufferRef socketBufferRef)
        {
            
        }

        
        /// <summary>
        /// 接收数据
        /// </summary>
        public virtual void Input(SocketBufferRef socketBufferRef)
        {
            NetDriver.Input(this, socketBufferRef);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            
            OnDispose();
            
            NetDriver = null;
        }

        protected virtual void OnDispose()
        {
            
        }
        
    }

    public enum ENetConnectionState
    {
        Invalid,
        Close,
        Pending,
        Open,
    }
}