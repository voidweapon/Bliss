using System;
using Bliss.ViewModel;
using ET;
using UnityEngine;

namespace Bliss.ViewLogic.Tests
{
    [Event(ELogicSceneType.Client)]
    public class EventHandle_AfterCreateSceneActor_Client : AEvent<LogicScene, Event_AfterCreateSceneActor>
    {
        protected override async ETTask Run(LogicScene clienScene, Event_AfterCreateSceneActor eventInfo)
        {
            var actor = eventInfo.Actor;
            var resourcesLoader = clienScene.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                return;
            }

            var prefab = await resourcesLoader.LoadAssetAsync<GameObject>(eventInfo.ViewAssetPath);
            if (prefab == null)
            {
                return;
            }
            
            var viewGo = UnityEngine.Object.Instantiate(prefab);
            var logicTransform = actor.GetComponent<TransformComponent>();
            viewGo.transform.position = logicTransform.Transform.Location;
            viewGo.transform.rotation = logicTransform.Transform.Rotation;

            actor.AddComponent<GameObjectViewComponent, GameObjectViewComponentInitParam>(
                new GameObjectViewComponentInitParam()
                {
                    View = viewGo,
                    IsFromPool = false
                });
            
            var netRole = actor.GetComponent<NetRoleComponent>().LocalNetRole;

            viewGo.name = $"{viewGo.name} ({actor.InstanceId}) ({Enum.GetName(typeof(ENetRole), netRole)})";

            if (netRole == ENetRole.Authority)
            {
                foreach (var renderer in viewGo.transform.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }
            }
            
            await ETTask.CompletedTask;
        }
    }
}