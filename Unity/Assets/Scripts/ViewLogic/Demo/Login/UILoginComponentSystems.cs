using Bliss.Core;
using ET;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bliss
{
    [UIEvent(UIType.Login)]
    public class UILoginEvent : AUGUI_UIEvent
    {
        public override string GetUIFileName()
        {
            return UIType.Login;
        }
        public override async ETTask AfterCreate(UIComponent uiComponent, EUILayer uiLayer, UI ui)
        {
            ui.AddComponent<UILoginComponent>();
            await ETTask.CompletedTask;
        }
    }

    [EntitySystemOf(typeof(UILoginComponent))]
    public static partial class UILoginComponentSystems
    {
        private static void Awake(this UILoginComponent self)
        {
            var ui = self.Parent as UI;
            var referenceCollector = ui.GameObject.GetComponent<ReferenceCollector>();
            self.Input_User = referenceCollector.Get<GameObject>("Input_User").GetComponent<TMP_InputField>();
            self.Input_Pwd = referenceCollector.Get<GameObject>("Input_Pwd").GetComponent<TMP_InputField>();
            self.Btn_Login = referenceCollector.Get<GameObject>("Btn_Login").GetComponent<Button>();
            
            self.Btn_Login.onClick.AddListener(self.onClick_Login);
        }
        private static void Destroy(this UILoginComponent self)
        {
            self.Btn_Login.onClick.RemoveListener(self.onClick_Login);
        }

        private static void onClick_Login(this UILoginComponent self)
        {
            LoginHelper.Login(self.Scene(), self.Input_User.text, self.Input_Pwd.text, null).Coroutine();
        }
    }
}