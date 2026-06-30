using Bliss.Core.EventSystem;
using UnityEngine;

namespace Bliss.ViewLogic.Physics
{
    #region BoxCollider
    [Invoke(PhysicsInvokeType.BoxColliderAwake)]
    public class EventHanlder_GameplayBoxColliderAwake : AInvokeHandler<GameplayBoxColliderAwakeParams>
    {
        public override void Handle(GameplayBoxColliderAwakeParams param)
        {
            var collider = param.Collider;
            var goBoxCollider = collider.GetOrCreateUnityCollider<BoxCollider>();
            ViewEntityMapManager.Instance.MapCollider(collider, goBoxCollider);

            goBoxCollider.center = param.InitParams.Center;
            goBoxCollider.size = param.InitParams.Size;
        }
    }
    
    [Invoke(PhysicsInvokeType.BoxColliderDestroy)]
    public class EventHanlder_GameplayBoxColliderDestroy : AInvokeHandler<GameplayBoxColliderDestroyParams>
    {
        public override void Handle(GameplayBoxColliderDestroyParams param)
        {
            var collider = param.Collider;
            ViewEntityMapManager.Instance.UnMapCollider(collider);
            var goBoxCollider = collider.GetUnityCollider<BoxCollider>();

            if(goBoxCollider == null)
            {
                return;
            }

            Object.Destroy(goBoxCollider);
        }
    }
    
    [Invoke(PhysicsInvokeType.BoxColliderSetCenter)]
    public class EventHanlder_GameplayBoxColliderSetCenter : AInvokeHandler<SetBoxColliderCenterParams>
    {
        public override void Handle(SetBoxColliderCenterParams param)
        {
            var collider = param.Collider;
            var goBoxCollider = collider.GetUnityCollider<BoxCollider>();

            if(goBoxCollider == null)
            {
                return;
            }

            goBoxCollider.center = param.Center;
        }
    }
    
    [Invoke(PhysicsInvokeType.BoxColliderSetSize)]
    public class EventHanlder_GameplayBoxColliderSetSize : AInvokeHandler<SetBoxColliderSizeParams>
    {
        public override void Handle(SetBoxColliderSizeParams param)
        {
            var collider = param.Collider;
            var goBoxCollider = collider.GetUnityCollider<BoxCollider>();

            if(goBoxCollider == null)
            {
                return;
            }

            goBoxCollider.size = param.Size;
        }
    }
    #endregion

    #region CapsuleCollider
    [Invoke(PhysicsInvokeType.CapsuleColliderAwake)]
    public class EventHanlder_GameplayCapsuleColliderAwake : AInvokeHandler<GameplayCapsuleColliderAwakeParams>
    {
        public override void Handle(GameplayCapsuleColliderAwakeParams param)
        {
            var collider = param.Collider;
            var goCapsuleCollider = collider.GetOrCreateUnityCollider<CapsuleCollider>();;
            ViewEntityMapManager.Instance.MapCollider(collider, goCapsuleCollider);

            goCapsuleCollider.center = param.InitParams.Center;
            goCapsuleCollider.radius = param.InitParams.Radius;
            goCapsuleCollider.height = param.InitParams.Height;
            goCapsuleCollider.direction = param.InitParams.Direction;
            
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderDestroy)]
    public class EventHanlder_GameplayCapsuleColliderDestroy : AInvokeHandler<GameplayCapsuleColliderDestroyParams>
    {
        public override void Handle(GameplayCapsuleColliderDestroyParams param)
        {
            var collider = param.Collider;
            ViewEntityMapManager.Instance.UnMapCollider(collider);
            
            var goCapsuleCollider = collider.GetUnityCollider<CapsuleCollider>();

            if(goCapsuleCollider == null)
            {
                return;
            }

            Object.Destroy(goCapsuleCollider);
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetCenter)]
    public class EventHanlder_GameplayCapsuleColliderSetCenter: AInvokeHandler<SetCapsuleColliderCenterParams>
    {
        public override void Handle(SetCapsuleColliderCenterParams param)
        {
            var collider = param.Collider;
            var goCapsuleCollider = collider.GetUnityCollider<CapsuleCollider>();;
            if(goCapsuleCollider == null)
            {
                return;
            }
            goCapsuleCollider.center = param.Center;
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetRadius)]
    public class EventHanlder_GameplayCapsuleColliderSetRadius: AInvokeHandler<SetCapsuleColliderRadiusParams>
    {
        public override void Handle(SetCapsuleColliderRadiusParams param)
        {
            var collider = param.Collider;
            var goCapsuleCollider = collider.GetUnityCollider<CapsuleCollider>();;
            if(goCapsuleCollider == null)
            {
                return;
            }
            goCapsuleCollider.radius = param.Radius;
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetHeight)]
    public class EventHanlder_GameplayCapsuleColliderSetHeight: AInvokeHandler<SetCapsuleColliderHeightParams>
    {
        public override void Handle(SetCapsuleColliderHeightParams param)
        {
            var collider = param.Collider;
            var goCapsuleCollider = collider.GetUnityCollider<CapsuleCollider>();;
            if(goCapsuleCollider == null)
            {
                return;
            }
            goCapsuleCollider.height = param.Height;
        }
    }
    
