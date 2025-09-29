using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Bliss.Core.Network
{
    public enum EHandsShakeType : byte
    {
        InitialPacket,
        Challenge,
        Response,
        Ack,
        RestartHandshake,
        RestartResponse,
    }
    internal class ConnectHandler
    {
        private UdpNetDriver _udpDriver;
        private UdpNetConnection _udpConnection;
        private bool _isServer = false;
        private EConnectState _state;
        private const int CHALLENGE_PAYLOAD_LENGTH = sizeof(byte) + sizeof(long) + COOKIE_BYTE_SIZE;
        private uint _nextUpdateTimeInterval;
        
        public ConnectHandler(UdpNetDriver udpDriver)
        {
            _isServer = true;
            _udpDriver = udpDriver;
            InitServer();
        }
        
        public ConnectHandler(UdpNetConnection udpConnection)
        {
            _isServer = false;
            _state = EConnectState.UnInitialized;
            _lastClientResponseCookie = new byte[COOKIE_BYTE_SIZE];
            _udpConnection = udpConnection;
            _udpDriver = (UdpNetDriver)udpConnection.NetDriver;
        }

        public uint Check(uint timeNow)
        {
            return _nextUpdateTimeInterval + timeNow;
        }
        
        public void Update(uint timeNow)
        {
            if (_isServer)
            {
                UpdateServer();
            }
            else
            {
                UpdateClient(timeNow);
            }
        }
        
        private void GenHandsShakePacketHeader(bool isRestartHandsShake, uint clientId, uint remoteId, EHandsShakeType  handsShakeType, SocketBufferRef bufferRef)
        {
            SocketBuffer buffer = bufferRef.Target;
            //bool isHandsShake = true;
            var span = buffer.Push(INIT_PACKET_HEADER_LENGTH);
            span.WriteByteTo((byte)EProtocolType.Handshake)
                .WriteUIntTo(clientId)
                .WriteUIntTo(remoteId)
                .WriteBoolTo(isRestartHandsShake)
                .WriteByteTo((byte)handsShakeType);
        }

        /// <summary>
        /// 解析握手包
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private HandshakePacket ParseHandshakePacket(ReadOnlySpan<byte> packet)
        {
            HandshakePacket packetResult = new HandshakePacket();
            
            packetResult.Error = 0;

            if (packet.Length < sizeof(bool))
            {
                packetResult.Error = 1;
                return packetResult;
            }
                
            packet = packet.ReadByte(out byte protoTypeBit);
            packetResult.Protocol =  (EProtocolType)protoTypeBit;;
            packetResult.IsHandshake =  packetResult.Protocol == EProtocolType.Handshake;
                
            if (!packetResult.IsHandshake)
            {
                if (packet.Length < sizeof(uint) + sizeof(uint))
                {
                    packetResult.Error = 2;
                    return packetResult;
                }
                
                packet.ReadUInt(out uint msgClientId)
                    .ReadUInt(out uint msgRemoteId);
                
                packetResult.ClientId = msgClientId;
                packetResult.RemoteId = msgRemoteId;
                
                return packetResult;
            }
                
            
            if (packet.Length < sizeof(bool) + sizeof(uint) + sizeof(uint)  + sizeof(byte))
            {
                packetResult.Error = 3;
                return packetResult;
            }

            packet = packet.ReadUInt(out uint clientId) 
                .ReadUInt(out uint remoteId) 
                .ReadBool(out bool isRestartHandsShake)
                .ReadByte(out byte handsShakeType);
                
            packetResult.IsRestartHandsShake =  isRestartHandsShake;
            packetResult.ClientId = clientId;
            packetResult.HandsShakeType = (EHandsShakeType)handsShakeType;

            if (handsShakeType == (byte)EHandsShakeType.InitialPacket
                || handsShakeType == (byte)EHandsShakeType.RestartHandshake)
            {
                //Do Nothing
            }
            else if (handsShakeType == (byte)EHandsShakeType.Challenge 
                     || handsShakeType == (byte)EHandsShakeType.Response
                     || handsShakeType == (byte)EHandsShakeType.RestartResponse)
            {
                if (packet.Length < sizeof(byte) + sizeof(long) + COOKIE_BYTE_SIZE)
                {
                    packetResult.Error = 4;
                    return packetResult;
                }
                
                packet = packet.ReadByte(out byte secretIndex)
                    .ReadLong(out long timestamp);   
                
                packetResult.SecretIndex =  secretIndex;
                packetResult.Timestamp = timestamp;
                packetResult.Cookie = packet.Slice(0, COOKIE_BYTE_SIZE);

                if (handsShakeType == (byte)EHandsShakeType.RestartResponse)
                {
                    if (packet.Length < COOKIE_BYTE_SIZE)
                    {
                        packetResult.Error = 5;
                        return packetResult;
                    }
                    packetResult.OriginRemoteId = remoteId;
                }
            }
            else if (handsShakeType == (byte)EHandsShakeType.Ack)
            {
                if (packet.Length < COOKIE_BYTE_SIZE)
                {
                    packetResult.Error = 6;
                    return packetResult;
                }
                
                packetResult.RemoteId = remoteId;
                packetResult.Cookie = packet.Slice(0, COOKIE_BYTE_SIZE);
            }
            
            return packetResult;
        }
        
        #region Client handshake

        private const int INIT_PACKET_HEADER_LENGTH = sizeof(byte) + sizeof(uint) + sizeof(uint) + sizeof(bool) + sizeof(byte);
        private const uint RESEND_INTERVAL_MS = 300;
        
        private long _lastClientSentTimeStamp;
        private long _lastClientChallengeTimeStamp;
        private byte _lastClientResponseSecretIndex;
        private long _lastClientResponseTimestamp;
        private readonly byte[] _lastClientResponseCookie;
        
        public void Input(ReadOnlySpan<byte> bufferSpan)
        {
            HandshakePacket packet = ParseHandshakePacket(bufferSpan);
            
            if (packet.IsHandshake)
            {
                if (packet.Error != 0)
                {
                    return;
                }
                
                if (packet.HandsShakeType == EHandsShakeType.Challenge)
                {
                    _lastClientChallengeTimeStamp = _udpDriver.TimeNow;
                    SendChallengeResponse(packet.SecretIndex, packet.Timestamp, packet.Cookie);
                }
                else if (packet.HandsShakeType == EHandsShakeType.Ack)
                {
                    _state = EConnectState.Initialized;
                    //uint conversationId = packet.Cookie.ToUInt();
                    _udpConnection.OnConnectCompleted(packet.RemoteId);
                }
            }
            
        }
        
        public void SendInitHandsShake()
        {
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(INIT_PACKET_HEADER_LENGTH);
            {
                SocketBuffer buffer = bufferRef.Target;
                buffer.Reserve(INIT_PACKET_HEADER_LENGTH);

                uint clientId = _udpConnection.ConnectionId;
                GenHandsShakePacketHeader(false, clientId, 0, EHandsShakeType.InitialPacket, bufferRef);
            
                SendToServer(bufferRef);
            }
            bufferRef.Dispose();
        }

        public void SendChallengeResponse(byte secretIndex, long timestamp, ReadOnlySpan<byte> cookie)
        {
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(CHALLENGE_PACKET_LENGTH);
            {
                SocketBuffer buffer = bufferRef.Target;
                buffer.Reserve(INIT_PACKET_HEADER_LENGTH);
                
                uint clientId = _udpConnection.ConnectionId;
                GenHandsShakePacketHeader(false, clientId, 0, EHandsShakeType.Response, bufferRef);

                var writer = buffer.Put(CHALLENGE_PAYLOAD_LENGTH)
                    .WriteByteTo(secretIndex)
                    .WriteLongTo(timestamp);
                
                cookie.CopyTo(writer);
                
                SendToServer(bufferRef);
                
                _state = EConnectState.InitializedOnLocal;
                _lastClientResponseSecretIndex = secretIndex;
                _lastClientResponseTimestamp = timestamp;
                cookie.CopyTo(_lastClientResponseCookie);
            }
            bufferRef.Dispose();
        }
        
        private void UpdateClient(uint timeNow)
        {
            if (_udpDriver == null)
            {
                return;
            }

            if (_state != EConnectState.Initialized && _lastClientSentTimeStamp != 0)
            {
                long currentTimeStamp = _udpDriver.TimeNow;
                if (currentTimeStamp - _lastClientSentTimeStamp > RESEND_INTERVAL_MS)
                {
                    _nextUpdateTimeInterval = RESEND_INTERVAL_MS;
                    bool isRestartHandsShake = currentTimeStamp - _lastClientChallengeTimeStamp > MIN_COOKIE_LIFETIME_MS;
                    if (isRestartHandsShake)
                    {
                        _state = EConnectState.Initialized;
                    }

                    if (_state == EConnectState.Initialized)
                    {
                        SendInitHandsShake();
                    }
                    else if (_state == EConnectState.InitializedOnLocal)
                    {
                        SendChallengeResponse(_lastClientResponseSecretIndex, _lastClientResponseTimestamp, _lastClientResponseCookie);
                    }
                }
            }
        }

        private void SendToServer(SocketBufferRef  bufferRef)
        {
            SocketBuffer buffer = bufferRef.Target;
            _udpConnection.OutputRaw(buffer.GetBuffer(), buffer.DataPosition, buffer.TrueLength);
            _lastClientSentTimeStamp = _udpDriver.TimeNow;
        }
        #endregion
        
        
        #region Server handshake

        private const int CHALLENGE_PACKET_LENGTH = INIT_PACKET_HEADER_LENGTH + CHALLENGE_PAYLOAD_LENGTH;
        private const int ACK_PACKET_LENGTH = INIT_PACKET_HEADER_LENGTH + COOKIE_BYTE_SIZE;
        
        private const int COOKIE_BYTE_SIZE = 20;
        private const int SECRET_BYTE_SIZE = 64;
        private const int SECRET_COUNT = 2;
        private const long SECRET_UPDATE_INTERVAL_MS = 15000;
        private const long SECRET_UPDATE_TIME_NOISE_MS = 5000;
        private const long MIN_COOKIE_LIFETIME_MS = SECRET_UPDATE_INTERVAL_MS;
        private const long MAX_COOKIE_LIFETIME_MS = (SECRET_UPDATE_INTERVAL_MS + SECRET_UPDATE_TIME_NOISE_MS) * SECRET_COUNT;
        
        private Random _secretRandom;
        private HMACSHA1  _secretHash;
        private byte[][] _handshakeSecret;
        private byte[] _cookieGenBuffer;
        private byte[] _cookieBuffer;
        private byte _activeSecret = byte.MaxValue;
        private long _lastSecretUpdateTimestamp;
        
        private IPEndPoint _lastPassedRemoteEndPoint;
        private uint _lastPassedClientId;
        private uint _acceptedConnectionId;

        private void InitServer()
        {
            _handshakeSecret = new byte[SECRET_COUNT][];
            _cookieGenBuffer = new byte[1024];
            _cookieBuffer = new byte[COOKIE_BYTE_SIZE];
            UpdateSecret();
        }

        public bool IsPassedChallenge(uint clientId, IPEndPoint remoteEndPoint, out uint connectionId)
        {
            connectionId = _acceptedConnectionId;
            return _lastPassedClientId ==  clientId
                   && _lastPassedRemoteEndPoint != null && remoteEndPoint != null
                   && _lastPassedRemoteEndPoint.Equals(remoteEndPoint);
        }

        public void ResetChallenge()
        {
            _lastPassedClientId = 0;
            _lastPassedRemoteEndPoint = null;
            _acceptedConnectionId = 0;
        }
        
        public void InputConnectionless(ReadOnlySpan<byte> bufferSpan, IPEndPoint remoteEndPoint)
        {
            HandshakePacket packet = ParseHandshakePacket(bufferSpan);

            if (packet.IsHandshake)
            {
                if (packet.Error != 0)
                {
                    return;
                }
                
                if (packet.HandsShakeType == EHandsShakeType.InitialPacket && packet.Cookie.IsEmpty)
                {
                    SendConnectChallenge(packet.ClientId, remoteEndPoint);
                }
                else if (packet.HandsShakeType == EHandsShakeType.Response ||  packet.HandsShakeType == EHandsShakeType.RestartResponse)
                {
                    //校验Cookie和SecretId
                    float cookieDelta = _udpDriver.TimeNow - packet.Timestamp;
                    float secretDelta = packet.Timestamp -  _lastSecretUpdateTimestamp;
                    bool isValidCookieLiftTime = cookieDelta >= 0 && cookieDelta < MAX_COOKIE_LIFETIME_MS;
                    bool isValidSecretLiftTime = packet.SecretIndex == _activeSecret ? secretDelta >= 0 : secretDelta <= 0;

                    if (isValidCookieLiftTime && isValidSecretLiftTime)
                    {
                        var regenSpan = new Span<byte>(_cookieBuffer, 0, COOKIE_BYTE_SIZE);
                        GenerateCookie(packet.SecretIndex, remoteEndPoint, packet.Timestamp, regenSpan);
                    
                        bool challengeSuccess = packet.Cookie.SequenceEqual(regenSpan);
                        if (challengeSuccess)
                        {
                            SendChallengeAck(packet.ClientId, remoteEndPoint, regenSpan);
                        }
                    }
                }
            }
            else if(packet.Error == 0 && packet.Protocol == EProtocolType.Msg)
            {
                //TODO: Restart HandsShake
                SendRestartChallenge(packet.ClientId, remoteEndPoint);
            }
        }

        private void SendConnectChallenge(uint clientId, IPEndPoint remoteEndPoint)
        {
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(CHALLENGE_PACKET_LENGTH);
            {
                SocketBuffer buffer = bufferRef.Target;
                buffer.Reserve(INIT_PACKET_HEADER_LENGTH);
                GenHandsShakePacketHeader(false, clientId, 0, EHandsShakeType.Challenge, bufferRef);
            
                long timeStamp = _udpDriver.TimeNow;
                buffer.Put(sizeof(byte) + sizeof(long))
                    .WriteByteTo(_activeSecret)
                    .WriteLongTo(timeStamp);
            
                var cookieSpan = buffer.Put(COOKIE_BYTE_SIZE);
                GenerateCookie(_activeSecret, remoteEndPoint, timeStamp, cookieSpan);
                SendToClient(bufferRef, remoteEndPoint);
            }
            bufferRef.Dispose();
        }

        private void SendChallengeAck(uint clientId, IPEndPoint remoteEndPoint, ReadOnlySpan<byte> cookieSpan)
        {
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(ACK_PACKET_LENGTH);
            {
                SocketBuffer buffer = bufferRef.Target;
                buffer.Reserve(INIT_PACKET_HEADER_LENGTH);
                uint connectionId =  _udpDriver.CreateRemoteConnectionId();
                GenHandsShakePacketHeader(false, clientId, connectionId, EHandsShakeType.Ack, bufferRef);
                
                cookieSpan.CopyTo(buffer.Put(COOKIE_BYTE_SIZE));
                SendToClient(bufferRef, remoteEndPoint);
                
                _lastPassedRemoteEndPoint = remoteEndPoint;
                _lastPassedClientId =  clientId;
                _acceptedConnectionId = connectionId;
            }
            bufferRef.Dispose();
        }

        private void SendRestartChallenge(uint clientId, IPEndPoint remoteEndPoint)
        {
            SocketBufferRef bufferRef = _udpDriver.RentBuffer(INIT_PACKET_HEADER_LENGTH);
            {
                SocketBuffer buffer = bufferRef.Target;
                buffer.Reserve(INIT_PACKET_HEADER_LENGTH);
                GenHandsShakePacketHeader(true, clientId, 0, EHandsShakeType.RestartHandshake, bufferRef);

                SendToClient(bufferRef, remoteEndPoint);
            }
            bufferRef.Dispose();
        }
        
        private void GenerateCookie( byte secretId, IPEndPoint remoteEndPoint, long timeStamp, Span<byte> destination)
        {
            var writer = new Span<byte>(_cookieGenBuffer);
            int count = 0;
            count += writer.WriteString(remoteEndPoint.ToString());
            writer = writer.Slice(count);
            writer.WriteLong(timeStamp);
            count += sizeof(long);
            
            var source = new ReadOnlySpan<byte>(_cookieGenBuffer, 0, count);
            
            _secretHash.Key = _handshakeSecret[secretId];
            _secretHash.TryComputeHash(source, destination, out _);
        }

        private void SendToClient(SocketBufferRef bufferRef, IPEndPoint remoteEndPoint)
        {
            SocketBuffer buffer = bufferRef.Target;
            try
            {
                _udpDriver.Output(buffer.GetBuffer(), buffer.DataPosition, buffer.TrueLength, remoteEndPoint);
            }
            catch
            {
                // ignored
            }
        }

        private void UpdateServer()
        {
            if (_udpDriver == null)
            {
                return;
            }

            float intervalNoise = _secretRandom.RandomNumber(0, SECRET_UPDATE_TIME_NOISE_MS);
            if (_udpDriver.TimeNow - _lastSecretUpdateTimestamp > (SECRET_UPDATE_INTERVAL_MS + intervalNoise) * 1000)
            {
                UpdateSecret();
            }
        }
        
        private void UpdateSecret()
        {
            _lastSecretUpdateTimestamp =  _udpDriver?.TimeNow ?? 0;
            
            if (_activeSecret == byte.MaxValue)
            {
                _secretRandom = new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount);
                for (var i = 0; i < SECRET_COUNT; i++)
                {
                    _handshakeSecret[i] = new byte[SECRET_BYTE_SIZE];
                    _secretRandom.NextBytes(_handshakeSecret[i]);
                }

                _activeSecret = 0;
                _secretHash = new HMACSHA1(_handshakeSecret[_activeSecret]);
            }
            
            _activeSecret = (byte)((_activeSecret + 1) %  SECRET_COUNT);
            
            byte[] currentSecret = _handshakeSecret[_activeSecret];
            for (int i = 0; i < SECRET_BYTE_SIZE; i++)
            {
                _secretRandom.NextBytes(currentSecret);
            }
        }
        #endregion


        private enum EConnectState
        {
            UnInitialized,
            InitializedOnLocal,
            InitializeOnRemote,
            Initialized,
        }
        
        private ref struct HandshakePacket
        {
            public int Error;
            public EProtocolType Protocol;
            public bool IsHandshake;
            public bool IsRestartHandsShake;
            public uint ClientId;
            public EHandsShakeType  HandsShakeType;
            public byte SecretIndex;
            public long Timestamp;
            public ReadOnlySpan<byte> Cookie;
            //public ReadOnlySpan<byte> OriginCookie;
            public uint RemoteId;
            public uint OriginRemoteId;
        }
    }
}