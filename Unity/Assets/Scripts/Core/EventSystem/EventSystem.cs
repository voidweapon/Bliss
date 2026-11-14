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
        /// Runs matching event handlers in sequence, requiring at least one and propagating failures.
        /// </summary>
        /// <param name="scene">The scene receiving the event.</param>
        /// <param name="eventData">The event data.</param>
        /// <typeparam name="TScene">The scene type expected by handlers.</typeparam>
        /// <typeparam name="TEvent">The event data type.</typeparam>
        /// <exception cref="InvalidOperationException">No compatible handler is registered.</exception>
        public async ETTask PushRequiredAsync<TScene, TEvent>(TScene scene, TEvent eventData)
            where TScene : class, ILogicScene where TEvent : struct
        {
            if (!this.TryGetStaticEvents(typeof(TEvent), out var events))
            {
                throw new InvalidOperationException($"Required event handler for {typeof(TEvent).FullName} was not found.");
            }

            var handlers = new List<AEvent<TScene, TEvent>>();
            foreach (EventInfo eventInfo in events)
            {
                if (!eventInfo.SceneType.HasFlag(scene.SceneType))
                {
                    continue;
                }

                if (eventInfo.EventHandler is not AEvent<TScene, TEvent> handler)
                {
                    throw new InvalidOperationException(
                        $"Event handler {eventInfo.EventHandler.GetType().FullName} is incompatible with {typeof(TScene).FullName}.");
                }

                handlers.Add(handler);
            }

            if (handlers.Count == 0)
            {
                throw new InvalidOperationException($"Required event handler for {typeof(TEvent).FullName} was not found.");
            }

            foreach (AEvent<TScene, TEvent> handler in handlers)
            {
                await handler.HandleRequired(scene, eventData);
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