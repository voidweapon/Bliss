using Bliss.Core;
using Bliss.ViewModel;
using Bliss.ViewModel.Debugger;
using ET;
using UnityEngine;
using UnityEngine.UI;

namespace Bliss.ViewLogic.Debugger
{
    [EntitySystemOf(typeof(TransformStatDebugComponent))]
    public static partial class TransformStatDebugComponentSystems
    {
        private static void Awake(this TransformStatDebugComponent self)
        {
            self.Time = TimeInfo.Instance.ClientNow();
            self.WatchTransforms.Clear();
            self.InitRes().Coroutine();
        }

        private static void Destroy(this TransformStatDebugComponent self)
        {
            self.WatchTransforms.Clear();
        }

        public static void RegisterWatchTransform(this TransformStatDebugComponent self, 
            int categoryIndex, Transform transform)
        {
            self.WatchTransforms[categoryIndex] = transform;
            self.TransformsTrace[categoryIndex] = new TransformStatDebugComponent.TransformTraceData();
            self.TransformsTrace[categoryIndex].Init(transform);
        }

        public static void RegisterWatchCmp(this TransformStatDebugComponent self,
            int categoryIndex, PeriodicMotionComponent cmpLhs, PeriodicMotionComponent cmpRhs)
        {
            self.WatchCmp[categoryIndex] = (cmpLhs, cmpRhs);
        }
        
        
        private static void Update(this TransformStatDebugComponent self)
        {
            if (self.UI == null)
            {
                return;
            }
            
            long now = TimeInfo.Instance.ClientNow();
            float dt = (now - self.Time) / 1000.0f;
            self.Time = now;
            
            foreach (var pair in self.WatchTransforms)
            {
                int categoryIndex = pair.Key;
                self.TransformsTrace[categoryIndex].Update(pair.Value, dt);
                self.TransformGraphic.SetValue(categoryIndex, self.TransformsTrace[categoryIndex].Velocity.magnitude);
                self.RotationGraphic.SetValue(categoryIndex, self.TransformsTrace[categoryIndex].AngularVelocity);
            }

            foreach (var pair in self.WatchCmp)
            {
                int categoryIndex = pair.Key;
                var cmp = pair.Value;
                var diff = cmp.Item1.ServerLastTransformUpdateTimeStamp - cmp.Item2.ServerLastTransformUpdateTimeStamp;
                self.TimeLineGraphic.SetValue(categoryIndex, diff);
            }
        }

        public static void Toggle(this TransformStatDebugComponent self)
        {
            if (self.UI == null)
            {
                return;
            }
            
            self.UI.gameObject.SetActive(!self.UI.gameObject.activeSelf);
        }

        private static async ETTask InitRes(this TransformStatDebugComponent self)
        {
            long instanceId = self.InstanceId;
            var mainScene = self.Scene<LogicScene>();
            var viewGlobal =  mainScene.GetComponent<ViewGlobalComponent>();
            var prefab =  await mainScene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>("Assets/Res/Tools/Prefab/DataVaraince Stat.prefab");

            if (instanceId != self.InstanceId)
            {
                return;
            }
            
            self.UI = GameObject.Instantiate(prefab, viewGlobal.UGUI_Canvas_Debug);
            self.TransformGraphic = self.UI.transform.Find("TransformChart").GetComponent<Varaince_DataUIGraphic>();
            self.RotationGraphic = self.UI.transform.Find("RotationChart").GetComponent<Varaince_DataUIGraphic>();
            self.TimeLineGraphic = self.UI.transform.Find("TimeLineChart").GetComponent<Varaince_DataUIGraphic>();
            self.UI.gameObject.SetActive(false); 
        }
    }
}