using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Bliss.Base.Collections;

namespace Bliss.Core.Network
{
    public unsafe class UdpNetConnection : IpNetConnection
    {
        public uint RemoteId { get; private set; }
        public uint ConversationId { get; private set; }

        private IPEndPoint _remoteEndPoint;
        private UdpNetDriver _udpDriver;
        private long _createTime;
        private ENetConnectionState _state;
        private ConnectHandler _connectHandler;
        private Queue<(bool, SocketBufferRef)> _waitSendQueue = new Queue<(bool, SocketBufferRef)>();
        private KCP _kcp;
        private int MaxWaitSendSize = 10240;
        
        /// <summary>
        /// 握手超时时间  毫秒
        /// </summary>
        private const long ConnectTimeoutTime = 10 * 1000;
        
        /// <summary>
        /// Init Incoming Connection
        /// Remote -> Local
        /// </summary>
        /// <param name="netDriver"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="connectionId"></param>
        /// <param name="remoteId"></param>
        /// <param name="conversationId">kcp conv</param>
        public void InitRemoteConnection(IpNetDriver netDriver,
            IPEndPoint remoteEndPoint, 
            uint connectionId, 
            uint remoteId,
            uint conversationId)
        {
            InitConnection(netDriver, remoteEndPoint, connectionId);   
            ConnectionType = EIpNetConnectionType.Remote;
            RemoteId = remoteId;
            ConversationId = conversationId;
            _state = ENetConnectionState.Open;
            _kcp = new KCP(conversationId, KCP_Output);
            InitKcp();
        }


        /// <summary>
        /// Init Outcoming Connection
        /// Local -> Remote
        /// </summary>
        /// <param name="netDriver"></param>
        /// <param name="remoteEndPoint"></param>
        /// <param name="connectionId"></param>
        public void InitLocalConnection(IpNetDriver netDriver, IPEndPoint remoteEndPoint, uint connectionId)
        {
            InitConnection(netDriver, remoteEndPoint, connectionId);   
            ConnectionType = EIpNetConnectionType.Local;
            _state = ENetConnectionState.Pending;
            _connectHandler = new ConnectHandler(this);
            _connectHandler.SendInitHandsShake();
            _udpDriver.ScheduleUpdate(ConnectionId, (uint)netDriver.TimeNow + 30);
        }

        private void InitConnection(IpNetDriver netDriver, IPEndPoint remoteEndPoint, uint connectionId)
        {
            NetDriver = netDriver;
            _udpDriver =  (UdpNetDriver)netDriver;
            ConnectionId = connectionId;
            
            _remoteEndPoint = remoteEndPoint;
            _createTime = netDriver.TimeNow;
        }

        private void InitKcp()
        {
            _kcp.ikcp_nodelay(1, 10, 2, 1);
            _kcp.ikcp_wndsize(256, 256);
            _kcp.ikcp_setmtu(470);
        }
        
        protected override void OnDispose()
        {
            base.OnDispose();

            uint localId = ConnectionId;
            uint remoteId = RemoteId;

            ConnectionId = 0;
            RemoteId = 0;
            ConversationId = 0;
                  
            _kcp?.Dispose();
            _kcp = null;
            
            _state = ENetConnectionState.Close;
            
            _udpDriver.RemoveConnection(localId);
            _udpDriver.SendDisconnect(localId, remoteId, _remoteEndPoint, Error);
        }


        internal void OnConnectCompleted(uint remoteId)
        {
            if (_state == ENetConnectionState.Open)
            {
                return;
            }
            RemoteId = remoteId;
            ConversationId = remoteId;
            
            _kcp = new KCP(ConversationId, KCP_Output);
            InitKcp();
            
            _state = ENetConnectionState.Open;

            while (_waitSendQueue.Count > 0)
            {
                (bool isReliable, SocketBufferRef buffer) = _waitSendQueue.Dequeue();
                Send(buffer, isReliable);
            }
        }

        internal void Update(uint timeNow)
        {
            if (IsDisposed)
            {
                return;
            }

            //处理握手包重发
            uint nextUpdateTime = timeNow + 1000;
            if (_connectHandler != null
                && ConnectionType == EIpNetConnectionType.Local
                && _state != ENetConnectionState.Open)
            {

                if (timeNow > _createTime + ConnectTimeoutTime)
                {
                    OnError(ConnectionError.ERR_HANDSHAKE_TIMEOUT);
                    return;
                }
                
                _connectHandler.Update(timeNow);
                nextUpdateTime = _connectHandler.Check(timeNow);
            }

            //处理kcp update
            if (_kcp != null)
            {
                if (_kcp.ikcp_state() == -1)
                {
                    OnError(ConnectionError.ERR_DEAD_LINK);
                    return;
                }
                
                _kcp.ikcp_update(timeNow);
                nextUpdateTime = System.Math.Min(_kcp.ikcp_check(timeNow), nextUpdateTime);
            }
            
            _udpDriver.ScheduleUpdate(ConnectionId, nextUpdateTime);
        }

