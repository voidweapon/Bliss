using Bliss.Core;
using ET;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bliss
{
    [UIEvent(UIType.Loading)]
    public class UILoadingEvent : AUGUI_UIEvent
    {
        public override string GetUIFileName()
        {
            return UIType.Loading;
        }
        public override async ETTask AfterCreate(UIComponent uiComponent, EUILayer uiLayer, UI ui)
        {
            ui.AddComponent<UILoadingComponent>();
            await ETTask.CompletedTask;
        }
    }

    [EntitySystemOf(typeof(UILoadingComponent))]
    public static partial class UILoadingComponentSystems
    {
        private static void Awake(this UILoadingComponent self)
        {
            var ui = self.Parent as UI;
            var referenceCollector = ui.GameObject.GetComponent<ReferenceCollector>();
        }
    }
}