using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Bliss.IO;

namespace Bliss.Core.Network
{
    public class TcpNetConnection : IpNetConnection
    {
        private Socket _socket;
        private SocketAsyncEventArgs _receiveEventArgs =  new SocketAsyncEventArgs();
        private SocketAsyncEventArgs _sendEventArgs = new SocketAsyncEventArgs();
        private TcpNetDriver _tcpNetDriver;
        private EndPoint _remoteEndPoint;
        private bool _isSending;
        private StreamPacketParser _streamPacketParser;

        protected override void OnDispose()
        {
            uint id = ConnectionId;
            ConnectionId = 0;
            NetDriver.RemoveConnection(id);
            
            _socket.Close();
            _receiveEventArgs.Dispose();
            _sendEventArgs.Dispose();
            
            _socket = null;
            _receiveEventArgs = null;
            _sendEventArgs = null;
            _tcpNetDriver = null;
        }

        /// <summary>
        /// Init Incoming Connection
        /// Remote -> Local
        /// </summary>
        internal void InitRemoteConnection(IpNetDriver netDriver, Socket socket, uint connectionId)
        {
            InternalInit(netDriver,socket, connectionId);
            State = ENetConnectionState.Open;
            ConnectionType = EIpNetConnectionType.Remote;

            _remoteEndPoint = socket.RemoteEndPoint;

        }

        /// <summary>
        /// Init Outcoming Connection
        /// Local -> Remote
        /// </summary>
        internal void InitLocalConnection(IpNetDriver netDriver, Socket socket, IPEndPoint  remoteEndPoint, uint connectionId)
        {
            InternalInit(netDriver, socket, connectionId);
            State = ENetConnectionState.Pending;
            ConnectionType = EIpNetConnectionType.Local;
            _remoteEndPoint =  remoteEndPoint;
            
            _tcpNetDriver.EnqueueOperation(new ConnectionOperate()
            {
                Operation = EConnectionOperate.StartConnect,
                ConnectionId = connectionId
            });
        }

        private void InternalInit(IpNetDriver netDriver, Socket socket, uint connectionId)
        {
            _socket = socket;
            NetDriver = netDriver;
            ConnectionId  = connectionId;
            _tcpNetDriver = (TcpNetDriver)netDriver;;
            _streamPacketParser = new StreamPacketParser(netDriver, sizeof(int));            
            
            _isSending = false;
            socket.NoDelay = true;
            _receiveEventArgs.Completed += SocketOpCompleted;
            _sendEventArgs.Completed += SocketOpCompleted;
        }
        
        private void SocketOpCompleted(object sender, SocketAsyncEventArgs eventArgs)
        {
            //queue socket op and execute in next frame 
            _tcpNetDriver.EnqueueOperation(new ConnectionOperate()
            {
                Operation = EConnectionOperate.SocketOp,
                ConnectionId = ConnectionId,
                EventArgs = eventArgs,
            });
        }

        #region Connect Op
        
        internal void StartConnectAsync()
        {
            if(_socket == null) return;
            
            _sendEventArgs.RemoteEndPoint = _remoteEndPoint;
            bool isOperatePending = _socket.ConnectAsync(_sendEventArgs);
            if (isOperatePending)
            {
                return;
            }

            //Connect Operate Complete Immediately
            OnConnectCompleted(_sendEventArgs);
        }

        internal void OnConnectCompleted(SocketAsyncEventArgs eventArgs)
        {
            if(_socket == null) return;

            if (eventArgs.SocketError != SocketError.Success)
            {
                OnError((int)eventArgs.SocketError);
                return;
            }
            
            State = ENetConnectionState.Open;
            eventArgs.RemoteEndPoint = null;
            
            //开始接收和发送数据
            
            _tcpNetDriver.EnqueueOperation(new ConnectionOperate()
            {
                Operation = EConnectionOperate.StartSend,
                ConnectionId = ConnectionId
            });
            
            _tcpNetDriver.EnqueueOperation(new ConnectionOperate()
            {
                Operation = EConnectionOperate.StartReceive,
                ConnectionId = ConnectionId
            });
        }
        
        #endregion

        #region Disconnect Op

        internal void OnDisconnectCompleted(SocketAsyncEventArgs eventArgs)
        {
            OnError((int)eventArgs.SocketError);
        }

        #endregion
        
        #region Send Op
        internal void StartSendAsync()
        {
            if(_socket == null) return;
            
            if (State != ENetConnectionState.Open)
            {
                return;
            }

            if (_isSending)
            {
                return;
            }

            while (true)
            {
                if (_socket == null || State != ENetConnectionState.Open)
                {
                    _isSending = false;
                    return;
                }
                
                if (!_streamPacketParser.Output(out byte[] buffer, out int offset, out int sendSize))
                {
                    _isSending = false;
                }
                
                _isSending = true;

                _sendEventArgs.SetBuffer(buffer, offset, sendSize);
                
                bool isOperatePending = _socket.SendAsync(_sendEventArgs);
                if (isOperatePending)
                {
                    return;
                }
                
                //Send Operate Complete Immediately, and try to send remain data
               int error =  OnSendPartialCompleted(_sendEventArgs);
               if (error != (int)SocketError.Success)
               {
                    _isSending = false;
                    OnError(error);
                   return;
               }
            }
            
        }