        #region Send Message To Remote

        /// <summary>
        /// Upper Level Send to this 
        /// </summary>
        /// <param name="socketBufferRef"></param>
        /// <param name="isReliable"></param>
        public sealed override void Send(SocketBufferRef socketBufferRef, bool isReliable)
        {
            if (IsDisposed || _state == ENetConnectionState.Invalid)
            {
                return;
            }
            
            if (_state == ENetConnectionState.Pending)
            {
                _waitSendQueue.Enqueue((isReliable, socketBufferRef));
                return;
            }


            if (isReliable)
            {
                SendReliable(socketBufferRef);
            }
            else
            {
                SendUnreliable(socketBufferRef);
            }
        }

        private void SendReliable(SocketBufferRef socketBufferRef)
        {
            if (_kcp.ikcp_waitsnd() > MaxWaitSendSize)
            {
                OnError(ConnectionError.ERR_SENDBUFF_TOOLARGE);
                return;
            }
            
            
            SocketBuffer buffer = socketBufferRef.Target;
            _kcp.ikcp_send(buffer.Pull(buffer.TrueLength));
            
            socketBufferRef.Dispose();
            _udpDriver.ScheduleUpdate(ConnectionId, 0);
        }
        
        private int KCP_Output(byte* buffer, int len)
        {
            if (IsDisposed)
            {
                return 0;
            }
            
            int messageSize = len + ProtocolDefine.MsgPacketHeaderSize;
            SocketBufferRef bufferRef =  _udpDriver.RentBuffer(messageSize);
            SocketBuffer skBuffer = bufferRef.Target;
            skBuffer.Reserve(messageSize);
            skBuffer.Push(len);
            Marshal.Copy((IntPtr)buffer, skBuffer.GetBuffer(), ProtocolDefine.MsgPacketHeaderSize, len);

            bool isReliable = true;
            skBuffer.Push(ProtocolDefine.MsgPacketHeaderSize)
                .WriteByteTo((byte)EProtocolType.Msg)
                .WriteBoolTo(isReliable)
                .WriteUIntTo(ConnectionId)
                .WriteUIntTo(RemoteId);
            
            OutputRaw(skBuffer.GetBuffer(), 0, messageSize);
            bufferRef.Dispose();
            return len;
        }

        private void SendUnreliable(SocketBufferRef socketBufferRef)
        {
            SocketBuffer messageBuf = socketBufferRef.Target;
            int messageSize = messageBuf.TrueLength + ProtocolDefine.MsgPacketHeaderSize;
            
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(messageSize);
            SocketBuffer skBuffer = bufferRef.Target;
            skBuffer.Reserve(messageSize);
            

            skBuffer.Push(messageBuf.TrueLength)
                .WriteReadOnlySpanTo(messageBuf.Pull(messageBuf.TrueLength));
            
            bool isReliable = false;
            skBuffer.Push(ProtocolDefine.MsgPacketHeaderSize)
                .WriteByteTo((byte)EProtocolType.Msg)
                .WriteBoolTo(isReliable)
                .WriteUIntTo(ConnectionId)
                .WriteUIntTo(RemoteId);
            
            OutputRaw(skBuffer.GetBuffer(), 0, messageSize);
            
            socketBufferRef.Dispose();
            bufferRef.Dispose();
        }
        
        internal void OutputRaw(byte[] buffer, int offset, int size)
        {
            int outPacketId = _outPacketIdGenerator++;
            var outPacketInfo = new OutPacketInfo()
            {
                OutPacketId = outPacketId,
                SendTime = NetDriver.TimeNow,
            };
            
            bool inSimulation = CheckOutgoingPacketSimulation(ref outPacketInfo, size);

            if (!inSimulation)
            {
                OutputRawImmediately(buffer, offset, size);
            }
            else
            {
                if (outPacketInfo.Droped)
                {
                    return;
                }
                
                SocketBufferRef bufferRef = NetDriver.RentBuffer(size);
                SocketBuffer skBuffer = bufferRef.Target;
                skBuffer.Reserve(size);
                skBuffer.Push(size)
                    .WriteReadOnlySpanTo(new ReadOnlySpan<byte>(buffer, offset, size));
                outPacketInfo.BufferRef =  bufferRef;
                
                _sendBucket.Add(outPacketInfo.SimulateSendTime, outPacketInfo.OutPacketId);
                _dicPacket.Add(outPacketInfo.OutPacketId, outPacketInfo);
                if (outPacketInfo.SimulateSendTime < _minTimeout)
                {
                    _minTimeout = outPacketInfo.SimulateSendTime;
                }

                if (_dicPacket.Count > 100)
                {
                    System.Diagnostics.Debugger.Break();
                }
            }
        }

