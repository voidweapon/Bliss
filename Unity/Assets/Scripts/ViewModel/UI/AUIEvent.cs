using ET;

namespace Bliss
{
    public abstract class AUIEvent
    {
        public abstract ETTask<UI> OnCreate(UIComponent uiComponent, EUILayer uiLayer);
        public abstract void OnRemove(UIComponent uiComponent);
    }
}
