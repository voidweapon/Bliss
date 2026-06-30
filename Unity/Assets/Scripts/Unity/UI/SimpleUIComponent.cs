using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bliss.SimpleUI
{
    public class SimpleUIComponent : Singleton<SimpleUIComponent>, ISingletonInit
    {
        public Dictionary<int, Transform> UILayers = new Dictionary<int, Transform>();
        
        public const string BootResPath_UI = "Assets/Res/BootRes/UI";
        private Dictionary<string, ASimpleUI> DicUITyoe2UI = new Dictionary<string, ASimpleUI>();
        
        public void Init()
        {
            DicUITyoe2UI.Clear();   
            
            
            GameObject uiRoot = GameObject.Find("/Global/UGUI Canvas");
            ReferenceCollector referenceCollector = uiRoot.GetComponent<ReferenceCollector>();

            
            this.UILayers.Add((int)EUILayer.Hidden, referenceCollector.Get<GameObject>(EUILayer.Hidden.ToString()).transform);
            this.UILayers.Add((int)EUILayer.Low, referenceCollector.Get<GameObject>(EUILayer.Low.ToString()).transform);
            this.UILayers.Add((int)EUILayer.Mid, referenceCollector.Get<GameObject>(EUILayer.Mid.ToString()).transform);
            this.UILayers.Add((int)EUILayer.High, referenceCollector.Get<GameObject>(EUILayer.High.ToString()).transform);
        }

        protected override void Destroy()
        {
            base.Destroy();
            
            var uis = DicUITyoe2UI.Values;
            foreach (var ui in uis)
            {
                ui.Dispose();
            }
            DicUITyoe2UI.Clear();
            
            UILayers.Clear();
        }
        
        public void Create<T>(string uiType, EUILayer uiLayer) where T : ASimpleUI, new()
        {
            string path = $"{BootResPath_UI}/{uiType}.prefab";
            var uiPrefab = SimpleResourcesComponent.Instance.LoadAsset<GameObject>("BootPackage", path);
            
            var layerNode = this.UILayers[(int)uiLayer];
            var uiObject = GameObject.Instantiate(uiPrefab, layerNode);
            
            var simpleUI = new T();
            simpleUI.Create(uiObject);
            DicUITyoe2UI[uiType] = simpleUI;
        }

        public void Remove(string uiType)
        {
            if (DicUITyoe2UI.Remove(uiType, out var ui))
            {
                ui.Dispose();
            }
        }
        
        public void SetLayer(string uiType, EUILayer uiLayer)
        {
            if(DicUITyoe2UI.TryGetValue(uiType, out var simpleUI))
            {
                var layerNode = this.UILayers[(int)uiLayer];
                simpleUI.UIObject.transform.SetParent(layerNode, false);
            }
        }
    }

    public abstract class ASimpleUI : IDisposable
    {
        public GameObject UIObject { get; private set; }

        public void Dispose()
        {
            OnDestroy();
            
            if (UIObject != null)
            {
                GameObject.Destroy(UIObject);
            }
            UIObject = null;
        }

        internal void Create(GameObject uiObject)
        {
            UIObject =  uiObject;
            OnCreate();
        }

        protected virtual void OnCreate()
        {
            
        }

        protected virtual void OnDestroy()
        {
            
        }
    }
}