        private void OutputRawImmediately(byte[] buffer, int offset, int size)
        {
            _udpDriver.Output(buffer, offset, size, _remoteEndPoint);
            
            OutBytes += (uint)size;
            OutTotalBytes += (uint)size;
            OutPackets++;
            OutTotalPackets++;
            
            _udpDriver.OutBytes += (uint)size;
            _udpDriver.OutTotalBytes += (uint)size;
            _udpDriver.OutPackets++;
            _udpDriver.OutTotalPackets++;
        }

        #endregion


        #region Receive Data From Remote

        internal void InputHandshakeRaw(ReadOnlySpan<byte> bufferSpan)
        {
            if (IsDisposed || _state == ENetConnectionState.Invalid)
            {
                return;
            }

            if (_state == ENetConnectionState.Pending)
            {
                _connectHandler.Input(bufferSpan);
            }
        }

        internal void InputRaw(ReadOnlySpan<byte> bufferSpan)
        {
            if (IsDisposed || _state != ENetConnectionState.Open)
            {
                return;
            }
            
            InBytes += (uint)bufferSpan.Length;
            InTotalBytes += (uint)bufferSpan.Length;
            InPackets++;
            InTotalPackets++;

            if (_udpDriver != null)
            {
                _udpDriver.InBytes += (uint)bufferSpan.Length;
                _udpDriver.InTotalBytes += (uint)bufferSpan.Length;
                _udpDriver.InPackets++;
                _udpDriver.InTotalPackets++;
            }

            
            bufferSpan.ReadByte(out _)
                .ReadBool(out bool isReliable);

            if (isReliable)
            {
                InputReliableRaw(bufferSpan);
            }
            else
            {
                InputUnreliableRaw(bufferSpan);
            }
        }
        
        private void InputReliableRaw(ReadOnlySpan<byte> bufferSpan)
        {
            bufferSpan = bufferSpan.Slice(ProtocolDefine.MsgPacketHeaderSize);
            int error = _kcp.ikcp_input(bufferSpan);
            if (error != 0)
            {
                return;
            }
            
            _udpDriver.ScheduleUpdate(ConnectionId, 0);
            
            while (ReceiveNextReliable(out var bufferRef))
            {
                if (bufferRef != null) Input(bufferRef.Value);
            }
        }
        
        private void InputUnreliableRaw(ReadOnlySpan<byte> bufferSpan)
        {
            bufferSpan = bufferSpan.Slice(ProtocolDefine.MsgPacketHeaderSize);
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(bufferSpan.Length);
            
            SocketBuffer buffer = bufferRef.Target;
            buffer.Reserve(bufferSpan.Length);
            
            buffer.Push(bufferSpan.Length)
                .WriteReadOnlySpanTo(bufferSpan);
            
            Input(bufferRef);
        }
        

        private bool ReceiveNextReliable(out SocketBufferRef? result)
        {
            int messageLength = _kcp.ikcp_peeksize();
            if (messageLength <= 0)
            {
                result = null;
                return false;
            }
                
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(messageLength);
            SocketBuffer buffer = bufferRef.Target;
            buffer.Reserve(messageLength);
            Span<byte> writer = buffer.Push(messageLength);
            
            int receiveDataLength = _kcp.ikcp_recv(writer);
            if (receiveDataLength < 0)
            {
                bufferRef.Dispose();
                result = null;
                OnError(ConnectionError.ERR_SOCKET_CANT_RECEIVE);
                return false;
            }

            result = bufferRef;
            return true;
        }
        #endregion

        protected override void UpdateNetworkLagStats()
        {
            if (_kcp != null)
            {
                AvgLagMS = _kcp.SmoothRTT;
                AverageJitterInMS =  _kcp.SmoothJitter;
            }
        }

        internal void OnError(int error, bool logError = true)
        {
            uint connectionId = ConnectionId;
            if (logError)
            {
                Log.Error($"{nameof(UdpNetConnection)}[{connectionId}] error: {error}");
            }
            _udpDriver.RemoveConnection(connectionId, error);
            _udpDriver.ReportConnectionError(connectionId, error);
        }


