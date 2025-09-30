using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Bliss.Base.Collections;

namespace Bliss.Core.Network
{
    public class UdpNetDriver : IpNetDriver
    {
        private Socket _socket;
        private byte[] _receiveBuffer =  new byte[2048];
        private EndPoint _receiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private ConnectHandler _connectHandler;
        private Dictionary<EndPoint, UdpNetConnection> _connectionLookup = new Dictionary<EndPoint, UdpNetConnection>();
        
        
        private SortedMultiMapList<uint, uint> _updateSchedule = new SortedMultiMapList<uint, uint>();
        private List<uint> _updateConnectionId = new List<uint>();
        private List<uint> _timeOutTime = new List<uint>();
        private uint _minUpdateTime = 0;

        internal const int MessageHeaderLength = sizeof(byte) + sizeof(uint) + sizeof(uint);
        
        protected override void OnDispose()
        {
            _socket?.Close();
            _socket = null;
            foreach (var connection in _dicId2Connection.Values.ToList())
            {
                connection.Dispose();
            }
            _dicId2Connection.Clear();
            _connectionLookup.Clear();
        }

        public override void InitClient(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            Init(addressFamily);
        }
        
        public override void InitServer(IPEndPoint endPoint)
        {
            Init(endPoint.AddressFamily);
            _socket.Bind(endPoint);
            _connectHandler = new ConnectHandler(this);
        }
        
