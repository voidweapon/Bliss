using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using ET;

namespace Bliss
{
    [SingletonManger]
    public class MessageDispatcher : Singleton<MessageDispatcher>, ISingletonInit
    {
        private readonly MultiMapList<Type, MessageDispatcherInfo> _messageHandlers = new MultiMapList<Type, MessageDispatcherInfo>();
        public void Init()
        {
            _messageHandlers.Clear();
            var messageHandleTypes = TypeManager.Instance.GetTypes(typeof(MessageHandlerAttribute));
            foreach (var messageHandleType in messageHandleTypes)
            {
                Register(messageHandleType);
            }
        }

        private void Register(Type type)
        {
            IMessageHandler messageHandler = Activator.CreateInstance(type) as IMessageHandler;

            if (messageHandler == null)
            {
                throw new Exception($"message handler not inherit IMActorHandler abstract class: {type.FullName}");
            }
            
            object[] attrs = type.GetCustomAttributes(typeof(MessageHandlerAttribute), true);
            
            foreach (object attr in attrs)
            {
                MessageHandlerAttribute messageHandlerAttribute = attr as MessageHandlerAttribute;
                if(messageHandlerAttribute == null) continue;

                Type messageType = messageHandler.GetRequestType();

                Type handleResponseType = messageHandler.GetResponseType();
                if (handleResponseType != null)
                {
                    Type responseType = MessageOpcodeManager.Instance.GetResponseTypeBy(messageType);
                    if (handleResponseType != responseType)
                    {
                        throw new Exception($"message handler response type error: {messageType.FullName}");
                    }
                }
                
                MessageDispatcherInfo messageDispatcherInfo = new MessageDispatcherInfo(messageHandlerAttribute.LogicSceneType, messageHandler);
                _messageHandlers.Add(messageType, messageDispatcherInfo);
            }
        }

        public async ETTask HandleMessageAsync(Entity entity, RootAddress fromAddress, MessageObject message)
        {
            if (!_messageHandlers.TryGetValue(message.GetType(), out var messageDispatcherInfoList))
            {
                Log.Error($"not found message handler: <{entity.GetType().FullName},{message.GetType().FullName}>");
                return;
            }

            ELogicSceneType sceneType = entity.LogicScene.SceneType;
            foreach (var messageDispatcherInfo in messageDispatcherInfoList)
            {
                if (!messageDispatcherInfo.SceneType.HasSameFlag(sceneType))
                {
                    continue;
                }

                await messageDispatcherInfo.MessageHandler.Handle(entity, fromAddress, message);
            }
        }
        
        private class MessageDispatcherInfo
        {
            public ELogicSceneType SceneType { get; }
        
            public IMessageHandler MessageHandler { get; }

            public MessageDispatcherInfo(ELogicSceneType sceneType, IMessageHandler messageHandler)
            {
                SceneType = sceneType;
                MessageHandler = messageHandler;
            }
        }
    }
}