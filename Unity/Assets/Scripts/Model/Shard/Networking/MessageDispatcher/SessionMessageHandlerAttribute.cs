using System;
using Bliss.Core;

namespace Bliss
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SessionMessageHandlerAttribute : BaseAttribute
    {
        public ELogicSceneType SceneType { get; }

        public SessionMessageHandlerAttribute(ELogicSceneType sceneType)
        {
            SceneType = sceneType;
        }
    }
}