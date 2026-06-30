using System;
using Bliss.Pool;
using ET;

namespace Bliss
{
    public abstract class LocationMessageHandler<TEntity,TMessage> : IMessageHandler
        where TEntity : Entity
        where TMessage : MessageObject, ILocationMessage
    {
        protected abstract ETTask Run(TEntity entity, TMessage message);
        
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
         
            //用通用Response告诉调用方, 消息已送达
            ResponseMessage response = ObjectPool.Instance.Fetch<ResponseMessage>();
            response.RpcId = message.RpcId;
            entity.RootScene().GetComponent<ProcessInnerMessageProxy>().Reply(fromAddress, response);
        
            await Run(targetEntity, message);
        }

        public Type GetRequestType()
        {
            return typeof (TMessage);
        }

        public Type GetResponseType()
        {
            return typeof(ResponseMessage);
        }
    }

    public abstract class LocationMessageHandler<TEntity,TRequest, TResponse> : IMessageHandler
        where TEntity : Entity
        where TRequest : MessageObject, ILocationRequest
        where TResponse : MessageObject, ILocationResponse
    {
        protected abstract ETTask Run(TEntity entity, TRequest request, TResponse response);
        
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
            }
            response.RpcId = rpcId;


            // 这里是为了保证response消息在handler消息处理完成之后发出，
            // 因为MessageLocationSenderComponentSystem里面的Send方法有可能需要从location获取actorid
            // 这样会导致send实际上进入了新的协程，从而response却先发送出去了
            using (await entity.RootScene().GetComponent<CoroutineLockComponent>() .WaitAsync(ECoroutineLockType.MessageLocationSender, entity.Id))
            {
                entity.RootScene().GetComponent<ProcessInnerMessageProxy>().Reply(fromAddress, response);
            }
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