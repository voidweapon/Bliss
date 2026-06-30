using System.Collections.Generic;
using Bliss.Core;
using ET.Client;
using UnityEngine;

namespace Bliss
{
    [EntitySystemOf(typeof(UI))]
    public static partial class UISystem
    {
        private static void Awake(this UI self, UIAwakeInfo  uiAwakeInfo)
        {
            self.nameChildren.Clear();
            uiAwakeInfo.GameObject.layer = LayerMask.NameToLayer(LayerNames.UI);
            self.Name = uiAwakeInfo.NodeName;
            self.GameObject = uiAwakeInfo.GameObject;
        }

        private static void Destroy(this UI self)
        {
            foreach (UI ui in self.nameChildren.Values)
            {
                ui.Dispose();
            }

            UnityEngine.Object.Destroy(self.GameObject);
            self.nameChildren.Clear();
        }

        public static void SetAsFirstSibling(this UI self)
        {
            self.GameObject.transform.SetAsFirstSibling();
        }

        public static void Add(this UI self, UI ui)
        {
            self.nameChildren.Add(ui.Name, ui);
        }

        public static void Remove(this UI self, string name)
        {
            UI ui;
            if (!self.nameChildren.TryGetValue(name, out ui))
            {
                return;
            }
            self.nameChildren.Remove(name);
            ui.Dispose();
        }

        public static UI Get(this UI self, string name)
        {
            UI child;
            if (self.nameChildren.TryGetValue(name, out child))
            {
                return child;
            }
            GameObject childGameObject = self.GameObject.transform.Find(name)?.gameObject;
            if (childGameObject == null)
            {
                return null;
            }
            child = self.AddChild<UI, UIAwakeInfo>(new UIAwakeInfo() { NodeName = name, GameObject = childGameObject });
            self.Add(child);
            return child;
        }
    }

    public sealed class UI: Entity, IAwake<UIAwakeInfo>, IDestroy
    {
        public GameObject GameObject { get; set; }
		
        public string Name { get; set; }

        public Dictionary<string, UI> nameChildren = new Dictionary<string, UI>();
    }

    public struct UIAwakeInfo
    {
        public string NodeName;
        public GameObject GameObject;
    }
}