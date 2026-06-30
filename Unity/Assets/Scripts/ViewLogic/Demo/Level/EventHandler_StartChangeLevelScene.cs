using ET;

namespace Bliss
{
    /// <summary>
    /// 场景切换流程1
    /// 开始切场景前开启LoadingUI
    /// <seealso cref="MapHelper.ChangeMap"/>
    /// </summary>
    [Event(ELogicSceneType.Client)]
    public class EventHandler_StartChangeLevelScene : AEvent<LogicScene, Event_StartChangeLevelScene>
    {
        protected override async ETTask Run(LogicScene scene, Event_StartChangeLevelScene eventInfo)
        {
            var uiComponent = scene.GetComponent<UIComponent>();
            await uiComponent.Create(UIType.Loading, EUILayer.High);
        }
    }
}