using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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
        private Queue<SocketBufferRef> _waitSendQueue = new Queue<SocketBufferRef>();
        private KCP _kcp;
        private int MaxWaitSendSize = 1024 * 1024;
        
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
                  
            _kcp.Dispose();
            _kcp = null;
            
            _state = ENetConnectionState.Close;
            
            _udpDriver.RemoveConnection(localId);
            _udpDriver.SendDisconnect(localId, remoteId, _remoteEndPoint);
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
                var buffer = _waitSendQueue.Dequeue();
                Send(buffer);
            }
        }

        internal void Update(uint timeNow)
        {
            if (IsDisposed)
            {
                return;
            }

            uint nextUpdateTime = timeNow + 1000;
            if (_connectHandler != null
                && ConnectionType == EIpNetConnectionType.Local
                && _state != ENetConnectionState.Open)
            {
                _connectHandler.Update(timeNow);
                nextUpdateTime = _connectHandler.Check(timeNow);
            }

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
        public sealed override void Send(SocketBufferRef socketBufferRef)
        {
            if (IsDisposed || _state == ENetConnectionState.Invalid)
            {
                return;
            }
            
            if (_state == ENetConnectionState.Pending)
            {
                _waitSendQueue.Enqueue(socketBufferRef);
                return;
            }

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
            
            skBuffer.Push(ProtocolDefine.MsgPacketHeaderSize)
                .WriteByteTo((byte)EProtocolType.Msg)
                .WriteUIntTo(ConnectionId)
                .WriteUIntTo(RemoteId);
            
            OutputRaw(skBuffer.GetBuffer(), 0, messageSize);
            bufferRef.Dispose();
            return len;
        }
        
        internal void OutputRaw(byte[] buffer, int offset, int size)
        {
            _udpDriver.Output(buffer, offset, size, _remoteEndPoint);
        }

        #endregion


        #region Receive Data From Remote

        internal void InputRaw(ReadOnlySpan<byte> bufferSpan)
        {
            if (IsDisposed || _state == ENetConnectionState.Invalid)
            {
                return;
            }

            if (_state == ENetConnectionState.Pending)
            {
                _connectHandler.Input(bufferSpan);
                return;
            }
            
            bufferSpan = bufferSpan.Slice(ProtocolDefine.MsgPacketHeaderSize);
            _kcp.ikcp_input(bufferSpan);
            _udpDriver.ScheduleUpdate(ConnectionId, 0);
            
            while (ReceiveNextReliable(out var bufferRef))
            {
                Input(bufferRef);
            }
        }

        private bool ReceiveNextReliable(out SocketBufferRef result)
        {
            int messageLength = _kcp.ikcp_peeksize();
            if (messageLength <= 0)
            {
                result = new SocketBufferRef();
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
                result = new SocketBufferRef();
                OnError(ConnectionError.ERR_SOCKET_CANT_RECEIVE);
                return false;
            }

            result = bufferRef;
            return true;
        }
        #endregion
        
        private void OnError(int error)
        {
            uint connectionId = ConnectionId;
            Log.Error($"{nameof(TcpNetConnection)}[{connectionId}] error: {error}");
            _udpDriver.RemoveConnection(connectionId, error);
            _udpDriver.ReportConnectionError(connectionId, error);
        }
    }
}