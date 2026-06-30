using ET;

namespace Bliss
{
    [Event(ELogicSceneType.Client)]
    public class EventHander_LoginSucess_UI : AEvent<LogicScene, Event_LoginSuccess>
    {
        protected override async ETTask Run(LogicScene scene, Event_LoginSuccess eventInfo)
        {
            var uiComponent = scene.GetComponent<UIComponent>();
            
            await uiComponent.Create(UIType.MapSelect, EUILayer.Mid);
            uiComponent.Remove(UIType.Login);
        }
    }
}