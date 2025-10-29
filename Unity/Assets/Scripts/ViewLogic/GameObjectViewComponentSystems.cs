using System;
using Bliss.Core;
using Bliss.ViewModel;
using UnityEngine;
using Unity.Mathematics;
using Drawing;
using UnityEditor;

namespace Bliss.ViewLogic
{
    [EntitySystemOf(typeof(GameObjectViewComponent))]
    public static partial class GameObjectViewComponentSystems
    {
        public static void Awake(this GameObjectViewComponent self, GameObjectViewComponentInitParam param1)
        {
            self.View = param1.View;
            self.IsViewFromPool = param1.IsFromPool;
            self.DrawTargets = new[] { Camera.main };
        }

        public static void Destroy(this GameObjectViewComponent self)
        {
            if (self.View != null)
            {
                GameObject.Destroy(self.View);
            }
            self.View = null;
            self.IsViewFromPool = false;
            self.DrawTargets = null;
            self.DrawTargets2 = null;
        }
        public static void Update(this GameObjectViewComponent self)
        {
            if(self.IsDisposed || self.DrawTargets == null) return;
            
            var netRoleCmp = self.Parent.GetComponent<NetRoleComponent>();
            if (netRoleCmp != null && self.View != null)
            {
                var netRole = netRoleCmp.LocalNetRole;
                Color roleColor  = Color.green;
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
                
#if UNITY_EDITOR
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
#endif
                
                using(draw)
                using (draw.WithMatrix(Matrix4x4.TRS(self.View.transform.position, self.View.transform.rotation, self.View.transform.lossyScale)))
                using (draw.WithColor(roleColor))
                {
                    draw.WireCapsule(float3.zero, new float3(0, 1, 0), 2, 0.5f);
                    draw.ArrowheadArc(float3.zero, Vector3.forward, 0.5f);
                }
            }
        }
    }
}