    [Invoke(PhysicsInvokeType.CapsuleColliderSetDirection)]
    public class EventHanlder_GameplayCapsuleColliderSetDirectiont: AInvokeHandler<SetCapsuleColliderDirectionParams>
    {
        public override void Handle(SetCapsuleColliderDirectionParams param)
        {
            var collider = param.Collider;
            var goCapsuleCollider = collider.GetUnityCollider<CapsuleCollider>();;
            if(goCapsuleCollider == null)
            {
                return;
            }
            goCapsuleCollider.direction = param.Direction;
            
        }
    }
    #endregion

    #region SphereCollider
    [Invoke(PhysicsInvokeType.SphereColliderAwake)]
    public class EventHanlder_GameplaySphereColliderAwake : AInvokeHandler<GameplaySphereColliderAwakeParams>
    {
        public override void Handle(GameplaySphereColliderAwakeParams param)
        {
            var collider = param.Collider;
            var goSphereCollider = collider.GetOrCreateUnityCollider<SphereCollider>();
            ViewEntityMapManager.Instance.MapCollider(collider, goSphereCollider);
            
            goSphereCollider.center = param.InitParams.Center;
            goSphereCollider.radius = param.InitParams.Radius;
        }
    }
    
    [Invoke(PhysicsInvokeType.SphereColliderDestroy)]
    public class EventHanlder_GameplaySphereColliderDestroy : AInvokeHandler<GameplaySphereColliderDestroyParams>
    {
        public override void Handle(GameplaySphereColliderDestroyParams param)
        {
            var collider = param.Collider;
            ViewEntityMapManager.Instance.UnMapCollider(collider);

            var goSphereCollider = collider.GetUnityCollider<SphereCollider>();

            if(goSphereCollider == null)
            {
                return;
            }

            Object.Destroy(goSphereCollider);
        }
    }
    
    [Invoke(PhysicsInvokeType.SphereColliderSetCenter)]
    public class EventHanlder_GameplaySphereColliderSetCenter : AInvokeHandler<SetSphereColliderCenterParams>
    {
        public override void Handle(SetSphereColliderCenterParams param)
        {
            var collider = param.Collider;
            var goSphereCollider = collider.GetUnityCollider<SphereCollider>();

            if(goSphereCollider == null)
            {
                return;
            }

            goSphereCollider.center = param.Center;
        }
    }
    
    [Invoke(PhysicsInvokeType.SphereColliderSetRadius)]
    public class EventHanlder_GameplaySphereColliderSetRadius : AInvokeHandler<SetSphereColliderRadiusParams>
    {
        public override void Handle(SetSphereColliderRadiusParams param)
        {
            var collider = param.Collider;
            var goSphereCollider = collider.GetUnityCollider<SphereCollider>();

            if(goSphereCollider == null)
            {
                return;
            }

            goSphereCollider.radius = param.Radius;
        }
    }
    #endregion
    
    #region MesheCollider
    [Invoke(PhysicsInvokeType.MeshColliderAwake)]
    public class EventHanlder_GameplayMeshColliderAwake : AInvokeHandler<GameplayMeshColliderAwakeParams>
    {
        public override void Handle(GameplayMeshColliderAwakeParams param)
        {
            var collider = param.Collider;
            var goMeshCollider = collider.GetOrCreateUnityCollider<MeshCollider>();
            collider.Convex = goMeshCollider.convex;
            ViewEntityMapManager.Instance.MapCollider(collider, goMeshCollider);
        }
    }
    
    [Invoke(PhysicsInvokeType.MeshColliderDestroy)]
    public class EventHanlder_GameplayMeshColliderDestroy : AInvokeHandler<GameplayMeshColliderDestroyParams>
    {
        public override void Handle(GameplayMeshColliderDestroyParams param)
        {
            var collider = param.Collider;
            var goMeshCollider = collider.GetUnityCollider<MeshCollider>();
            ViewEntityMapManager.Instance.UnMapCollider(collider);

            if(goMeshCollider == null)
            {
                return;
            }

            Object.Destroy(goMeshCollider);
        }
    }
    #endregion

    #region HeightFieldCollider
    [Invoke(PhysicsInvokeType.HeightFieldColliderAwake)]
    public class EventHanlder_GameplayHeightFieldCColliderAwake : AInvokeHandler<GameplayHeightFieldColliderAwakeParams>
    {
        public override void Handle(GameplayHeightFieldColliderAwakeParams param)
        {
            var collider = param.Collider;
            var goTerrainCollider = collider.GetOrCreateUnityCollider<TerrainCollider>();
            ViewEntityMapManager.Instance.MapCollider(collider, goTerrainCollider);
        }
    }
    
    [Invoke(PhysicsInvokeType.HeightFieldColliderDestroy)]
    public class EventHanlder_GameplayHeightFieldCColliderDestroy : AInvokeHandler<GameplayHeightFieldDestroyParams>
    {
        public override void Handle(GameplayHeightFieldDestroyParams param)
        {
            var collider = param.Collider;
            var goTerrainCollider = collider.GetUnityCollider<TerrainCollider>();
            ViewEntityMapManager.Instance.UnMapCollider(collider);

            if(goTerrainCollider == null)
            {
                return;
            }

            Object.Destroy(goTerrainCollider);
        }
    }
    

    #endregion
}