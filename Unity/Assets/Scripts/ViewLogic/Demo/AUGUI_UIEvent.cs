using Bliss.ViewLogic;
using Bliss.ViewModel;
using ET;
using UnityEngine;
using YooAsset;

namespace Bliss
{
    public  abstract class AUGUI_UIEvent : AUIEvent
    {
        public const string ResPath_UI = "Assets/Res/UI";
        public override async ETTask<UI> OnCreate(UIComponent uiComponent, EUILayer uiLayer)
        {
            string uiResPath = $"{ResPath_UI}/{GetUIFileName()}.prefab";
            
            var uiPrefab = await uiComponent.Scene().GetComponent<ResourcesLoaderComponent>() .LoadAssetAsync<GameObject>(uiResPath);
            var uiGo =  GameObject.Instantiate(uiPrefab, uiComponent.UIGlobalComponent.GetLayer((int)uiLayer));
            
            UI ui = uiComponent.AddChild<UI, UIAwakeInfo>(new UIAwakeInfo() { NodeName = GetUIFileName(), GameObject = uiGo });
            
            await AfterCreate(uiComponent, uiLayer, ui);
            
            return ui;
        }

        public override void OnRemove(UIComponent uiComponent)
        {
        }

        public abstract string GetUIFileName();

        public abstract ETTask AfterCreate(UIComponent uiComponent, EUILayer uiLayer, UI ui);
    }
}