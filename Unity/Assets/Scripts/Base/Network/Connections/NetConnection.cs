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
        public uint ConnectionId {get; protected set;}

        public ANetDriver NetDriver { get; protected set; }
        
        public ENetConnectionState State { get; protected set; }
        
        protected bool IsDisposed = false;

        internal int Error;

        /// <summary>
        /// 发送数据
        /// </summary>
        public virtual void Send(SocketBufferRef socketBufferRef, bool isReliable)
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
        /// 本次采样间隔内发送的包数量
        /// </summary>
        public uint InPackets { get; internal set; }
        
        /// <summary>
        /// 发送的总包数量
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
        
        /// <summary>
        /// 每秒收到的包数
        /// </summary>
        public uint InPacketsPerSecond { get; internal set; }
        
        /// <summary>
        /// 每秒发送的包数
        /// </summary>
        public uint OutPacketsPerSecond { get; internal set; }
        
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
        
        internal void UpdateNetworkStats()
        {
            long timeNow = NetDriver.TimeNow;
            
            if (timeNow > LastStatUpdateTime + StatPeriod)
            {
                float intervalS = (timeNow - LastStatUpdateTime) / 1000f;

                InBytesPerSecond = (uint)System.Math.Floor(InBytes / intervalS);
                OutBytesPerSecond = (uint)System.Math.Floor(OutBytes / intervalS);
                
                InPacketsPerSecond = (uint)System.Math.Floor(InPackets / intervalS);
                OutPacketsPerSecond = (uint)System.Math.Floor(OutPackets / intervalS);

                UpdateNetworkLagStats();
                
                InBytes = 0;
                OutBytes = 0;
                InPackets = 0;
                OutPackets = 0;
                LastStatUpdateTime = timeNow;
            }
        }

        protected virtual void UpdateNetworkLagStats()
        {
            
        }
        #endregion
    }

    public enum ENetConnectionState
    {
        Invalid,
        Close,
        Pending,
        Open,
    }
}