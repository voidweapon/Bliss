using System;
using ET;

namespace Bliss
{
    public abstract class MessageHandler<TEntity,TMessage> : IMessageHandler
        where TEntity : Entity
        where TMessage : IMessage
    {
        protected abstract ETTask Run(TEntity entity, TMessage message);
        
        public async ETTask Handle(Entity entity, MessageObject actorMessage)
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
}