using System;
using Bliss.Base.Collections;
using Bliss.CharacterControllers;
using Bliss.CharacterMovement;
using Bliss.ViewLogic;
using Bliss.ViewModel;
using ET;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bliss
{
    /// <summary>
    /// 场景切换流程2
    /// 先切换到空场景(Loading)来卸载, 然后切换到目标场景
    /// 以此来削减内存峰值
    /// <seealso cref="MapHelper.ChangeMap"/>
    /// </summary>
    [Event(ELogicSceneType.Level)]
    public class EventHandler_EndCreateLevelScene : AEvent<LogicScene, Event_EndCreateLevelScene>
    {
        protected override async ETTask Run(LogicScene levelScene, Event_EndCreateLevelScene eventInfo)
        {
            var gameplayWorld = levelScene.GetComponent<LevelGameWorldComponent>().GameplayWorld;
            var resourcesLoaderCmp = levelScene.GetComponent<ResourcesLoaderComponent>();
            LevelConfig levelConfig = ConfigManager.Instance.Tables.TbLevelConfig.GetOrDefault(eventInfo.LevelConfigId);

            // Scene Physical Data
            var physicalPath = $"Assets/Res/Prefabs/Scenes/{levelConfig.Name}_Physical.prefab";
            GameObject physicalData = null;
            if (resourcesLoaderCmp.CheckLocationValid(physicalPath))
            {
                var physicalDataPrefab = await resourcesLoaderCmp.LoadAssetAsync<GameObject>(physicalPath);
                var scenePhysicalActor = levelScene.AddChild<SceneActor>();
                scenePhysicalActor.AddComponent<TransformComponent>();
                scenePhysicalActor.AddComponent<GameplayLayer, int >(0);
                
                physicalData = GameObject.Instantiate(physicalDataPrefab);
                scenePhysicalActor.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(new GameObjectViewComponentInitParam()
                {
                    View = physicalData
                });
            }


            string levelLightSetting = $"Assets/Res/Prefabs/Scenes/{levelConfig.Name}_RuntimeLightDataAsset.asset";
            if (resourcesLoaderCmp.CheckLocationValid(levelLightSetting))
            {
                var lightDataAsset = await resourcesLoaderCmp.LoadAssetAsync<RuntimeLightDataAsset>(levelLightSetting);
                
                //TODO:Level Renderer Light Setting
                
                //Light Map
                LightmapData[] levelLightData = new LightmapData[lightDataAsset.lightmaps.Length];
                for (int i = 0; i < lightDataAsset.lightmaps.Length; i++)
                {
                    levelLightData[i] = new LightmapData();
                    levelLightData[i].lightmapColor = lightDataAsset.lightmaps[i];
                    levelLightData[i].lightmapDir = lightDataAsset.lightmapsDir[i];
                    levelLightData[i].shadowMask = lightDataAsset.shadowMasks[i];
                }
                
                LevelSceneViewTransitionComponent transition =
                    levelScene.GetComponent<LevelSceneViewTransitionComponent>();
                if (transition == null)
                {
                    throw new InvalidOperationException("The LevelScene has no transition state for lightmap setup.");
                }

                transition.captureLightingSettings();
                LightmapSettings.lightmapsMode = lightDataAsset.lightmapsMode;
                LightmapSettings.lightmaps = levelLightData;
                
                //Light Prob
                if (lightDataAsset.lightProbesAsset.coefficients.Length > 0)
                {
                    try
                    {
                        LightmapSettings.lightProbes = lightDataAsset.lightProbesAsset.lightProbes;
                        LightmapSettings.lightProbes.bakedProbes = lightDataAsset.lightProbesAsset.coefficients;
                    }
                    catch 
                    {
                        Log.Warning($"error when trying to load light probes for {lightDataAsset.name}");
                    }
                }
            }
            
            var viewPath = $"Assets/Res/Prefabs/Scenes/{levelConfig.Name}_View.prefab";
            GameObject viewData = null;
            if (resourcesLoaderCmp.CheckLocationValid(viewPath))
            {
                var viewDataPrefab = await resourcesLoaderCmp.LoadAssetAsync<GameObject>(viewPath);
                var sceneViewActor = levelScene.AddChild<SceneActor>();
                sceneViewActor.AddComponent<TransformComponent>();
                sceneViewActor.AddComponent<GameplayLayer, int >(0);
                
                viewData = GameObject.Instantiate(viewDataPrefab);
                sceneViewActor.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(new GameObjectViewComponentInitParam()
                {
                    View = viewData
                });
            }

            CreateSceneEntity(levelScene, physicalData, viewData);

            //Start Process Movement
            gameplayWorld.GetComponent<KinematicMoverManager>().Enable = true;
            
            //Player Input
            var playerInputComponent = levelScene.AddComponent<PlayerInputComponent>();
            var unitManager = gameplayWorld.GetComponent<UnitManager>();
            unitManager.GetUnit(unitManager.ObserveUnitId, out var unitRef);
            var localUnit = (Unit)unitRef;
            if (localUnit != null)
            {
                var characterController = localUnit.GetComponent<CharacterControllers.CharacterController>();
                if (characterController != null)
                {
                    characterController.InputIntentProcessPipeline?.SetInputSource(playerInputComponent);
                }
                
                var cameraComponent = levelScene.RootScene().GetComponent<CameraComponent>();
                cameraComponent.SetTarget(localUnit);
            }
            
            
            await ETTask.CompletedTask;
        }
        

        private static void CreateSceneEntity(LogicScene levelScene, GameObject physicalData, GameObject viewData)
        {
            Log.Debug("===== Start Create Level Scene Physics Data=====");
            WalkThroughGameObject(levelScene, physicalData, viewData);
            Log.Debug("================================================");
        }

        private static void WalkThroughGameObject(LogicScene levelScene, GameObject physicalGo, GameObject viewGo)
        {
            if(physicalGo == null) return;
            
            Rigidbody goRigidBody = physicalGo.GetComponent<Rigidbody>();
            using var goColliders = ListComponent<Collider>.Create();
           physicalGo.GetComponents<Collider>(goColliders);

           if (goRigidBody != null || (goColliders != null && goColliders.Count > 0))
           {
               var entity = levelScene.AddChild<SceneActor>();
               entity.AddComponent<TransformComponent>();
               entity.AddComponent<GameplayLayer, int>(physicalGo.layer);
               var gameObjectViewCmp = entity.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(new GameObjectViewComponentInitParam()
               {
                   View = physicalGo,
                   IsFromPool = false
               });
               gameObjectViewCmp.ViewMode = viewGo;
               
               if(goRigidBody != null)
               {
                   entity.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(new GameplayRigidbodyInitParams()
                   {
                       IsStatic = physicalGo.isStatic,
                       IsKinematic = goRigidBody.isKinematic,
                       UseGravity = true,
                   });
               }

               if (goColliders != null && goColliders.Count > 0)
               {
                   foreach (var collider in goColliders)
                   {
                       if(collider == null) continue;
                       
                       switch (collider)
                       {
                           case BoxCollider boxCollider:
                               entity.AddChild<GameplayBoxCollider, BoxColliderInitParams>( new BoxColliderInitParams() 
                               {
                                   Center = boxCollider.center,
                                   Size = boxCollider.size,
                                   IsTrigger = boxCollider.isTrigger
                               });
                               break;
                           case SphereCollider sphereCollider:
                               entity.AddChild<GameplaySphereCollider, SphereColliderInitParams>(new SphereColliderInitParams()
                               {
                                   Center = sphereCollider.center,
                                   Radius = sphereCollider.radius,
                                   IsTrigger = sphereCollider.isTrigger
                               });
                               break;
                           case CapsuleCollider capsuleCollider:
                               entity.AddChild<GameplayCapsuleCollider, CapsuleColliderInitParams>(new CapsuleColliderInitParams()
                               {
                                   Center = capsuleCollider.center,
                                   Radius = capsuleCollider.radius,
                                   Height = capsuleCollider.height,
                                   Direction = capsuleCollider.direction,
                                   IsTrigger = capsuleCollider.isTrigger
                               });
                               break;
                           case MeshCollider meshCollider:
                               entity.AddChild<GameplayMeshCollider, GameplayMeshColliderInitParams>(new GameplayMeshColliderInitParams()
                               {
                                   Convex = meshCollider.convex,
                                   IsTrigger = meshCollider.isTrigger
                               });
                               break;
                       }
                   }
               }
           }


           for (int i = 0; i < physicalGo.transform.childCount; i++)
           {
               var physicalChild = physicalGo.transform.GetChild(i);
               var viewChild = viewGo.transform.GetChild(i);
               
               //TODO: 如果同层级内有同名GameObject就会出问题,还是需要一个映射机制
               if (!string.Equals(viewChild.name, physicalChild.name))
               {
                   for (int j = 0; j < viewGo.transform.childCount; j++)
                   {
                       viewChild = viewGo.transform.GetChild(j);
                       if (viewChild.name == physicalChild.name)
                       {
                           break;
                       }
                   }
               }
               
               WalkThroughGameObject(levelScene, physicalChild.gameObject, viewChild.gameObject);
           }
        }
        
    }
}