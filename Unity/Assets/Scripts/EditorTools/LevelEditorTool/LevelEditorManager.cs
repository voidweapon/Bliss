using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Bliss.EditorTool.LevelEditorTool
{
    public class LevelEditorManager : IDisposable
    {
        // private static readonly Dictionary<int, Type> typeIdentifierToType = new Dictionary<int, Type>();
        // private static readonly Dictionary<Type, Type> configTypeToSceneActorType = new Dictionary<Type, Type>();
        
        private Dictionary<long, AbstractLevelEditorActor> networkIdentityToSceneActor = new Dictionary<long, AbstractLevelEditorActor>();

        public static LevelEditorManager Instance { private set; get; }
        
        public void Init()
        {
            Instance = this;
            
            // typeIdentifierToType.Clear();
            // configTypeToSceneActorType.Clear();
            //
            // RegisterConfigAndActor(typeof(ShooterGearConfig), typeof(ShooterGearEditorActor));
        }
        
        public void Dispose()
        {
            Instance = null;
        }

        public List<AbstractLevelEditorActor> LoadSceneSetting(Scene editorScene, string sceneName)
        {
            List<AbstractLevelEditorActor> levelActors = new List<AbstractLevelEditorActor>();

            if(!Directory.Exists(LevelEditorDefines.SceneSettingFolder))
            {
                Debug.LogError($"Scene setting folder not found: {LevelEditorDefines.SceneSettingFolder}");
                return levelActors;
            }
            string outputPath = $"{LevelEditorDefines.SceneSettingFolder}/{sceneName}.bytes";
            if(!File.Exists(outputPath))
            {
                Debug.LogError($"Scene setting file not found: {outputPath}");
                return  levelActors;
            }  
            
            var data = File.ReadAllBytes(outputPath);
            SceneSetting sceneSetting = Serializer.Deserialize(typeof(SceneSetting), data, 0, data.Length) as SceneSetting;
            if(sceneSetting == null)  
            {
                Debug.LogError($"Failed to deserialize scene setting: {outputPath}");
                return levelActors; 
            }
            
            //清除场景
            List<GameObject> rootGameObjects = new();
            editorScene.GetRootGameObjects(rootGameObjects);
            for (int i = 0; i < rootGameObjects.Count; i++)
            {
                GameObject.DestroyImmediate(rootGameObjects[i]);
            }
            networkIdentityToSceneActor.Clear();
            

            foreach (var sceneActorData in sceneSetting.LevelActors)
            {
                GameObject sceneActorGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sceneActorData.PrefabPath), editorScene);
                AbstractLevelEditorActor sceneActor = sceneActorGo.GetComponent<AbstractLevelEditorActor>();
                
                sceneActor.Deserialize(sceneActorData);
                levelActors.Add(sceneActor);
                networkIdentityToSceneActor.Add(sceneActor.NetworkIdentity, sceneActor);
                
                Undo.RegisterCreatedObjectUndo(sceneActorGo, "Load Scene Actor");
            }

            foreach (var levelActor in levelActors)
            {
                levelActor.EndInit();
            }
            
            return levelActors;
        }

        public void ExportSceneSetting(Scene editorScene, string sceneName)
        {
            if(!Directory.Exists(LevelEditorDefines.SceneSettingFolder))
            {
                Directory.CreateDirectory(LevelEditorDefines.SceneSettingFolder);
            }
            string outputPath = $"{LevelEditorDefines.SceneSettingFolder}/{sceneName}.bytes";
            
            List<GameObject> rootGameObjects = new();
            editorScene.GetRootGameObjects(rootGameObjects);
            
            List<GameObject> levelActors = new List<GameObject>();
            foreach (GameObject root in rootGameObjects)
            {
                FindLevelActorsRecursive(root, levelActors);
            }
            
            SceneSetting sceneSetting = new SceneSetting();
            foreach (var levelActor in levelActors)
            {
                AbstractLevelEditorActor editorActor = levelActor.GetComponent<AbstractLevelEditorActor>();
                if (editorActor != null)
                {
                    var sceneActorData = editorActor.Serialize();
                    sceneSetting.LevelActors.Add(sceneActorData);
                }
            }
            
            var data = Serializer.Serialize(sceneSetting);
            File.WriteAllBytes(outputPath, data);
            AssetDatabase.Refresh(); 
        }
        
        public AbstractLevelEditorActor GetSceneActor(long networkIdentity)
        {
            if (networkIdentityToSceneActor.TryGetValue(networkIdentity, out var actor))
            {
                return actor;
            }
            return null;
        }
                    
        private void FindLevelActorsRecursive(GameObject gameObject, List<GameObject> result)
        {
            if (gameObject.GetComponent<AbstractLevelEditorActor>() != null)
            {
                result.Add(gameObject);
            }
            
            foreach (Transform child in gameObject.transform)
            {
                FindLevelActorsRecursive(child.gameObject, result);
            }
        }

        // private void RegisterConfigAndActor(Type configType, Type sceneActorType)
        // {
        //     if (!configTypeToSceneActorType.TryAdd(configType, sceneActorType))
        //     {
        //         Debug.LogError($"Config type {configType.FullName} already registered");
        //         return;
        //     }
        //     typeIdentifierToType[configType.FullName.GetStableHashCode()] = configType;
        // }
    }
}