        internal void OnSendCompleted(SocketAsyncEventArgs eventArgs)
        {
            _isSending = false;
            
            int error = OnSendPartialCompleted(eventArgs);
            if (error != (int)SocketError.Success)
            {
                OnError(error);
                return;
            }
            
            //TODO: Send出错时是否需要再次尝试Send？是否需要断开Socket？
            
            _tcpNetDriver.EnqueueOperation(new ConnectionOperate()
            {
                Operation = EConnectionOperate.StartSend,
                ConnectionId = ConnectionId
            });
        }

        private int OnSendPartialCompleted(SocketAsyncEventArgs eventArgs)
        {
            if (_socket == null)
            {
                return (int) SocketError.NotSocket;
            }

            if (eventArgs.SocketError != SocketError.Success)
            {
                return (int)eventArgs.SocketError;
            }

            if (eventArgs.BytesTransferred == 0)
            {
                return ConnectionError.ERR_PEER_DISCONNECT;
            }
            
            _streamPacketParser.OutputCompleted(eventArgs.BytesTransferred);
            
            return (int) SocketError.Success;
        }
        
        #endregion
        
        #region Receive Op

        internal void StartReceiveAsync()
        {
            while (true)
            {
                if (_socket == null || State != ENetConnectionState.Open)
                {
                    return;
                }

                _streamPacketParser.SetSocketReceiveBuffer(_receiveEventArgs);
                
                bool isOperatePending = _socket.ReceiveAsync(_receiveEventArgs);
                if (isOperatePending)
                {
                    return;
                }
                
                //Receive Operate Complete Immediately, and try to receive remain data
                int error = OnReceivePartialCompleted(_receiveEventArgs);
                if (error != (int)SocketError.Success)
                {
                    OnError(error);
                    return;
                }
            }
        }

        internal void OnReceiveCompleted(SocketAsyncEventArgs eventArgs)
        {
            int error = OnReceivePartialCompleted(eventArgs);

            if (_socket == null)
            {
                return;
            }

            if (error != (int)SocketError.Success)
            {
                OnError(error);
                return;
            }
            
            _tcpNetDriver.EnqueueOperation(new ConnectionOperate()
            {
                Operation = EConnectionOperate.StartReceive,
                ConnectionId = ConnectionId
            });
        }

        private int OnReceivePartialCompleted(SocketAsyncEventArgs eventArgs)
        {
            if (_socket == null)
            {
                return (int) SocketError.NotSocket;
            }

            if (eventArgs.SocketError != SocketError.Success)
            {
                return (int)eventArgs.SocketError;
            }

            if (eventArgs.BytesTransferred == 0)
            {
                return ConnectionError.ERR_PEER_DISCONNECT;
            }
            
            _streamPacketParser.Input(eventArgs.BytesTransferred);
            
            //解析数据流，将整个消息从流里捞出来，提交个上层
            while (true)
            {
                int error = _streamPacketParser.Receive(out SocketBufferRef? receiveBuffer);

                if (error != (int)SocketError.Success)
                {
                    return error;
                }
                
                if (receiveBuffer == null)
                {
                    break;
                }
                
                Input(receiveBuffer.Value);
            }
            
            return (int) SocketError.Success;
        }
        
        #endregion

        public sealed override void Send(SocketBufferRef socketBufferRef)
        {
            if (IsDisposed)
            {
                throw new Exception($"Connection is disposed.");
            }
            
            int error = _streamPacketParser.Send(socketBufferRef);
            if (error == StreamPacketParser.ERR_SENDBUFF_TOOLARGE)
            {
                OnError(ConnectionError.ERR_SENDBUFF_TOOLARGE);
                return;
            }
            
            if (!_isSending)
            {
                _tcpNetDriver.EnqueueOperation(new ConnectionOperate()
                {
                    Operation = EConnectionOperate.StartSend,
                    EventArgs = _sendEventArgs,
                    ConnectionId = ConnectionId,
                });
            }
            
            socketBufferRef.Dispose();
        }

        // public override void Input(SocketBufferRef socketBufferRef)
        // {
        //     SocketBuffer buffer = socketBufferRef.Target;
        //     int bytesReceived = buffer.DataPosition - buffer.TailPosition;
        //     
        //     base.Input(socketBufferRef);
        // }

        private void OnError(int error)
        {
            uint connectionId = ConnectionId;
            Log.Error($"{nameof(TcpNetConnection)}[{connectionId}] error: {error}");
            _tcpNetDriver.RemoveConnection(connectionId, error);
            _tcpNetDriver.ReportConnectionError(connectionId, error);
        }
        
    }
}