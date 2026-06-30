using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bliss.EditorTool.LevelEditorTool
{
    public class LevelEditorWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Level Editor")]
        private static void OpenWindow()
        {
            var window = GetWindow<LevelEditorWindow>();
            
            window.OnClose -= window.ForceExitEditorModelInner;
            window.OnClose += window.ForceExitEditorModelInner;
            
            EditorApplication.quitting -= window.ForceExitEditorModelInner;
            EditorApplication.quitting += window.ForceExitEditorModelInner;
            
            window.Show();
        }

        private bool _inEditorModel;
        private Scene _editorScene;
        private Scene _currentScene;
        private LevelEditorManager  _levelEditorManager;
        
        [DisableIf(nameof(_inEditorModel))]
        [Button("进入编辑模式", ButtonSizes.Large)]
        private void StartEditorModel()
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            _currentScene = currentScene;
            ClearSceneDirtyFlag(currentScene);
            _editorScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _editorScene.name = $"{currentScene.name}_Setting(Editing)";
            _inEditorModel = true;
            EditorSceneManager.SetActiveScene(currentScene);

            SceneVisibilityManager.instance.DisableAllPicking();
            LockScene(currentScene, true);
            
            ClearNetworkIdentityGenerateInfo();
            
            EditorSceneManager.sceneDirtied -= EditorSceneManagerOnSceneDirtied;
            ObjectChangeEvents.changesPublished -= ObjectChangeEventsOnChangesPublished;

            _levelEditorManager = new LevelEditorManager();
            _levelEditorManager.Init();
            
            EditorSceneManager.sceneDirtied += EditorSceneManagerOnSceneDirtied;
            ObjectChangeEvents.changesPublished += ObjectChangeEventsOnChangesPublished;
        }
        

        [EnableIf(nameof(_inEditorModel))]
        [Button("退出编辑模式", ButtonSizes.Large)]
        private void ExitEditorModel()
        {
            ExitEditorModelInner();
        }

        private void ForceExitEditorModelInner()
        {
            ExitEditorModelInner(true);
        }
        
        private void ExitEditorModelInner(bool forceExit = false)
        {
            if(!_inEditorModel) return;

            if (!forceExit && !EditorUtility.DisplayDialog("编辑模式", "是否退出编辑模式", "退出", "取消"))
            {
                return;
            }

            if (_levelEditorManager != null)
            {
                _levelEditorManager.ExportSceneSetting(_editorScene, _currentScene.name);
            }
            
            _levelEditorManager?.Dispose();
            _levelEditorManager = null;
            
            EditorSceneManager.sceneDirtied -= EditorSceneManagerOnSceneDirtied;
            ObjectChangeEvents.changesPublished -= ObjectChangeEventsOnChangesPublished;
            ClearNetworkIdentityGenerateInfo();
            
            EditorSceneManager.CloseScene(_editorScene, true);
            _inEditorModel = false;
            
            var currentScene = EditorSceneManager.GetActiveScene();
            LockScene(currentScene, false);
            
            SceneVisibilityManager.instance.EnableAllPicking();
        }
        
        [EnableIf(nameof(_inEditorModel))]
        [Button("导入场景配置", ButtonSizes.Large)]
        private void ImportSceneSetting()
        {
            string filePath = EditorUtility.OpenFilePanel("Scene Settings", LevelEditorDefines.SceneSettingFolder, "");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            Debug.Log(filePath);
            var sceneActors = _levelEditorManager.LoadSceneSetting(_editorScene, _currentScene.name);
            foreach (var sceneActor in sceneActors)
            {
                TrySetActorNetworkIdentify(_editorScene, sceneActor.gameObject.GetInstanceID(), out var _);
            }

        }
        
        [EnableIf(nameof(_inEditorModel))]
        [Button("导出场景配置", ButtonSizes.Large)]
        private void ExportSceneSetting()
        {
            string filePath = EditorUtility.SaveFilePanel("Scene Settings", LevelEditorDefines.SceneSettingFolder, $"{_currentScene.name}_setting","json");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            Debug.Log(filePath);
            
            if (_levelEditorManager != null)
            {
                _levelEditorManager.ExportSceneSetting(_editorScene, _currentScene.name);
            }
        }
        
        /// <summary>
        /// 重定向创建对象到编辑场景
        /// </summary>
        /// <param name="scene"></param>
        private void EditorSceneManagerOnSceneDirtied(Scene scene)
        {
            if (scene == _editorScene)
            {
                return;
            }
            
            NativeList<int> moveObjects = new NativeList<int>(1, Allocator.Temp);
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (!rootGameObject.hideFlags.HasFlag(HideFlags.NotEditable))
                {
                    moveObjects.Add(rootGameObject.GetInstanceID());
                }
            }
            
            var moveObjectAry =  moveObjects.ToArray(Allocator.Temp);
            EditorSceneManager.MoveGameObjectsToScene(moveObjectAry, _editorScene);
            
            moveObjectAry.Dispose();
            moveObjects.Dispose();

            // var rootGameObjects = scene.GetRootGameObjects();
            // for (int i = rootGameObjects.Length - 1; i >= 0; i--)
            // {
            //     var rootGameObject =  rootGameObjects[i];
            //     if (!rootGameObject.hideFlags.HasFlag(HideFlags.NotEditable))
            //     {
            //         EditorSceneManager.MoveGameObjectToScene(rootGameObject, _editorScene);
            //     }
            // }

            ClearSceneDirtyFlag(scene);
            ClearSceneDirtyFlag(_editorScene);
        }

        private static void ClearSceneDirtyFlag(Scene scene)
        {
            var clearSceneDirtyMethod =  typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", BindingFlags.Static | BindingFlags.NonPublic);
            if (clearSceneDirtyMethod != null)
            {
                clearSceneDirtyMethod.Invoke(null, new object[] {scene});
            }
            else
            {
                Debug.Log($"not found ClearSceneDirtiness in EditorSceneManager");
            }
        }

        private static void LockScene(Scene scene, bool locked)
        {
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                LockObject(rootGameObject, locked);
            }
        }

        private static void LockObject(GameObject gameObject, bool locked)
        {
            if (locked)
            {
                gameObject.hideFlags |= HideFlags.NotEditable;
            }
            else
            {
                gameObject.hideFlags &= (~HideFlags.NotEditable);
            }

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                LockObject(gameObject.transform.GetChild(i).gameObject, locked);
            }
        }
        
        private void ObjectChangeEventsOnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                var eventType = stream.GetEventType(i);
                switch (eventType)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                    {
                        stream.GetCreateGameObjectHierarchyEvent(i, out var createGameObject);
                        TrySetActorNetworkIdentify(createGameObject.scene, createGameObject.instanceId, out var networkIdentity);
                        break;
                    }
                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
                        TrySetActorNetworkIdentify(changeGameObjectStructure.scene, changeGameObjectStructure.instanceId,  out var networkIdentity);
                        break;
                    }
                    case ObjectChangeKind.DestroyGameObjectHierarchy:
                    {
                        stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyGameObject);
                        OnDestroyGameObject(destroyGameObject.scene, destroyGameObject.instanceId);
                        break;
                    }
                    default:
                        continue;
                }
            }
            ClearSceneDirtyFlag(_editorScene);
        }

        private long _maxNetworkIdentity;
        private Dictionary<int, long> _dicInstanceId2NetworkIdentity = new Dictionary<int, long>();
        private HashSet<long> _NetworkIdentitySet =  new HashSet<long>();
        private Queue<long> _spareNetworkIdentityQueue =  new Queue<long>();
        private void TrySetActorNetworkIdentify(Scene scene, int instanceId, out long networkIdentity)
        {
            networkIdentity = 0;
            if (scene != _editorScene)
            {
                return;
            }
            
            var go =  EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null)
            {
                return;
            }
            
            var actor = go.GetComponent<AbstractLevelEditorActor>();
            if (actor == null)
            {
                if (_dicInstanceId2NetworkIdentity.ContainsKey(instanceId))
                {
                    OnDestroyGameObject(scene, instanceId);
                }

                return;
            }

            if (actor.NetworkIdentity == 0 || _NetworkIdentitySet.Contains(actor.NetworkIdentity))
            {
                actor.NetworkIdentity = GenerateNetworkIdentity();
            }
            networkIdentity = actor.NetworkIdentity;
            _dicInstanceId2NetworkIdentity[instanceId] = networkIdentity;
            _NetworkIdentitySet.Add(networkIdentity);
        }

        private void OnDestroyGameObject(Scene scene, int instanceId)
        {
            if (scene != _editorScene)
            {
                return;
            }

            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go != null)
            {
                return;
            }
            
            if (!_dicInstanceId2NetworkIdentity.Remove(instanceId, out var networkIdentity))
            {
                 return;   
            }
            
            _spareNetworkIdentityQueue.Enqueue(networkIdentity);
            _NetworkIdentitySet.Remove(networkIdentity);
            
            if (networkIdentity == _maxNetworkIdentity)
            {
                while (_maxNetworkIdentity > 0)
                {
                    _maxNetworkIdentity--;
                    if (_NetworkIdentitySet.Contains(_maxNetworkIdentity))
                    {
                        break;
                    }
                }
            }
        }

        private long GenerateNetworkIdentity()
        {
            if (_spareNetworkIdentityQueue.Count == 0)
            {
                long id = ++_maxNetworkIdentity;
                return id;
            }

            return _spareNetworkIdentityQueue.Dequeue();
        }

        private void ClearNetworkIdentityGenerateInfo()
        {
            _maxNetworkIdentity = 0;
            _dicInstanceId2NetworkIdentity.Clear();
            _NetworkIdentitySet.Clear();
            _spareNetworkIdentityQueue.Clear();
        }
    }
}