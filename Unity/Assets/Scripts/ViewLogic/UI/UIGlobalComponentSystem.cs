using System;
using Bliss.Core;
using ET;
using UnityEngine;

namespace Bliss
{
    [EntitySystemOf(typeof(UIGlobalComponent))]
    public static partial class UIGlobalComponentSystem
    {
        private static void Awake(this UIGlobalComponent self)
        {
            GameObject uiRoot = GameObject.Find("/Global/UGUI Canvas");
            ReferenceCollector referenceCollector = uiRoot.GetComponent<ReferenceCollector>();

            self.UILayers.Add((int)EUILayer.Hidden, referenceCollector.Get<GameObject>(EUILayer.Hidden.ToString()).transform);
            self.UILayers.Add((int)EUILayer.Low, referenceCollector.Get<GameObject>(EUILayer.Low.ToString()).transform);
            self.UILayers.Add((int)EUILayer.Mid, referenceCollector.Get<GameObject>(EUILayer.Mid.ToString()).transform);
            self.UILayers.Add((int)EUILayer.High, referenceCollector.Get<GameObject>(EUILayer.High.ToString()).transform);
        }

        public static async ETTask<UI> OnCreate(this UIGlobalComponent self, UIComponent uiComponent, string uiType, EUILayer uiLayer)
        {
            try
            {
                UI ui = await UIEventComponent.Instance.UIEvents[uiType].OnCreate(uiComponent, uiLayer);
                return ui;
            }
            catch (Exception e)
            {
                throw new Exception($"on create ui error: {uiType}", e);
            }
        }

        public static Transform GetLayer(this UIGlobalComponent self, int layer)
        {
            return self.UILayers[layer];
        }

        public static void OnRemove(this UIGlobalComponent self, UIComponent uiComponent, string uiType)
        {
            try
            {
                UIEventComponent.Instance.UIEvents[uiType].OnRemove(uiComponent);
            }
            catch (Exception e)
            {
                throw new Exception($"on remove ui error: {uiType}", e);
            }
        }
    }
}