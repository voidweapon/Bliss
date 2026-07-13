using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Core;
using Bliss.Pool;
using Bliss.Timer;
using ET;

namespace Bliss
{
    public partial class EventSystem : Singleton<EventSystem>
    {
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
            if (!TryGetStaticEvents(typeof(TEvent), out var events))
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
        /// 推送一个立即触发的异步事件
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="eventData"></param>
        /// <typeparam name="TScene"></typeparam>
        /// <typeparam name="TEvent"></typeparam>
        public async ETTask PushAsync<TScene, TEvent>(TScene scene, TEvent eventData)
            where TScene : class, ILogicScene where TEvent : struct
        {
            if (!TryGetStaticEvents(typeof(TEvent), out var events))
            {
                return;
            }

            using ListComponent<ETTask> list = ListComponent<ETTask>.Create();
            ELogicSceneType sceneType = scene.SceneType;
            
            foreach (EventInfo eventInfo in events)
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
            InnerPushLateAsync(scene, eventData).Coroutine();
        }

        private async ETTask InnerPushLateAsync<TScene, TEvent>(TScene scene, TEvent eventData)
            where TScene : class, ILogicScene where TEvent : struct
        {
            await scene.Root.Scene.GetComponent<TimerComponent>().WaitNextFrameAsync();
            Push(scene, eventData);
        }
        
        public async ETTask PushLateAsync<TScene, TEvent>(TScene scene, TEvent eventData) 
            where TScene : class, ILogicScene where TEvent : struct
        {
            await scene.Root.Scene.GetComponent<TimerComponent>().WaitNextFrameAsync();
            await PushAsync(scene, eventData);
        }

        public void Invoke<TArg>(long invokeType, TArg arg) where TArg : struct
        {
            if (!TryGetInvoke(typeof(TArg), invokeType, out var invokeHandler))
            {
                throw new Exception($"Not Found Invoke Handler ArgumentType={typeof(TArg).FullName} invokeType={invokeType}");
            }

            if (invokeHandler is not AInvokeHandler<TArg> handler)
            {
                throw new Exception($"invokeHandler is  not AInvokeHandler: {invokeHandler.GetType()} {typeof(TArg).FullName}");
            }
            
            handler.Handle(arg);
        }

        public TResult Invoke<TArg, TResult>(long invokeType, TArg arg) where TArg : struct
        {
            if (!TryGetInvoke(typeof(TArg), invokeType, out var invokeHandler))
            {
                throw new Exception($"Not Found Invoke Handler invokeType={invokeType}  ArgumentType={typeof(TArg).FullName} Assembly.Location={typeof(TArg).Assembly.Location} HashCode={typeof(TArg).GetHashCode()} ");
            }

            if (invokeHandler is not AInvokeHandler<TArg, TResult> handler)
            {
                throw new Exception($"invokeHandler is  not AInvokeHandler: {invokeHandler.GetType()} {typeof(TArg).FullName}");
            }
            
            return  handler.Handle(arg);
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