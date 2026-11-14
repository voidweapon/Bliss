using System;
using ET;

namespace Bliss
{
    public interface IEvent
    {
        Type EventType { get; }
    }

    public abstract class AEvent<TScene, TEvent> : IEvent
        where TScene : class, ILogicScene where TEvent : struct
    {
        public Type EventType
        {
            get
            {
                return typeof(TEvent);
            }
        }

        public async ETTask Handle(TScene scene, TEvent eventInfo)
        {
            try
            {
                await Run(scene, eventInfo);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// Runs the handler and propagates initialization failures to the caller.
        /// </summary>
        /// <param name="scene">The scene receiving the event.</param>
        /// <param name="eventInfo">The event data.</param>
        public ETTask HandleRequired(TScene scene, TEvent eventInfo)
        {
            return this.Run(scene, eventInfo);
        }

        protected abstract ETTask Run(TScene mainScene, TEvent eventInfo);

    }
}