using System;
using System.IO;
using System.Net.Sockets;
using Bliss.IO;

namespace Bliss.Core.Network
{
    public class StreamPacketParser
    {
        public int HeaderLength => _packetHeader.Length;
        
        private ANetDriver _netDriver;
        private RingStream _sendBuffer = new RingStream();
        private RingStream _receiveBuffer = new RingStream();
        private byte[] _packetHeader;
        
        private const int CSendBufferLimit = 8192 * 16;
        private const int CReceiveBufferLimit = 8192 * 16;
        public const int ERR_SENDBUFF_TOOLARGE = -2;
        
        public StreamPacketParser(ANetDriver netDriver, int packetHeaderSize)
        {
            if (packetHeaderSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(packetHeaderSize), "Packet header size must be greater than 0");
            }
            _packetHeader =  new byte[packetHeaderSize];
            _netDriver = netDriver;
        }

        #region Send Process

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socketBufferRef"></param>
        /// <returns>-2: buffer full, >=0: send size</returns>
        public int Send(SocketBufferRef socketBufferRef)
        {
            if (_sendBuffer.Length > CSendBufferLimit)
            {
                return ERR_SENDBUFF_TOOLARGE;
            }
            
            SocketBuffer buffer = socketBufferRef.Target;
            int messageSize = buffer.TailPosition - buffer.HeadPosition;
            
            buffer.Reserve(_packetHeader.Length);
            BitConverter.TryWriteBytes(_packetHeader, messageSize);
            buffer.Push(_packetHeader, 0, _packetHeader.Length);
            
            buffer.Seek(buffer.DataPosition, SeekOrigin.Begin);
            buffer.SetLength(buffer.TailPosition);
            _sendBuffer.Write(buffer);

            return messageSize + _packetHeader.Length;
        }

        public bool Output(out byte[] buffer, out int offset, out int length)
        {
            if (_sendBuffer.Length == 0)
            {
                buffer = null;
                offset = 0;
                length = 0;
                return false;
            }

            int sendSize = System.Math.Min((int)_sendBuffer.Length, _sendBuffer.HeadChunkLength);
            buffer = _sendBuffer.HeadChunk;
            offset = _sendBuffer.HeadChunkPosition;
            length = sendSize;
            
            return true;
        }

        public void OutputCompleted(int bytesTransferred)
        {
            _sendBuffer.Pull(bytesTransferred);
        }

        #endregion
        
        
        
        private EParserState _parserState;
        private int _packetSize;
        
        public void SetSocketReceiveBuffer(SocketAsyncEventArgs receiveArgs)
        {
            int receiveSize = _receiveBuffer.ChunkSize - _receiveBuffer.TailChunkPosition;
            receiveArgs.SetBuffer(_receiveBuffer.TailChunk, _receiveBuffer.TailChunkPosition, receiveSize);
        }
        
        public int Input(int count)
        {
            _receiveBuffer.Put(count);
            return count;
        }

        public int Receive(out SocketBufferRef? bufferRef)
        {
            bufferRef = null;

            while (true)
            {
                switch (_parserState)
                {
                    case EParserState.Head:
                    {
                        if (_receiveBuffer.Length < _packetHeader.Length)
                        {
                            bufferRef = null;
                            return 0;
                        }
                        ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>(_receiveBuffer.HeadChunk, 0, _packetHeader.Length);
                        _packetSize = buffer.ToAdaptiveInt();

                        if (_packetSize < ProtocolDef.PacketMaxSize || _packetSize > ProtocolDef.PacketMaxSize)
                        {
                            return ConnectionError.ERR_INVALID_PACKET;
                        }

                        _parserState = EParserState.Payload;
                        break;
                    }
                    case EParserState.Payload:

                        if (_receiveBuffer.Length > CReceiveBufferLimit)
                        {
                            bufferRef = null;
                            return ConnectionError.ERR_RECEIVEBUFF_TOOLARGE;
                        }

                        //消息没收全，继续接收
                        if (_receiveBuffer.Length < _packetSize)
                        {
                            return 0;
                        }

                        SocketBufferRef rentBufferRef = _netDriver.RentBuffer(_packetSize);
                        SocketBuffer skBuf = rentBufferRef.Target;
                        skBuf.Reserve(_packetSize);
                        Span<byte> span =  skBuf.Push(_packetSize);
                        _receiveBuffer.Read(span);
                        
                        bufferRef = rentBufferRef;
                    
                        _parserState = EParserState.Head;
                        return 0;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private  enum EParserState
        {
            Head,
            Payload,
        }
    }
}