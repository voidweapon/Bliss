using Bliss.Core;
using Bliss.ViewModel;
using Unity.Cinemachine;
using UnityEngine;

namespace Bliss.ViewLogic
{
    [EntitySystemOf(typeof(ViewGlobalComponent))]
    public static partial class ViewGlobalComponentSystems
    {
        private static void Awake(this ViewGlobalComponent self)
        {
            self.GlobalTransform = GameObject.Find("/Global").transform;
            self.EntitiesTransform = GameObject.Find("/Global/Entities").transform;
            
            
            self.UGUI_Canvas = GameObject.Find("/Global/UGUI Canvas").transform;
            self.UGUI_Canvas_Low = self.UGUI_Canvas.Find("Low").transform;
            self.UGUI_Canvas_Mid = self.UGUI_Canvas.Find("Mid").transform;
            self.UGUI_Canvas_High = self.UGUI_Canvas.Find("High").transform;
            self.UGUI_Canvas_Debug = self.UGUI_Canvas.Find("Debug").transform;
            
            self.PlayerVirtualCamera = GameObject.Find("/Global/PlayerVM").GetComponent<CinemachineVirtualCamera>();
            self.PlayerVirtualCamera.enabled = false;
            
            GameObject.DontDestroyOnLoad(self.GlobalTransform);
        }
    }
}