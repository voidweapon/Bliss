using System;
using System.IO;
using Bliss.Base.Collections;
using Bliss.ViewLogic;
using Bliss.ViewLogic.Debugger;
using Bliss.ViewLogic.Physics;
using Bliss.ViewModel;
using Bliss.ViewModel.Debugger;
using ET;
using Unity.Mathematics;
using UnityEngine;

namespace Bliss
{
    [Event(ELogicSceneType.GameplayWorld)]
    [Event(ELogicSceneType.Level)]
    public class EventHandle_Level_AfterCreateSceneActor : AEvent<LogicScene, Event_AfterCreateSceneActor>
    {
        protected override async ETTask Run(LogicScene levelScene, Event_AfterCreateSceneActor eventInfo)
        {
            var actor = (Entity)eventInfo.Actor;
            if (actor == null)
            {
                return;
            }


            
            var resourcesLoader = levelScene.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                return;
            }

            //TODO: 不要每次都拼接字符串
            string goExtension = Path.GetExtension(eventInfo.ViewAssetPath);
            string goPathWithoutExtension = eventInfo.ViewAssetPath.Substring(0, eventInfo.ViewAssetPath.Length  - goExtension.Length);
            string physicalGoPath = $"{goPathWithoutExtension}_Physical{goExtension}";
            string viewGoPath = $"{goPathWithoutExtension}_View{goExtension}";
            
            GameObject viewGo = null;
            if (resourcesLoader.CheckLocationValid(physicalGoPath))
            {
                var physicalPrefab = resourcesLoader.LoadAsset<GameObject>(physicalGoPath);
                viewGo = GameObject.Instantiate(physicalPrefab);
            }
            else
            { 
                viewGo = new GameObject();
            }
            
            string viewPath = eventInfo.ViewAssetPath;
            if (resourcesLoader.CheckLocationValid(viewGoPath))
            {
                viewPath = viewGoPath;
            }

            var globalEntitiesNode = levelScene.RootScene().GetComponent<ViewGlobalComponent>().EntitiesTransform;

            //设置父节点
            viewGo.transform.SetParent(globalEntitiesNode);
            
            //设置Transforms
            var logicTransform = actor.GetComponent<TransformComponent>();
            if (logicTransform != null)
            {
                viewGo.transform.position = logicTransform.Transform.Location;
                viewGo.transform.rotation = logicTransform.Transform.Rotation;
            }
            
            var gameObjectVieCmp =  actor.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(
                new GameObjectViewComponentInitParam()
                {
                    View = viewGo,
                    ViewAssetPath = eventInfo.ViewAssetPath,
                    IsFromPool = false
                });
            
            GameplayColliderHelper_ViewLogic.MapCollider(actor, viewGo);
            
#if UNITY_EDITOR
            var netRole = actor.GetComponent<NetRoleComponent>()?.LocalNetRole ?? ENetRole.Authority;
            if (netRole == ENetRole.AutonomousProxy)
            {
                levelScene.RootScene().GetComponent<TransformStatDebugComponent>().RegisterWatchTransform(0, viewGo.transform);
            }
            
            viewGo.name = $"{actor.GetType().Name} ({actor.InstanceId}) ({Enum.GetName(typeof(ENetRole), netRole)})";
#endif
            
            //ViewNode同步创建直接返回
            await ETTask.CompletedTask;
            
            //异步记载ViewModel
            gameObjectVieCmp.LoadViewmodel(viewPath).Coroutine();
        }
        
    }
}