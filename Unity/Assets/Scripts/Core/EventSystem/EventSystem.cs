using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using Bliss.EventSystems;
using Bliss.Pool;
using ET;

namespace Bliss
{

    [EntitySystemOf(typeof(EventSystem))]
    public static partial class EventSystemLogic
    {
        public static void Update(this EventSystem self)
        {
            self.UpdateInner();
        }
    }

    public class EventSystem : Entity, IUpdate
    {
        private Queue<AEventDataPack> _lateEventQueue = new Queue<AEventDataPack>();
        
        /// <summary>
        /// 推送一个立即触发的事件
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="eventData"></param>
        /// <typeparam name="TScene"></typeparam>
        /// <typeparam name="TEvent"></typeparam>
        public void Push<TScene, TEvent>(TScene scene, TEvent eventData)
            where TScene : class, ILogicScene where TEvent : struct
        {
            if (!EventSystemManager.Instance.TryGetStaticEvents(typeof(TEvent), out var events))
            {
                return;
            }

            ELogicSceneType sceneType = scene.SceneType;
            foreach (var eventInfo in events)
            {
                if (!eventInfo.SceneType.HasFlag(sceneType))
                {
                    continue;
                }
                
                if (!(eventInfo.EventHandler is AEvent<TScene, TEvent> aEvent))
                {
                    Log.Error($"event error: {eventInfo.EventHandler.GetType().FullName}");
                    continue;
                }
                
                aEvent.Handle(scene, eventData).Coroutine();
            }
        }

        /// <summary>
        /// 推送一个下一帧触发的事件
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="eventData"></param>
        /// <typeparam name="TScene"></typeparam>
        /// <typeparam name="TEvent"></typeparam>
        public void PushLate<TScene, TEvent>(TScene scene, TEvent eventData) 
            where TScene : class, ILogicScene where TEvent : struct
        {
            _lateEventQueue.Enqueue(new EventDataPack<TScene, TEvent>()
            {
                Scene = scene,
                EventData = eventData,
                System = this
            });
        }
        
        /// <summary>
        /// 推送一个立即触发的异步事件
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="eventData"></param>
        /// <typeparam name="TScene"></typeparam>
        /// <typeparam name="TEvent"></typeparam>
        public async ETTask PushAsync<TScene, TEvent>(TScene scene, TEvent eventData)
            where TScene : class, ILogicScene where TEvent : struct
        {
            if (!EventSystemManager.Instance.TryGetStaticEvents(typeof(TEvent), out var events))
            {
                return;
            }

            using ListComponent<ETTask> list = ListComponent<ETTask>.Create();
            ELogicSceneType sceneType = scene.SceneType;
            
            foreach (EventSystemManager.EventInfo eventInfo in events)
            {
                if (!eventInfo.SceneType.HasFlag(sceneType))
                {
                    continue;
                }
                    
                if (!(eventInfo.EventHandler is  AEvent<TScene, TEvent> aEvent))
                {
                    Log.Error($"event error: {eventInfo.EventHandler.GetType().FullName}");
                    continue;
                }

                list.Add(aEvent.Handle(scene, eventData));
            }

            try
            {
                await ETTaskHelper.WaitAll(list);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void Invoke<TArg>(TArg arg, long invokeType) where TArg : struct
        {
            if (!EventSystemManager.Instance.TryGetInvoke(typeof(TArg), invokeType, out var invokeHandler))
            {
                throw new Exception($"Not Found Invoke Handler ArgumentType={typeof(TArg).FullName} invokeType={invokeType}");
            }

            if (invokeHandler is not AInvokeHandlerHandler<TArg> handler)
            {
                throw new Exception($"invokeHandler is  not AInvokeHandler: {invokeHandler.GetType()} {typeof(TArg).FullName}");
            }
            
            handler.Handle(arg);
        }

        public TResult Invoke<TArg, TResult>(TArg arg, long invokeType) where TArg : struct
        {
            if (!EventSystemManager.Instance.TryGetInvoke(typeof(TArg), invokeType, out var invokeHandler))
            {
                throw new Exception($"Not Found Invoke Handler ArgumentType={typeof(TArg).FullName} invokeType={invokeType}");
            }

            if (invokeHandler is not AInvokeHandlerHandler<TArg, TResult> handler)
            {
                throw new Exception($"invokeHandler is  not AInvokeHandler: {invokeHandler.GetType()} {typeof(TArg).FullName}");
            }
            
            return  handler.Handle(arg);
        }
        
        internal void UpdateInner()
        {
            int count = _lateEventQueue.Count;
            while (count > 0)
            {
                count--;
                var eventData = _lateEventQueue.Dequeue();
                eventData.Run();
            }
        }

        
        private abstract class AEventDataPack
        {
            public abstract void Run();
        }
        
        private class EventDataPack<TScene, TEvent> : AEventDataPack
            where TScene : class, ILogicScene where TEvent : struct
        {
            public EventSystem System;
            public TScene Scene;
            public TEvent EventData;
            
            public override void Run()
            {
                System.Push<TScene, TEvent>(Scene, EventData);
            }
        }
    }
}