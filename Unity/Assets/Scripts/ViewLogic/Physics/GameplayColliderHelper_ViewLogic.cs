using Bliss.Base.Collections;
using UnityEngine;

namespace Bliss.ViewLogic.Physics
{
    public static class GameplayColliderHelper_ViewLogic
    {
        public static void MapCollider(Entity entity, GameObject viewGo)
        {
            var rigidbody = viewGo.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                var gameplayRigidbody = entity.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(new GameplayRigidbodyInitParams()
                {
                    IsStatic = viewGo.isStatic,
                    IsKinematic = rigidbody.isKinematic,
                    UseGravity = rigidbody.useGravity,
                });
                var transform = entity.GetComponent<TransformComponent>().Transform;
                gameplayRigidbody.Position = transform.Location;
                gameplayRigidbody.Rotation = transform.Rotation;
                gameplayRigidbody.Mass = rigidbody.mass;
            }
            
            using var colliderList = ListComponent<Collider>.Create();
            viewGo.GetComponents<Collider>(colliderList);
            
            foreach (var collider in colliderList)
            {
                GameplayCollider gameplayCollider = null;
                switch (collider)
                {
                    case  BoxCollider boxCollider:
                        gameplayCollider = entity.AddChild<GameplayBoxCollider, BoxColliderInitParams>(
                            new BoxColliderInitParams()
                            {
                                IsTrigger =  boxCollider.isTrigger,
                                Center = boxCollider.center,
                                Size = boxCollider.size,
                            });
                        break;
                    case  SphereCollider sphereCollider:
                        gameplayCollider = entity.AddChild<GameplaySphereCollider, SphereColliderInitParams>(
                            new SphereColliderInitParams()
                            {
                                IsTrigger =  sphereCollider.isTrigger,
                                Center = sphereCollider.center,
                                Radius = sphereCollider.radius,
                            });
                        break;
                    case  CapsuleCollider capsuleCollider:
                        gameplayCollider = entity.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(
                            new CapsuleColliderInitParams()
                            {
                                IsTrigger =  capsuleCollider.isTrigger,
                                Center = capsuleCollider.center,
                                Radius = capsuleCollider.radius,
                                Height = capsuleCollider.height,
                                Direction = capsuleCollider.direction,
                            });
                        break;
                    case MeshCollider meshCollider:
                        gameplayCollider = entity.AddChild<GameplayMeshCollider, GameplayMeshColliderInitParams>(
                            new GameplayMeshColliderInitParams()
                            {
                                IsTrigger =  meshCollider.isTrigger,
                                Convex = meshCollider.convex,
                            });
                        break;
                    case TerrainCollider terrainCollider:
                        gameplayCollider = entity.AddChild<GameplayHeightFieldCollider, GameplayHeightFieldColliderInitParams>(
                            new GameplayHeightFieldColliderInitParams()
                            {
                                EnableTreeCollider = true
                            });
                        break;
                }
                ViewEntityMapManager.Instance.MapCollider(gameplayCollider, collider);
            }
            
        }
    }
}