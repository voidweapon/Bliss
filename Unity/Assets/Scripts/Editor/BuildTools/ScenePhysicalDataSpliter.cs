using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Bliss.EditorTool.LevelEditorTool;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;

namespace ScenePhysicalExport
{
    public class ScenePhysicalDataSplit : UnityEditor.Editor
    {
        private static HashSet<Type> types = new HashSet<Type>()
        {
            typeof(BoxCollider),
            typeof(SphereCollider),
            typeof(CapsuleCollider),
            typeof(MeshCollider),
            typeof(Rigidbody),
            typeof(Transform),
        };
        
        private static HashSet<Type> physicalCmpTypes = new HashSet<Type>()
        {
            typeof(BoxCollider),
            typeof(SphereCollider),
            typeof(CapsuleCollider),
            typeof(MeshCollider),
            typeof(Rigidbody),
        };
        
        [MenuItem("Tools/PhysicalStrip/CurrentScene")]
        public static void ExportCurrentScene()
        {
            string outputPath = $"{Application.dataPath}/Assets/Res/Scenes";
            outputPath = EditorUtility.SaveFolderPanel("Select Folder To Save", outputPath, $"{EditorSceneManager.GetActiveScene().name}_PhysicalData.prefab");

            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }
            
            bool result = StripCurrentScene(EditorSceneManager.GetActiveScene(), outputPath);
            if(result)
            {
                EditorUtility.DisplayDialog("Success", "Export Scene  Success", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failure", "Export Scene  Failure, for detail information in the console window", "OK");
            }
        }
        
        [MenuItem("Tools/PhysicalStrip/SelectFolder")]
        public static void ExportCurrentFolder()
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }
            