        #region Simulate
        private int _outPacketIdGenerator;
        private readonly Random _random = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount);
        private SortedMultiMapList<long, int> _sendBucket = new SortedMultiMapList<long, int>();
        private Dictionary<int, OutPacketInfo> _dicPacket = new Dictionary<int, OutPacketInfo>();
        private long _minTimeout;
        private Queue<long> _queueOutTimestamp = new Queue<long>();
        private Queue<int> _queueOutTimePacketId = new Queue<int>();
        internal void UpdateSimulate(long now)
        {
            if (_sendBucket.Count == 0)
            {
                return;
            }
            
            if (now < _minTimeout)
            {
                return;
            }

            _queueOutTimestamp.Clear();
            _queueOutTimePacketId.Clear();
            
            foreach (var kv in _sendBucket)
            {
                long timeStamp = kv.Key;
                if (_minTimeout < timeStamp)
                {
                    _minTimeout = timeStamp;
                    break;
                }
                _queueOutTimestamp.Enqueue(timeStamp);
            }

            while (_queueOutTimestamp.Count > 0)
            {
                var timeStamp = _queueOutTimestamp.Dequeue();
                
                foreach (var packetIdList in _sendBucket[timeStamp])
                {
                    _queueOutTimePacketId.Enqueue(packetIdList);
                }
                _sendBucket.Remove(timeStamp);
            }
            
            while (_queueOutTimePacketId.Count > 0)
            {
                int packetId = _queueOutTimePacketId.Dequeue();
                if (!_dicPacket.Remove(packetId, out var outPacketInfo))
                {
                    continue;
                }
                SocketBufferRef bufferRef = outPacketInfo.BufferRef;
                SocketBuffer skBuffer = bufferRef.Target;
                OutputRawImmediately(skBuffer.GetBuffer(), skBuffer.DataPosition, skBuffer.TrueLength);
                bufferRef.Dispose();
            }
        }
        private bool CheckOutgoingPacketSimulation(ref OutPacketInfo  outPacketInfo, int numBytes)
        {
            bool inSimulation = false;
            long now = NetDriver.TimeNow;

            if (ShouldDropOutgoingPacketForLossSimulation(numBytes))
            {
                outPacketInfo.Droped = true;
                inSimulation = true;
            }
            else if (SimulationSettings.Jitter != 0)
            {
                bool isLowLatency = outPacketInfo.OutPacketId % 2 == 0;
                
                if (isLowLatency)
                {
                    double latencyVariance = 2.0 * (_random.NextDouble() - 0.5) * SimulationSettings.LatencyVariance;
                    long latency = (long)(SimulationSettings.LatencyMin + latencyVariance);
                    outPacketInfo.SimulateSendTime = now + latency;
                }
                else
                {
                    double latencyVariance = 2.0 * (_random.NextDouble() - 0.5) * SimulationSettings.LatencyVariance;
                    long latency = (long)(SimulationSettings.LatencyMin + SimulationSettings.Jitter + latencyVariance);
                    outPacketInfo.SimulateSendTime = now + latency;
                }
                inSimulation =  true;
            }
            else if (SimulationSettings.Latency > 0)
            {
                double latencyVariance = 2.0 * (_random.NextDouble() - 0.5) * SimulationSettings.LatencyVariance;
                long latency = (long)(SimulationSettings.Latency + latencyVariance);
                outPacketInfo.SimulateSendTime = now + latency;
                inSimulation =  true;

            }
            else if (SimulationSettings.LatencyMin > 0 || SimulationSettings.LatencyMax > 0)
            {
                double latencyVariance = _random.NextDouble() * (SimulationSettings.LatencyMax - SimulationSettings.LatencyMin);
                long latency = (long)(SimulationSettings.LatencyMin + latencyVariance);
                outPacketInfo.SimulateSendTime = now + latency;
                inSimulation =  true;
            }
            
            return inSimulation;
        }

        private bool ShouldDropOutgoingPacketForLossSimulation(int numBytes)
        {
            return NetDriver.IsSimulatingPacketLossBurst() ||
                   (SimulationSettings.Loss > 0 &&
                    SimulationSettings.ShouldDropPacketOfSize(numBytes) &&
                    (_random.NextDouble() * 100f < SimulationSettings.Loss));
        }

        public override void DebugSimulate(System.Text.StringBuilder stringBuilder)
        {
            foreach (var kv in _sendBucket)
            {
                var simulateSendTime = kv.Key;
                stringBuilder.AppendLine($"--------------------------");
                stringBuilder.AppendLine($"[{simulateSendTime}]");
                foreach (var outPacketId in kv.Value)
                {
                    var outPacketInfo = _dicPacket[outPacketId];
                    stringBuilder.AppendLine($"Pkt[{outPacketId}] SendTime={outPacketInfo.SendTime}, SimulateSendTime={outPacketInfo.SimulateSendTime}, Diff={outPacketInfo.SimulateSendTime - outPacketInfo.SendTime}");
                }
                stringBuilder.AppendLine($"--------------------------");
            }
        }
        
        private struct OutPacketInfo
        {
            public int OutPacketId;
            public SocketBufferRef  BufferRef;
            public long SendTime;
            public long SimulateSendTime;
            public bool Droped;
        }

        #endregion
    }
}