using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Bliss.Core.Network
{
    public class TcpNetDriver : IpNetDriver
    {
        private const int c_pendingQueueLimit = 1024;
        private ConcurrentQueue<ConnectionOperate> _tcpOperateQueue = new ConcurrentQueue<ConnectionOperate>();

        
        protected override void OnDispose()
        {
            _acceptor?.Close();
            _acceptor = null;
            _acceptEventArgs?.Dispose();
            _acceptEventArgs = null;

            foreach (var connection in _dicId2Connection.Values.ToList())
            {
                connection.Dispose();
            }
            _dicId2Connection.Clear();
            
            _tcpOperateQueue.Clear();
        }

        public override void InitClient(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            //do nothing
        }

        #region Server Code
        /// <summary>
        /// 用于接收客户端连接请求
        /// </summary>
        private Socket _acceptor;
        private SocketAsyncEventArgs _acceptEventArgs;
        
        public override void InitServer(IPEndPoint bindEndPoint)
        {
            _acceptor = new Socket(bindEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _acceptEventArgs = new SocketAsyncEventArgs();
            _acceptEventArgs.Completed += OnAcceptorOpCompleted;

            try
            {
                _acceptor.Bind(bindEndPoint);
                _acceptor.Listen(c_pendingQueueLimit);
            }
            catch (Exception e)
            {
                throw new Exception($"Init acceptor socket error: {bindEndPoint}", e);
            }
            
            StartAcceptAsync();
        }
        
        #region Accept Op

        private void StartAcceptAsync()
        {
            _acceptEventArgs.AcceptSocket = null;
            bool isOperationPending = _acceptor.AcceptAsync(_acceptEventArgs);
            if (isOperationPending)
            {
                return;
            }

            OnAcceptCompleted(_acceptEventArgs);
        }
        
        private void OnAcceptorOpCompleted(object sender, SocketAsyncEventArgs eventArgs)
        {
            switch (eventArgs.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    _tcpOperateQueue.Enqueue(new ConnectionOperate() { EventArgs = eventArgs });
                    break;
                default:
                    throw new InvalidOperationException($"not supported operation {eventArgs.LastOperation}");
            }
        }
        
        private void OnAcceptCompleted(SocketAsyncEventArgs eventArgs)
        {
            if (_acceptor == null)
            {
                return;
            }

            if (eventArgs.SocketError != SocketError.Success)
            {
                //接收连接出错
                Log.Error($"accept socket error: {eventArgs.SocketError}");
                StartAcceptAsync();
                return;
            }

            try
            {
                uint connectionId =  CreateConnection(eventArgs.AcceptSocket);
                ReportAcceptConnection(connectionId, (IPEndPoint)eventArgs.RemoteEndPoint);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            
            StartAcceptAsync();
        }

        #endregion
        
        #endregion
        
        public override void Update()
        {
            base.Update();

            while (_tcpOperateQueue.TryDequeue(out var tcpOperate))
            {
                switch (tcpOperate.Operation)
                {
                    case EConnectionOperate.StartConnect:
                    {
                        if (TryGetConnection(tcpOperate.ConnectionId, out var connection) 
                            && connection is TcpNetConnection tcpNetConnection)
                        {
                            tcpNetConnection.StartConnectAsync();
                        }
                        break;
                    }
                    case EConnectionOperate.StartSend:
                    {
                        if (TryGetConnection(tcpOperate.ConnectionId, out var connection) 
                            && connection is TcpNetConnection tcpNetConnection)
                        {
                            tcpNetConnection.StartSendAsync();
                        }
                        break;
                    }
                    case EConnectionOperate.StartReceive:
                    {
                        if (TryGetConnection(tcpOperate.ConnectionId, out var connection) 
                            && connection is TcpNetConnection tcpNetConnection)
                        {
                            tcpNetConnection.StartReceiveAsync();
                        }
                        break;
                    }
                    case EConnectionOperate.SocketOp:
                    {
                        OnSocketOpCompleted(tcpOperate.ConnectionId, tcpOperate.EventArgs);
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"not supported operation {tcpOperate.GetType()}");
                }
            }
        }

        private void OnSocketOpCompleted(uint connectionId, SocketAsyncEventArgs eventArgs)
        {
            switch (eventArgs.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                {
                    OnAcceptCompleted(eventArgs);
                    break;
                }
                case SocketAsyncOperation.Connect:
                {
                    if (TryGetConnection(connectionId, out var connection) 
                        && connection is TcpNetConnection tcpNetConnection)
                    {
                        tcpNetConnection.OnConnectCompleted(eventArgs);
                    }
                    break;
                }
                case SocketAsyncOperation.Disconnect:
                {
                    if (TryGetConnection(connectionId, out var connection) 
                        && connection is TcpNetConnection tcpNetConnection)
                    {
                        tcpNetConnection.OnDisconnectCompleted(eventArgs);
                    }
                    break;
                }
                case SocketAsyncOperation.Send:
                {
                    if (TryGetConnection(connectionId, out var connection) 
                        && connection is TcpNetConnection tcpNetConnection)
                    {
                        tcpNetConnection.OnSendCompleted(eventArgs);
                    }
                    break;
                }
                case SocketAsyncOperation.Receive:
                {
                    if (TryGetConnection(connectionId, out var connection) 
                        && connection is TcpNetConnection tcpNetConnection)
                    {
                        tcpNetConnection.OnReceiveCompleted(eventArgs);
                    }
                    break;
                }
                default:
                    throw new InvalidOperationException($"not supported socket operation {eventArgs.LastOperation}");
            }
        }
        
        /// <summary>
        /// Queue Connection operation and excute in next frame
        /// </summary>
        /// <param name="operation"></param>
        internal void EnqueueOperation(ConnectionOperate  operation)
        {
            _tcpOperateQueue.Enqueue(operation);
        }
        
        private uint CreateConnection(Socket acceptedSocket)
        {
            uint connectionId = CreateRemoteConnectionId();
            TcpNetConnection connection = new TcpNetConnection();
            connection.InitRemoteConnection(this, acceptedSocket, connectionId);
            _dicId2Connection.Add(connectionId, connection);
            return connectionId;
        }

        protected override void CreateConnection(uint connectionId, IPEndPoint remoteEndPoint)
        {
            var connection = new TcpNetConnection();
            _dicId2Connection.Add(connectionId, connection);
            var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            connection.InitLocalConnection(this, socket, remoteEndPoint, connectionId);
        }

        public override void RemoveConnection(uint connectionId, int error = 0)
        {
            if (_dicId2Connection.Remove(connectionId, out ANetConnection connection))
            {
                connection.Error = error;
                connection.Dispose();
            }
        }
        
        
        public override void Send(uint connectionId, SocketBufferRef socketBufferRef, bool isReliable)
        {
            if (!TryGetConnection(connectionId, out var connection))
            {
                return;
            }
            
            connection.Send(socketBufferRef, true);
        }
    }

    internal enum EConnectionOperate
    {
        //Socket Op
        SocketOp,
        
        //Connection Op
        
        /// <summary>
        /// Connection Require Connect
        /// </summary>
        StartConnect,
        /// <summary>
        /// Connection Require Send Data
        /// </summary>
        StartSend,
        /// <summary>
        /// Connection Require Receive Data
        /// </summary>
        StartReceive,
    }
    
    internal struct ConnectionOperate
    {
        public EConnectionOperate Operation;
        public uint ConnectionId;
        public SocketAsyncEventArgs EventArgs;
    }
    

    
}