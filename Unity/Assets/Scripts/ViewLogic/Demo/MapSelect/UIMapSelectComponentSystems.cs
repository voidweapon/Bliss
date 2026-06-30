using Bliss.Core;
using ET;
using UnityEngine;
using UnityEngine.UI;

namespace Bliss
{
    [UIEvent(UIType.MapSelect)]
    public class UIMapSelectEvent : AUGUI_UIEvent
    {
        public override string GetUIFileName()
        {
            return UIType.MapSelect;
        }
        public override async ETTask AfterCreate(UIComponent uiComponent, EUILayer uiLayer, UI ui)
        {
            ui.AddComponent<UIMapSelectComponent>();
            await ETTask.CompletedTask;
        }
    }


    [EntitySystemOf(typeof(UIMapSelectComponent))]
    public static partial class UIMapSelectComponentSystems
    {
        private static void Awake(this UIMapSelectComponent self)
        {
            var ui = self.Parent as UI;
            var referenceCollector = ui.GameObject.GetComponent<ReferenceCollector>();
            self.Btn_Enter = referenceCollector.Get<GameObject>("Btn_Enter").GetComponent<Button>();
            
            self.Btn_Enter.onClick.AddListener(self.onClick_Enter);
        }
        private static void Destroy(this UIMapSelectComponent self)
        {
            self.Btn_Enter.onClick.RemoveListener(self.onClick_Enter);
        }
        
        private static void onClick_Enter(this UIMapSelectComponent self)
        {
            self.EnterMap().Coroutine();
        }

        private static async ETTask EnterMap(this UIMapSelectComponent self)
        {
            var scene = self.Scene();
            await MapHelper.EnterMap(scene, 5, null);
            scene.GetComponent<UIComponent>().Remove(UIType.MapSelect);
            
        }
    }
}