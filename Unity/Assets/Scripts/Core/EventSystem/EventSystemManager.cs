using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;

namespace Bliss.EventSystems
{
    [SingletonManger]
    public class EventSystemManager : Singleton<EventSystemManager>, ISingletonInit
    {
        private MultiMapList<Type, EventInfo> _allStaticEvents = new MultiMapList<Type, EventInfo>();
        
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
        }

        internal bool TryGetStaticEvents(Type eventType, out List<EventInfo> events)
        { 
            return _allStaticEvents.TryGetValue(eventType, out  events);
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