        private void Init(AddressFamily  addressFamily)
        {
            _socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public override void Update()
        {
            base.Update();

            uint timeNow = (uint)TimeNow;
            
            //Server Connection Cmp Update
            if (_connectHandler != null)
            {
                _connectHandler.Update(timeNow);
            }
            
            //Data Receive
            ReceiveIncomingData();

            //Connection Update
            TimerUpdate(timeNow);
            UpdateConnections(timeNow);
        }


        protected override void CreateConnection(uint connectionId, IPEndPoint remoteEndPoint)
        {
            var connection = new UdpNetConnection();
            _dicId2Connection.Add(connectionId, connection);
            connection.InitLocalConnection(this, remoteEndPoint, connectionId);
        }

        private uint CreateAcceptedConnection(uint connectionId, uint remoteId, IPEndPoint remoteEndPoint)
        {
            UdpNetConnection connection = new UdpNetConnection();
            connection.InitRemoteConnection(this, remoteEndPoint, connectionId, remoteId, connectionId);
            _dicId2Connection.Add(connectionId, connection);
            return connectionId;
        }

        
        public override void RemoveConnection(uint connectionId, int error = 0)
        {
            if (_dicId2Connection.Remove(connectionId, out ANetConnection connection))
            {
                connection.Error = error;
                connection.Dispose();
            }
        }

        internal void SendDisconnect(uint local, uint remoteId,  IPEndPoint remoteEndPoint)
        {
            if (!CheckValid())
            {
                return;
            }
            
            SocketBufferRef bufferRef = RentBuffer(MessageHeaderLength);
            {
                SocketBuffer buffer = bufferRef.Target;
                buffer.Reserve(MessageHeaderLength);
                buffer.Push(MessageHeaderLength)
                    .WriteByteTo((byte)EProtocolType.Fin)
                    .WriteUIntTo(local)
                    .WriteUIntTo(remoteId);
                Output(buffer.GetBuffer(), buffer.DataPosition, buffer.TrueLength, remoteEndPoint);
            }
            bufferRef.Dispose();
        }

        public override void Send(uint connectionId, SocketBufferRef socketBufferRef, bool isReliable)
        {
            if (!CheckValid())
            {
                return;
            }
            
            if (!TryGetConnection(connectionId, out var connection))
            {
                return;
            }
            
            connection.Send(socketBufferRef, isReliable);
        }

        internal void Output(byte[] buffer, int offset, int size, IPEndPoint remoteEndPoint)
        {
            if (!CheckValid())
            {
                return;
            }
            
            _socket.SendTo(buffer, offset, size, SocketFlags.None, remoteEndPoint);
        }

        private void ReceiveIncomingData()
        {
            if (_socket == null)
            {
                return;
            }

            while (_socket != null && _socket.Available > 0)
            {
                int messageLength =  _socket.ReceiveFrom(_receiveBuffer, ref _receiveEndPoint);
                
                try
                {
                    var packetSpan = new ReadOnlySpan<byte>(_receiveBuffer, 0, messageLength);
                
                    if (messageLength < MessageHeaderLength)
                    {
                        //不是合法的消息
                        continue;
                    }

                    packetSpan.ReadByte(out byte protoTypeBit);
                    
                    var protocolType = (EProtocolType)protoTypeBit;
                    switch (protocolType)
                    {
                        case EProtocolType.Handshake:
                            ProcessConnectionlessPacket(packetSpan);
                            break;
                        case EProtocolType.Msg:
                        case EProtocolType.Fin:
                            ProcessConnectedPacket(packetSpan, protocolType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
            
        }

        private void ProcessConnectionlessPacket(ReadOnlySpan<byte> bufferSpan)
        {
            bufferSpan.ReadByte(out var protoType)
                .ReadUInt(out uint clientId);

            if (TryGetConnection(clientId, out var connection))
            {
                if (connection is UdpNetConnection udpConnection && protoType == (byte)EProtocolType.Handshake)
                {
                    udpConnection.InputHandshakeRaw(bufferSpan);
                    return;
                }
            }

            if (_connectHandler != null)
            {
                IPEndPoint remoteEndPoint = (IPEndPoint)_receiveEndPoint;
                _connectHandler.InputConnectionless(bufferSpan, remoteEndPoint);

                if (_connectHandler.IsPassedChallenge(clientId, remoteEndPoint, out uint connectionId))
                {
                    CreateAcceptedConnection(connectionId, clientId, remoteEndPoint);
                    _receiveEndPoint = new IPEndPoint(IPAddress.Any, 0);
                }

                _connectHandler.ResetChallenge();
            }
        }

        private void ProcessConnectedPacket(ReadOnlySpan<byte> bufferSpan, EProtocolType protocolType)
        {
            bufferSpan.ReadByte(out var protoType)
                .ReadBool(out bool isReliable)
                .ReadUInt(out uint clientId)
                .ReadUInt(out uint remoteId)
                .ReadUInt(out uint conversationId);

            TryGetConnection(remoteId, out var connection);

            UdpNetConnection udpConnection = connection as UdpNetConnection;
            bool haveValidConnection = udpConnection != null 
                                       && udpConnection.ConnectionId == remoteId
                                       && udpConnection.RemoteId == clientId
                                       && udpConnection.ConversationId == conversationId;
            
            switch (protocolType)
            {
                case EProtocolType.Msg:
                    if (haveValidConnection)
                    {
                        udpConnection.InputRaw(bufferSpan);
                    }
                    else
                    {
                        //没有有效的Connection，通知对方断开
                        SendDisconnect(remoteId, clientId, (IPEndPoint) _receiveEndPoint);
                    }
                    break;
                case EProtocolType.Fin:
                    if (haveValidConnection)
                    {
                        SendDisconnect(remoteId, clientId, (IPEndPoint) _receiveEndPoint);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Connection Update 

        internal void ScheduleUpdate(uint connectionId, uint time)
        {
            _updateSchedule.Add(time, connectionId);
        }

        private void TimerUpdate(uint timeNow)
        {
            if (timeNow < _minUpdateTime)
            {
                return;
            }
            
            _timeOutTime.Clear();
            
            foreach (var kv in _updateSchedule)
            {
                uint timeToUpdate = kv.Key;
                if (timeToUpdate > timeNow)
                {
                    _minUpdateTime = timeToUpdate;
                    break;
                }
                _timeOutTime.Add(timeToUpdate);
            }

            foreach (var time in _timeOutTime)
            {
                foreach (var connectionId in _updateSchedule[time])
                {
                    _updateConnectionId.Add(connectionId);
                }

                _updateSchedule.Remove(time);
            }
        }

        private void UpdateConnections(uint timeNow)
        {
            foreach (var connectionId in _updateConnectionId)
            {
                if(TryGetConnection(connectionId, out var connection)
                   && connection is UdpNetConnection udpConnection)
                {
                    udpConnection.Update(timeNow);
                }
            }
            _updateConnectionId.Clear();
        }

        #endregion

        private bool CheckValid()
        {
            return !IsDisposed && _socket != null;
        }
    }
}