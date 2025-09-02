using Bliss.Core;

namespace Bliss
{
    /// <summary>
    /// 标记类作为事件处理器，由<see cref="EntitySystem"/>自动创建
    /// </summary>
    public class EventAttribute : BaseAttribute
    {
        public ELogicSceneType SceneType { get; private set; }

        public EventAttribute(ELogicSceneType sceneType)
        {
            SceneType = sceneType;
        }
    }
}