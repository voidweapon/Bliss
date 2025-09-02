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

        protected abstract ETTask Run(TScene scene, TEvent eventInfo);

    }
}