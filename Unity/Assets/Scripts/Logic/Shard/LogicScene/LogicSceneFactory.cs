using System;
using System.Collections.Generic;
using System.IO;
using Bliss.Base.Collections;

namespace Bliss
{
    public static class LogicSceneFactory
    {
        /// <summary>
        /// 从场景配置文件创建场景Actor
        /// </summary>
        /// <param name="levelScene"></param>
        /// <param name="settingsFileName"></param>
        public static void LoadLogicSceneActorFromSettingFile(LogicScene levelScene, string settingsFileName, ENetRole localRole, ENetRole remoteRole)
        {
            Log.Info($"Start Load Level[{levelScene.Name}] Setting Data: {settingsFileName}");
            
            if (!File.Exists(settingsFileName))
            {
                return;
            }
            
            byte[] data = File.ReadAllBytes(settingsFileName);
            SceneSetting setting = (SceneSetting)Serializer.Deserialize(typeof(SceneSetting), data, 0, data.Length);
            
            CreateSceneActor(levelScene, setting.LevelActors, localRole, remoteRole);
            
            Log.Info($"End Load Level[{levelScene.Name}] Setting Data: {settingsFileName}");
        }

        public static void CreateSceneActor(LogicScene levelScene, List<SceneActorData> levelActors, 
            ENetRole localRole, ENetRole remoteRole,
            List<Entity> outResult = null)
        {
            outResult?.Clear();
            using ListComponent<ASceneActorCmp> tempList = ListComponent<ASceneActorCmp>.Create();
            
            foreach (var sceneActorData in levelActors)
            {
                int typeIdentifier = sceneActorData.TypeIdentifier;
                Type configType = SceneActorDataTypeManager.Instance.GetConfigType(typeIdentifier);
                if (configType == null)
                {
                    Log.Error(new Exception($"Config type {typeIdentifier} not found"));
                    continue;
                }
                
                Type sceneActorType = SceneActorDataTypeManager.Instance.GetSceneActorType(configType);
                if (sceneActorType == null)
                {
                    Log.Error(new Exception($"Scene type {configType} not found"));
                }
                
                ASceneActorConfig config = (ASceneActorConfig)Serializer.Deserialize(configType, sceneActorData.Data, 0, sceneActorData.Data.Length);
                SceneActor sceneActor = levelScene.AddChildWithId<SceneActor>(sceneActorData.NetworkIdentity, true);
                ASceneActorCmp sceneActorCmp = (ASceneActorCmp)sceneActor.AddComponent(sceneActorType);
                sceneActorCmp.SetConfig(config);
                sceneActorCmp.SetData(sceneActorData);
                
                sceneActor.AddComponent<TransformComponent>().SetPositionAndRotation(config.Position, config.Rotation);
                sceneActor.AddComponent<NetRoleComponent, NetRoleComponentInitParam>(new NetRoleComponentInitParam()
                {
                    LocalNetRole = localRole,
                    RemoteNetRole = remoteRole,
                });
                
                outResult?.Add(sceneActor);
                tempList.Add(sceneActorCmp);
            }
            
            foreach (var sceneActorCmp in tempList)
            {
                EventSystem.Instance.Push(levelScene, new Event_AfterCreateSceneActor()
                {
                    Actor = sceneActorCmp.Parent,
                    ViewAssetPath = sceneActorCmp.RawConfigData.PrefabPath,
                });
            }
            
            foreach (var sceneActorCmp in tempList)
            {
                sceneActorCmp.EndSceneInit();
            }
        }
    }
}