using System;
using System.Collections.Generic;
using Bliss.Core;

namespace Bliss
{
    [SingletonManger]
    public class SessionMessageDispatcher : Singleton<SessionMessageDispatcher>, ISingletonInit
    {
        private Dictionary<Type, List<MessageSessionDispatcherInfo>> _handlers =  new Dictionary<Type, List<MessageSessionDispatcherInfo>>();
        public void Init()
        {
            var handlerTypes = TypeManager.Instance.GetTypes(typeof(SessionMessageHandlerAttribute));
            foreach (var handlerType in handlerTypes)
            {
                var handler = Activator.CreateInstance(handlerType) as ISessionMessageHandler;
                if (handler == null)
                {
                    Log.Error($"message handle {handlerType.Name} 需要继承 {nameof(ISessionMessageHandler)}");
                    continue;
                }
                
                object[] attrs = handlerType.GetCustomAttributes(typeof(SessionMessageHandlerAttribute), true);
                
                foreach (object attr in attrs)
                {
                    var messageSessionHandlerAttribute = attr as SessionMessageHandlerAttribute;
                    
                    Type messageType = handler.GetMessageType();
                    
                    if (!MessageOpcodeManager.Instance.TryGetOpcodeTypeBy(messageType, out ushort opcode))
                    {
                        Log.Error($"无法找到消息[{messageType.Name}]的opcode");
                        continue;
                    }

                    if (messageSessionHandlerAttribute != null)
                    {
                        MessageSessionDispatcherInfo messageSessionDispatcherInfo = new (messageSessionHandlerAttribute.SceneType, handler);
                        RegisterHandler(messageType, messageSessionDispatcherInfo);
                    }
                }
            }
        }
        
        private void RegisterHandler(Type messageType, MessageSessionDispatcherInfo handler)
        {
            if (!_handlers.TryGetValue(messageType, out var list))
            {
                list = new List<MessageSessionDispatcherInfo>();
                _handlers.Add(messageType, list);
            }

            list.Add(handler);
        }
        
        
        public void Handle(Session session, object message)
        {
            if (!this._handlers.TryGetValue(message.GetType(), out var actions))
            {
                Log.Error($"消息没有处理: {message.GetType()} {message}");
                return;
            }

            ELogicSceneType sceneType = session.LogicScene.SceneType;
            foreach (MessageSessionDispatcherInfo ev in actions)
            {
                if (!ev.SceneType.HasSameFlag(sceneType))
                {
                    continue;
                }
                
                try
                {
                    ev.MessageHandler.Handle(session, message);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        private struct MessageSessionDispatcherInfo
        {
            public ELogicSceneType SceneType { get; }
            public ISessionMessageHandler MessageHandler { get; }

            public MessageSessionDispatcherInfo(ELogicSceneType sceneType, ISessionMessageHandler messageHandler)
            {
                this.SceneType = sceneType;
                this.MessageHandler = messageHandler;
            }
        }
    }
}