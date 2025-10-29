using Bliss.Core;

namespace Bliss
{
    /// <summary>
    /// 标记消息处理器在哪个Scene上工作
    /// </summary>
    public class MessageHandlerAttribute : BaseAttribute
    {
        public ELogicSceneType LogicSceneType { get; private set; }

        public MessageHandlerAttribute(ELogicSceneType logicSceneType)
        {
            LogicSceneType = logicSceneType;
        }
    }
}