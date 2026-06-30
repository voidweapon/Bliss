using System.Collections.Generic;
using Bliss.Core;
using ET;

namespace Bliss
{
    /// <summary>
    /// 管理Scene上的UI
    /// </summary>
    [EntitySystemOf(typeof(UIComponent))]
    public static partial class UIComponentSystem
    {
        private static void Awake(this UIComponent self)
        {
            self.UIGlobalComponent = self.RootScene().GetComponent<UIGlobalComponent>();
        }
        
        private static void Destroy(this UIComponent self)
        {
            foreach (var kv in self.UIs)
            {
                var uiType = kv.Key;   
                self.UIGlobalComponent.OnRemove(self, uiType);
            }
            self.UIs.Clear();
        }

        public static async ETTask<UI> Create(this UIComponent self, string uiType, EUILayer uiLayer)
        {
            UI ui = await self.UIGlobalComponent.OnCreate(self, uiType, uiLayer);
            self.UIs.Add(uiType, ui);
            return ui;
        }

        public static void Remove(this UIComponent self, string uiType)
        {
            if (!self.UIs.TryGetValue(uiType, out UI ui))
            {
                return;
            }

            self.UIGlobalComponent.OnRemove(self, uiType);

            self.UIs.Remove(uiType);
            ui.Dispose();
        }

        public static UI Get(this UIComponent self, string name)
        {
            self.UIs.TryGetValue(name, out var ui);
            return ui;
        }
    }
}