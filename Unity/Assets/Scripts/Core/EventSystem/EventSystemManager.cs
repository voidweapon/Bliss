using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using Bliss.Core.EventSystem;

namespace Bliss
{
    [SingletonManger]
    public partial class EventSystem : ISingletonInit
    {
        private MultiMapList<Type, EventInfo> _allStaticEvents = new MultiMapList<Type, EventInfo>();
        
        /// <summary>
        /// (Argument Type, InvokeType) -> Invoke Handle Obj
        /// </summary>
        private Dictionary<(Type, long), IInvokeHandler> _allStaticInvokers = new Dictionary<(Type, long), IInvokeHandler>();
        
        public void Init()
        {
            foreach (var type in TypeManager.Instance.GetTypes(typeof(EventAttribute)))
            {
                IEvent obj = Activator.CreateInstance(type) as IEvent;
                if (obj == null)
                {
                    throw new Exception($"type not is AEvent: {type.Name}");
                }
                
                object[] attrs = type.GetCustomAttributes(typeof(EventAttribute), false);
                foreach (object attr in attrs)
                {
                    EventAttribute eventAttribute = attr as EventAttribute;

                    Type eventType = obj.EventType;

                    EventInfo eventInfo = new EventInfo(obj, eventAttribute.SceneType);

                    _allStaticEvents.Add(eventType, eventInfo);
                }
            }
            
            foreach (var type in TypeManager.Instance.GetTypes(typeof(InvokeAttribute)))
            {
                IInvokeHandler obj = Activator.CreateInstance(type) as IInvokeHandler;
                if (obj == null)
                {
                    throw new Exception($"type not is IInvokeHandler: {type.Name}");
                }
                
                object[] attrs = type.GetCustomAttributes(typeof(InvokeAttribute), false);
                foreach (object attr in attrs)
                {
                    InvokeAttribute invokeAttribute = attr as InvokeAttribute;
                    try
                    {
                        _allStaticInvokers.Add((obj.ArgumentType, invokeAttribute.InvokeHandleType), obj);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"invoke duplicate invoke handle type {invokeAttribute.InvokeHandleType}: {e}");
                    }
                }
            }
        }

        internal bool TryGetStaticEvents(Type eventType, out List<EventInfo> events)
        { 
            return _allStaticEvents.TryGetValue(eventType, out  events);
        }

        internal bool TryGetInvoke(Type argType, long invokeType, out IInvokeHandler invokeHandler)
        {
            return _allStaticInvokers.TryGetValue((argType, invokeType), out invokeHandler);
        }
        
        internal class EventInfo
        {
            public ELogicSceneType SceneType { get; }
            public IEvent EventHandler { get; }

            public EventInfo( IEvent handler, ELogicSceneType sceneType)
            {
                SceneType = sceneType;
                EventHandler = handler;
            }
        }
    }
}