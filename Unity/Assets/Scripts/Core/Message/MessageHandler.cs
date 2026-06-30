using System;
using Bliss.Pool;
using ET;

namespace Bliss
{
    /// <summary>
    /// 实体消息处理器
    /// <see cref="MessageHandlerAttribute"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public abstract class MessageHandler<TEntity,TMessage> : IMessageHandler
        where TEntity : Entity
        where TMessage : MessageObject, IMessage
    {
        protected abstract ETTask Run(TEntity clientScene, TMessage message);
        
        public async ETTask Handle(Entity entity, RootAddress fromAddress, MessageObject actorMessage)
        {
            if (entity is not TEntity targetEntity)
            {
                Log.Error($"Actor类型转换错误: {entity.GetType().FullName} to {typeof (TEntity).Name} --{typeof (TMessage).FullName}");
                return;
            }

            if (actorMessage is not TMessage message)
            {
                Log.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (TMessage).Name}");
                return;
            }
            
            await Run(targetEntity, message);
        }

        public Type GetRequestType()
        {
            return typeof (TMessage);
        }

        public Type GetResponseType()
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="MessageHandlerAttribute"/>
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public abstract class MessageHandler<TEntity, TRequest, TResponse> : IMessageHandler
        where TEntity : Entity
        where TRequest : MessageObject, IRequest
        where TResponse : MessageObject, IResponse
    {
        protected abstract ETTask Run(TEntity mapScene, TRequest request, TResponse response);

        public async ETTask Handle(Entity entity, RootAddress fromAddress, MessageObject actorMessage)
        {
            if (entity is not TEntity targetEntity)
            {
                Log.Error($"Actor类型转换错误: {entity.GetType().FullName} to {typeof (TEntity).Name} --{typeof (TRequest).FullName}");
                return;
            }

            if (actorMessage is not TRequest request)
            {
                Log.Error($"消息类型转换错误: {actorMessage.GetType().FullName} to {typeof (TRequest).Name}");
                return;
            }
            
            //只是告诉调用方, 消息已送达
            TResponse response = ObjectPool.Instance.Fetch<TResponse>();
            //防止后续业务里改变了RpcId
            int rpcId = request.RpcId;

            try
            {
                await Run(targetEntity, request,  response);
            }
            catch (RpcException exception)
            {
                response.Error = exception.ErrorCode;
                response.Message = exception.ToString();
            }
            catch (Exception exception)
            {
                response.Error = ErrorCode.ERR_RPC_FAIL;
                response.Message = exception.ToString();
                Log.Error(exception);
            }
            response.RpcId = rpcId;
            
            entity.RootScene().GetComponent<ProcessInnerMessageProxy>().Reply(fromAddress, response);
        }

        public Type GetRequestType()
        {
            return typeof (TRequest);
        }

        public Type GetResponseType()
        {
            return typeof (TResponse);
        }
    }
}