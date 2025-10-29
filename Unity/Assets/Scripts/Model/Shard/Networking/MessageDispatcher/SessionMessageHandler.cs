using System;
using Bliss.Core;
using Bliss.Pool;
using ET;

namespace Bliss
{
    public abstract class SessionMessageHandler<TMessage> : ISessionMessageHandler where TMessage : MessageObject
    {
        public void Handle(Session session, object message)
        {
            HandleAsync(session, message).Coroutine();
        }

        public Type GetMessageType()
        {
            return typeof(TMessage);
        }

        public Type GetResponseType()
        {
            return null;
        }

        private async ETTask HandleAsync(Session session, object message)
        {
            if (message == null)
            {
                Log.Error($"消息解析失败:{typeof (TMessage).Name}");
                return;
            }
            
            if (message is not TMessage messageObject)
            {
                Log.Error($"消息类型转换错误: {message.GetType().FullName} to {typeof (TMessage).Name}");
                return;
            }

            if (session.IsDisposed)
            {
                Log.Error($"session disconnect {message}");
                return;
            }

            await Run(session, messageObject);
        }
        
        protected abstract ETTask Run(Session session, TMessage message);
    }
    
    
    public  abstract class SessionMessageHandler<TRequest, TResponse> : ISessionMessageHandler 
        where TRequest : MessageObject, IRequest
        where TResponse : MessageObject, IResponse
    {
        public void Handle(Session session, object message)
        {
            HandleAsync(session, message).Coroutine();
        }

        public Type GetMessageType()
        {
            return typeof(TRequest);
        }

        public Type GetResponseType()
        {
            return typeof(TResponse);
        }

        private async ETTask HandleAsync(Session session, object message)
        {
            try
            {
                if (message == null)
                {
                    Log.Error($"消息解析失败:{typeof (TRequest).Name}");
                    return;
                }
            
                if (message is not TRequest request)
                {
                    Log.Error($"消息类型转换错误: {message.GetType().FullName} to {typeof (TRequest).Name}");
                    return;
                }

                if (session.IsDisposed)
                {
                    Log.Error($"session disconnect {message}");
                    return;
                }
            
                int rpcId = request.RpcId;
                long instanceId = session.InstanceId;
            
                using var response = ObjectPool.Instance.Fetch<TResponse>();
                try
                {
                    await Run(session, request, response);
                }
                catch (RpcException rpcException)
                {
                    // 这里不能返回堆栈给客户端
                    Log.Error(rpcException.ToString());
                    response.Error = rpcException.ErrorCode;
                }
                catch (Exception exception)
                {
                    // 这里不能返回堆栈给客户端
                    Log.Error(exception.ToString());
                    response.Error = ConnectionError.ERR_RpcFail;
                }
            
                // 等回调回来,session可以已经断开了,所以需要判断session InstanceId是否一样
                if (session.InstanceId != instanceId)
                {
                    return;
                }
            
                response.RpcId = rpcId; // 在这里设置rpcId是为了防止在Run中不小心修改rpcId字段
                session.Send(response);
            }
            catch (Exception e)
            {
                throw new Exception($"处理消息失败: {message.GetType().FullName}", e);
            }
        }
        
        protected abstract ETTask Run(Session session, TRequest message, TResponse response);
    }
}