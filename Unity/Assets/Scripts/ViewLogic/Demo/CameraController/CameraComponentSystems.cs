using Bliss.Core;
using Bliss.ViewLogic;
using Bliss.ViewModel;
using ET;
using Unity.Cinemachine;
using UnityEngine;

namespace Bliss
{
    [EntitySystemOf(typeof(CameraComponent))]
    public static partial class CameraComponentSystems
    {
        private static void Awake(this CameraComponent self)
        {
            self.MainCamera = Camera.main;
            
            var viewGlobal = self.RootScene().GetComponent<ViewGlobalComponent>();
            self.SetPlayerVirtualCamera(viewGlobal.PlayerVirtualCamera);
        }
        private static void Destroy(this CameraComponent self)
        {
            self.MainCamera = null;

            if (self.PlayerVirtualCamera != null)
            {
                self.PlayerVirtualCamera.enabled = false;
                self.ClearCinemachineVirtualCamera(self.PlayerVirtualCamera);
            }
            self.PlayerVirtualCamera = null;
        }
        
        private static void LateUpdate(this CameraComponent self)
        {

        }
        
        public static void SetPlayerVirtualCamera(this CameraComponent self, CinemachineVirtualCamera virtualCamera)
        {
            var old = self.PlayerVirtualCamera;
            self.PlayerVirtualCamera = virtualCamera;

            if (old != null)
            {
                self.ClearCinemachineVirtualCamera(old);
            }
            
            if (self.PlayerVirtualCamera != null)
            {
                var cinemachinePov = self.PlayerVirtualCamera.GetCinemachineComponent<CinemachinePOV>();
                if (cinemachinePov != null)
                {
                    cinemachinePov.m_HorizontalAxis.SetInputAxisProvider(0, self);
                    cinemachinePov.m_VerticalAxis.SetInputAxisProvider(1, self);
                }
            }
        }
        
        public static void EnablePlayerVirtualCamera(this CameraComponent self, bool enable)
        {
            self.PlayerVirtualCamera.enabled = enable;
        }
        
        private static void ClearCinemachineVirtualCamera(this CameraComponent self, CinemachineVirtualCamera virtualCamera)
        {
            if (virtualCamera != null)
            {
                var cinemachinePov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
                if (cinemachinePov != null)
                {
                    cinemachinePov.m_HorizontalAxis.SetInputAxisProvider(0, null);
                    cinemachinePov.m_VerticalAxis.SetInputAxisProvider(1, null);
                }
            }
        }
        
        public static void SetTarget(this CameraComponent self, EntityRef<Entity> targetRef)
        {
            if (self.SetupTargetCancellationToken != null)
            {
                self.SetupTargetCancellationToken.Cancel();
            }

            self.SetupTargetCancellationToken = null;
            
            self.TargetRef = targetRef;
            self.SetupTargetCancellationToken = new ETCancellationToken();
            
            self.SetupPlayerVirtualCameraWaitTargetViewLoad(self.TargetRef, self.SetupTargetCancellationToken).Coroutine();
        }

        private static async ETTask SetupPlayerVirtualCameraWaitTargetViewLoad(this CameraComponent self, EntityRef<Entity> targetRef,  ETCancellationToken cancellationToken)
        {
            try
            {
                Entity target = (Entity)targetRef;
                if (target != null)
                {
                    bool loaded = await self.WaitTargetViewLoaded(targetRef, cancellationToken);
                    if (loaded)
                    {
                        var viewCmp = target.GetComponent<GameObjectViewComponent>();
                        var lookAt = viewCmp.FindTransform("CameraBasePoint", "/");
                        self.PlayerVirtualCamera.Follow = lookAt;
                        self.PlayerVirtualCamera.LookAt = lookAt;
                
                        self.EnablePlayerVirtualCamera(true);
                        Log.Info($"EnablePlayerVirtualCamera");
                        return;
                    }
                }
                self.EnablePlayerVirtualCamera(false);
                
            }
            finally
            {
                if (self != null)
                {
                    self.SetupTargetCancellationToken = null;
                }
            }
        }

        private static async ETTask<bool> WaitTargetViewLoaded(this CameraComponent self, EntityRef<Entity> targetRef, ETCancellationToken cancellationToken)
        {
            Entity target = (Entity)targetRef;
            if (target == null)
            {
                return false;
            }
            
            var viewCmp = target.GetComponent<GameObjectViewComponent>();
            if (viewCmp == null || viewCmp.View == null)
            {
                var result = await target.GetComponent<ObjectWait>().Wait<Wait_AfterCreateGameObjectView>(cancellationToken);
                if (result.Error != WaitTypeError.Success)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}