            string outputPath = $"{Application.dataPath}/Assets/Res/Scenes";
            outputPath = EditorUtility.SaveFolderPanel("Select Folder To Save", outputPath, $"{EditorSceneManager.GetActiveScene().name}_PhysicalData.prefab");

            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }
            
            bool result = StripPrefabInFolder(new string[] { folderPath}, outputPath, outputPath);
            if(result)
            {
                EditorUtility.DisplayDialog("Success", "Export Prefab InFolder Success", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failure", "Export Prefab InFolder Failure, for detail information in the console window", "OK");
            }
        }
        
        [MenuItem("Tools/PhysicalStrip/SelectPrefab")]
        public static void ExportCurrentPrefab()
        {
            string prefabPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return;
            }
            
            string outputPath = $"{Application.dataPath}/Assets/Res/Scenes";
            outputPath = EditorUtility.SaveFolderPanel("Select Folder To Save", outputPath, $"{EditorSceneManager.GetActiveScene().name}_PhysicalData.prefab");

            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }
            
            StripPrefab(Selection.activeGameObject, outputPath, outputPath, true);
            Debug.Log($"Strip {prefabPath} ViewPrefab To: {outputPath}");
            Debug.Log($"Strip {prefabPath} PhysicalPrefab To: {outputPath}");
        }
        
        public static bool StripCurrentScene(Scene unityScene, string outputPath)
        {
            GameObject physicalData = null;
            GameObject viewData = null;
            try
            {
                GameObject[] gameObjectArray = unityScene.GetRootGameObjects();
                viewData = new GameObject($"{unityScene.name}_View");
                physicalData = new GameObject($"{unityScene.name}_Physical");
                foreach (GameObject go in gameObjectArray)
                {
                    if (CanExport(go))
                    {
                        var viewCopy = Instantiate(go, viewData.transform);
                        viewCopy.name = go.name;

                        if (go.GetComponent<Light>() == null)
                        {
                            var physicalCopy = Instantiate(go, physicalData.transform);
                            physicalCopy.name = go.name;
                        }
                    }
                }
                
                RemoveAllComponent(physicalData, types);
                
                RemoveComponents<Rigidbody>(viewData);
                RemoveComponents<Collider>(viewData);
                
                PrefabUtility.SaveAsPrefabAsset(viewData, $"{outputPath}/{viewData.name}.prefab");
                PrefabUtility.SaveAsPrefabAsset(physicalData, $"{outputPath}/{physicalData.name}.prefab");
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
            finally
            {
                if (physicalData != null) DestroyImmediate(physicalData);
                if (viewData != null) DestroyImmediate(viewData);
            }
        }

        private static bool StripPrefabInFolder(string[] folders, string viewOutputPath, string physicalOutputPath)
        {
            try
            {
                var prefabsGUIDs = AssetDatabase.FindAssets("t:Prefab", folders);
                foreach (var guid in prefabsGUIDs)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    StripPrefab(prefab, viewOutputPath, physicalOutputPath, false);
                }
                AssetDatabase.Refresh();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
        
        private static void StripPrefab(GameObject prefab, string viewOutputPath, string physicalOutputPath,bool refresh)
        {
            GameObject physicalCopy = null;
            GameObject viewCopy = null;
            try
            {
                viewCopy = Instantiate(prefab);
                viewCopy.name = $"{prefab.name}_View";
                        
                physicalCopy = Instantiate(prefab);
                physicalCopy.name = $"{prefab.name}_Physical";
                
                RemoveComponents<Rigidbody>(viewCopy);
                RemoveComponents<Collider>(viewCopy);
                RemoveComponents<AbstractLevelEditorActor>(viewCopy);
                
                RemoveAllComponent(physicalCopy, types);
                
                
                PrefabUtility.SaveAsPrefabAsset(viewCopy, $"{viewOutputPath}/{viewCopy.name}.prefab");
                
                bool havePhysicalCmp = HaveComponent(physicalCopy, types);
                if (havePhysicalCmp)
                {
                    PruePhysicalPrefab(physicalCopy, physicalCmpTypes);
                    PrefabUtility.SaveAsPrefabAsset(physicalCopy, $"{physicalOutputPath}/{physicalCopy.name}.prefab");
                }
                
                if (refresh)
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                if(physicalCopy != null) DestroyImmediate(physicalCopy);
                if (viewCopy != null) DestroyImmediate(viewCopy);
            }
        }
        
        
        static bool CanExport(GameObject go)
        {
            return go.activeInHierarchy &&
                   go.GetComponent<Camera>() == null;
        }

        private static void PruePhysicalPrefab(GameObject go, HashSet<Type> types)
        {
            bool haveComponents = PruePhysicalPrefabInner(go, types);
        }
        
        
        private static bool PruePhysicalPrefabInner(GameObject go, HashSet<Type> includeType)
        {
            bool haveComponents = false;
            
            foreach (var component in go.GetComponents<Component>())
            {
                if (includeType.Contains(component.GetType()))
                {
                    haveComponents = true;
                    break;
                }
            }
            
            //Scan Child
            bool childHasComponents = false;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                var child = go.transform.GetChild(i);
                bool childHasComponent = PruePhysicalPrefabInner(child.gameObject, includeType);
                childHasComponents |= childHasComponent;

                if (haveComponents && !childHasComponent)
                {
                    GameObject.DestroyImmediate(child.gameObject);
                }
            }
            
            return haveComponents || childHasComponents;
        }
        


        static bool HaveComponent(GameObject go, HashSet<Type> includeType)
        {
            foreach (var component in go.GetComponents<Component>())
            {
                if (includeType.Contains(component.GetType()))
                {
                    return true;
                }
            }
            
            foreach (var component in go.GetComponentsInChildren<Component>())
            {
                if (includeType.Contains(component.GetType()))
                {
                    return true;
                }
            }
            
            return false;
        }

        static void CopyGameObject(GameObject source, GameObject destination, string overrideName = null)
        {
            destination.name = overrideName ?? source.name;
            destination.tag = source.tag;
            destination.layer = source.layer;
            destination.isStatic = source.isStatic;
        }

        static void CopyComponent(GameObject source, GameObject destination, HashSet<Type> includeType)
        {
            var components = source.GetComponents<Component>();
            foreach (var component in components)
            {
                if (includeType.Contains(component.GetType()))
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(component);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(destination);
                }
            }
        }
        
        
        static void RemoveComponents<TComponent>(GameObject source) where TComponent : Component
        {
            foreach (var collider in source.GetComponents<TComponent>())
            {
                DestroyImmediate(collider);
            }
            foreach (var rigidbody in source.GetComponentsInChildren<TComponent>())
            {
                DestroyImmediate(rigidbody);
            }
        }
        
        static void RemoveAllComponent(GameObject source, HashSet<Type> excepts)
        {
            
            foreach (var component in source.GetComponents<Component>())
            {
                if (!excepts.Contains(component.GetType()))
                {
                    DestroyImmediate(component);
                }
            }
            
            foreach (var component in source.GetComponentsInChildren<Component>())
            {
                if (!excepts.Contains(component.GetType()))
                {
                    DestroyImmediate(component);
                }
            }
        }
        
    }
}

