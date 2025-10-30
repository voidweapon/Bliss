using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Bliss.Core.Network
{
    public class NetworkProfiler : IDisposable
    {
        public ConcurrentQueue<NetworkProfilerSample> SampleQueue = new ConcurrentQueue<NetworkProfilerSample>();
        
        public static bool EnableNetworkProfiler = false;
        private static NetworkProfiler _instance;
        public static NetworkProfiler Instance => _instance;

        public static void Create()
        {
            _instance = new NetworkProfiler();
        }
        
        public void Dispose()
        {
            _instance = null;
        }

        public void Sample(ANetDriver driver)
        {
            if(!EnableNetworkProfiler) return;
            
            var newSample = new NetworkProfilerSample();
            newSample.Timestamp = driver.TimeNow;
            newSample.InTotalBytes = driver.InTotalBytes;
            newSample.OutTotalBytes = driver.OutTotalBytes;
            newSample.InTotalPackets =  driver.InTotalPackets;
            newSample.OutTotalPackets = driver.OutTotalPackets;
            newSample.InBytesPerSecond =  driver.InBytesPerSecond;
            newSample.OutBytesPerSecond = driver.OutBytesPerSecond;
            
            Enqueue(newSample);
        }

        public void Sample(ANetConnection netConnection)
        {
            if(!EnableNetworkProfiler) return;
            
            var newSample = new NetworkProfilerNetConnectionSample();
            newSample.Timestamp = netConnection.NetDriver.TimeNow;
            newSample.ConnectionId = netConnection.ConnectionId;
            newSample.InTotalBytes = netConnection.InTotalBytes;
            newSample.OutTotalBytes = netConnection.OutTotalBytes;
            newSample.InTotalPackets = netConnection.InTotalPackets;
            newSample.OutTotalPackets = netConnection.OutTotalPackets;
            newSample.InBytesPerSecond = netConnection.InBytesPerSecond;
            newSample.OutBytesPerSecond = netConnection.OutBytesPerSecond;
            newSample.InPacketsPerSecond = netConnection.InPacketsPerSecond;
            newSample.OutPacketsPerSecond = netConnection.OutPacketsPerSecond;
            newSample.AvgLagMS = netConnection.AvgLagMS;
            newSample.AverageJitterInMS = netConnection.AverageJitterInMS;
            
            Enqueue(newSample);
        }
        
        private void Enqueue(NetworkProfilerSample sample)
        {
            SampleQueue.Enqueue(sample);
            if (SampleQueue.Count > 100)
            {
                SampleQueue.TryDequeue(out _);
            }
        }
    }

    public class NetworkProfilerSample
    {
        public long Timestamp;
        
        /// <summary>
        /// 接收到的总字节数
        /// </summary>
        public uint InTotalBytes;
        
        /// <summary>
        /// 发送的总字节数
        /// </summary>
        public uint OutTotalBytes;
        
        /// <summary>
        /// 收到的总包数量
        /// </summary>
        public uint InTotalPackets;
        
        /// <summary>
        /// 发送的总包数量
        /// </summary>
        public uint OutTotalPackets;
        
        /// <summary>
        /// 每秒发送的字节数
        /// </summary>
        public uint InBytesPerSecond;
        
        /// <summary>
        /// 每秒发送的字节数
        /// </summary>
        public uint OutBytesPerSecond;
    }

    public class NetworkProfilerNetConnectionSample : NetworkProfilerSample
    {
        public long ConnectionId;
        
        /// <summary>
        /// 每秒收到的包数
        /// </summary>
        public uint InPacketsPerSecond;
        
        /// <summary>
        /// 每秒发送的包数
        /// </summary>
        public uint OutPacketsPerSecond;

        public long AvgLagMS;
        
        public long AverageJitterInMS;
    }
}