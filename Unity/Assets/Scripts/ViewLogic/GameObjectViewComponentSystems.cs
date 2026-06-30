using System;
using Bliss.Core;
using Bliss.ViewModel;
using UnityEngine;
using Unity.Mathematics;
using Drawing;
using ET;
using UnityEditor;

namespace Bliss.ViewLogic
{
    [EntitySystemOf(typeof(GameObjectViewComponent))]
    public static partial class GameObjectViewComponentSystems
    {
        private static void Awake(this GameObjectViewComponent self, GameObjectViewComponentInitParam param1)
        {
            self.TransformComponent = self.Parent.GetComponent<TransformComponent>();
            
            self.View = param1.View;
            self.ViewAssetPath = param1.ViewAssetPath;
            self.IsViewModelFromPool = param1.IsFromPool;
            self.ViewModelLoadCancellationToken = new ETCancellationToken();
            ViewEntityMapManager.Instance?.Map(self.Parent, self.View);
            
#if UNITY_EDITOR
            self.DrawTargets = new[] { Camera.main };
#endif
        }

        private static void Destroy(this GameObjectViewComponent self)
        {
            ViewEntityMapManager.Instance?.UnMap(self.Parent);
            self.TransformComponent =  null;
            
            if (self.View != null)
            {
                GameObject.Destroy(self.View);
            }
            self.View = null;

            if (self.ViewMode != null)
            {
                if (self.IsViewModelFromPool)
                {
                    //TODO: GameObjectPool
                }
                else
                {
                    GameObject.Destroy(self.ViewMode);
                }
            }

            self.ViewMode = null;

            self.ViewAssetPath = null;
            self.IsViewModelFromPool = false;

            self.DicPathTpTransform.Clear();

            self.ViewModelLoadCancellationToken?.Cancel();
            self.ViewModelLoadCancellationToken = null;

#if UNITY_EDITOR
            self.DrawTargets = null;
            self.DrawTargets2 = null;
#endif
        }
        
        
        private static void LateUpdate(this GameObjectViewComponent self)
        {
            if (self.ViewMode != null)
            {
                self.ViewMode.transform.position = self.View.transform.position;
                self.ViewMode.transform.rotation = self.View.transform.rotation;
                self.ViewMode.transform.localScale = self.View.transform.localScale;
            }
        }

        public static Transform FindTransform(this GameObjectViewComponent self, string path, string sep)
        {
            if (self.ViewMode == null) return null;
            if (string.IsNullOrEmpty(path)) return self.ViewMode.transform;

            if (!self.DicPathTpTransform.TryGetValue(path, out var transform))
            {
                transform = self.ViewMode.transform.FindTransform(path, sep);
                self.DicPathTpTransform.Add(path, transform);
            }
            return transform;
        }

        public static async ETTask LoadViewmodel(this GameObjectViewComponent self, string path)
        {
            self.ViewAssetPath = path;

            LogicScene levelScene = self.Scene();
            long instanceId = self.InstanceId;

            var resourcesLoader = levelScene.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                return;
            }

            var prefab = await resourcesLoader.LoadAssetAsync<GameObject>(path);
            if (prefab == null)
            {
                return;
            }

            if (instanceId != self.InstanceId)
            {
                return;
            }

            if (self.ViewModelLoadCancellationToken.IsDispose())
            {
                return;
            }

            if (self.View == null)
            {
                return;
            }

            var viewModelGo = UnityEngine.Object.Instantiate(prefab, self.View.transform.parent);
            viewModelGo.transform.localPosition = float3.zero;
            viewModelGo.transform.localRotation = quaternion.identity;
            self.ViewMode = viewModelGo;
        }
        

#if UNITY_EDITOR
        private static void DrawDebugInfo(this GameObjectViewComponent self)
        {
            if (self.IsDisposed || self.DrawTargets == null) return;

            var netRoleCmp = self.Parent.GetComponent<NetRoleComponent>();
            if (netRoleCmp != null && self.View != null)
            {
                var netRole = netRoleCmp.LocalNetRole;
                Color roleColor = Color.green;
                switch (netRole)
                {
                    case ENetRole.None:
                        break;
                    case ENetRole.SimulatedProxy:
                        roleColor = Color.blue;
                        break;
                    case ENetRole.AutonomousProxy:
                        roleColor = Color.green;
                        break;
                    case ENetRole.Authority:
                        roleColor = Color.red;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var draw = DrawingManager.GetBuilder(true);
                draw.cameraTargets = self.DrawTargets;

                if (self.DrawTargets2 == null || self.DrawTargets2.Length < SceneView.sceneViews.Count + 1)
                {
                    self.DrawTargets2 = new Camera[SceneView.sceneViews.Count + 1];
                }
                self.DrawTargets2[0] = Camera.main;
                for (int i = 0; i < SceneView.sceneViews.Count; i++)
                {
                    self.DrawTargets2[i + 1] = ((SceneView)SceneView.sceneViews[i]).camera;
                }
                draw.cameraTargets = self.DrawTargets2;

                using (draw)
                using (draw.WithMatrix(Matrix4x4.TRS(self.View.transform.position, self.View.transform.rotation, self.View.transform.lossyScale)))
                using (draw.WithColor(roleColor))
                {
                    draw.WireCapsule(float3.zero, new float3(0, 1, 0), 2, 0.5f);
                    draw.ArrowheadArc(float3.zero, Vector3.forward, 0.5f);
                }
            }
        }
#endif

    }
}