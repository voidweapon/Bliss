using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bliss;
using ScenePhysicalExport;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace Bliss.BuildTools
{
    public class BuildSceneProcessor : UnityEditor.Editor
    {
        public static void ProcessSingleArtScene(Scene scene, string outputPath)
        {
            string sceneName = scene.name;
            string savePath = Path.Combine(outputPath, $"{sceneName}.unity");
            
            bool modified =  false;
            if (!File.Exists(savePath))
            {
                modified = true;
            }
            else
            {
                System.DateTime sceneLastWriteTime = File.GetLastWriteTime(scene.path);
                System.DateTime outputSceneLastWriteTime = File.GetLastWriteTime(savePath);

                if (sceneLastWriteTime > outputSceneLastWriteTime)
                {
                    modified = true;
                }
            }

            if (!modified)
            {
                Debug.Log($"Skip {sceneName}.unity asset because not found modify");
            }

            bool saveResult = EditorSceneManager.SaveScene(scene, savePath, true);
            if(!saveResult)
            {
                Debug.LogError($"Failed to save  scene to {savePath}");
                return;
            }

            CopyLightMap($"{outputPath}/{scene.name}");
            
            var outputScene = EditorSceneManager.OpenScene(savePath, OpenSceneMode.Single);
            
            Debug.Log($"Start Process Scene[{sceneName}]");
            
            //1. Strip All ProBuild Script
            Debug.Log("Strip All ProBuild Script");
            if (!StripAllProBuildScript())
            {
                return;
            }
            
            string stripDataOutPutPath = $"Assets/Res/Prefabs/Scenes";

            FixLightmapReference(outputScene);
            BuildSceneRuntimeLightData(outputScene, stripDataOutPutPath);
            
            //2.Strip Physical Data And View Data
            if (!Directory.Exists(stripDataOutPutPath))
            {
                Directory.CreateDirectory(stripDataOutPutPath);
            }
            Debug.Log($"Strip Physical Data And View Data, save to {stripDataOutPutPath}");

            ScenePhysicalDataSplit.StripCurrentScene(outputScene, stripDataOutPutPath);
            
            //3.Clear Scene
            Debug.Log($"Clear Scene MainCamera");
            
            var rootGameObjects = outputScene.GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; i++)
            {
                GameObject.DestroyImmediate(rootGameObjects[i]);
            }
            
            //4. Save Scene
            EditorSceneManager.SaveScene(outputScene, savePath);
                
            Debug.Log($"Finish Process Scene[{sceneName}]");
            
        }

        private static bool StripAllProBuildScript()
        {
            var typeStripProBuilderScripts =  typeof(ProBuilderEditor).Assembly.GetType("UnityEditor.ProBuilder.Actions.StripProBuilderScripts");
            if (typeStripProBuilderScripts == null)
            {
                Debug.LogError("Type[StripProBuilderScriptsType] not found");
                return false;
            }
            
            var methodGetMeshesInActiveScenes = typeStripProBuilderScripts.GetMethod("GetMeshesInActiveScenes",  BindingFlags.NonPublic | BindingFlags.Static);
            if (methodGetMeshesInActiveScenes == null)
            {
                Debug.LogError("Type[StripProBuilderScriptsType] not found method [GetMeshesInActiveScenes]");
                return false;
            }
            
            var methodDoStrip = typeStripProBuilderScripts.GetMethod("DoStrip",  BindingFlags.Public | BindingFlags.Static);
            if (methodDoStrip == null)
            {
                Debug.LogError("Type[StripProBuilderScriptsType] not found method [DoStrip]");
                return false;
            }
            
            var all = (List<ProBuilderMesh>)methodGetMeshesInActiveScenes.Invoke(null, new object[] { });
            if (all == null || all.Count == 0)
            {
                return true;
            }
            
            for (int i = 0, c = all?.Count ?? 0; i < c; i++)
            {
                ProBuilderMesh proBuildMesh = all[i];
                methodDoStrip.Invoke(null, new object[] {proBuildMesh, true });
            }

            return true;
        }

        private static string[] lightmaps;
        private static string[] lightmapDirs;
        private static string[] shadowMasks;
        private static bool CopyLightMap(string outputPath)
        {
            lightmaps = new string[LightmapSettings.lightmaps.Length];
            lightmapDirs = new string[LightmapSettings.lightmaps.Length];
            shadowMasks = new string[LightmapSettings.lightmaps.Length];
            if (Lightmapping.lightingDataAsset != null)
            {
                var lightDataAssetPath = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                AssetDatabase.CopyAsset(lightDataAssetPath, $"{outputPath}/{Path.GetFileName(lightDataAssetPath)}");
                
            }
            for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            {
                if (LightmapSettings.lightmaps[i].lightmapColor != null)
                {
                    string colorPath = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].lightmapColor);
                    lightmaps[i] = Path.GetFileName(colorPath);
                    AssetDatabase.CopyAsset(colorPath, $"{outputPath}/{lightmaps[i]}");
                }
                
                if (LightmapSettings.lightmaps[i].lightmapDir != null)
                {
                    var dirPath = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].lightmapDir);
                    lightmapDirs[i] =  Path.GetFileName(dirPath);
                    AssetDatabase.CopyAsset(dirPath, $"{outputPath}/{lightmapDirs[i]}");
                }                
                
                if(LightmapSettings.lightmaps[i].shadowMask != null)
                {
                    var shadowMaskPath = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].shadowMask);
                    shadowMasks[i] =  Path.GetFileName(shadowMaskPath);
                    AssetDatabase.CopyAsset(shadowMaskPath, $"{outputPath}/{shadowMasks[i]}");
                }
            }
            return true;
        }

        private static bool FixLightmapReference(Scene outputScene)
        {
            string sceneName = outputScene.name;
            Debug.Log($"FixLightmapReference: {outputScene.path}");
            string lightmapSettingPath = $"{Path.GetDirectoryName(outputScene.path)}/{sceneName}";
            var lightingDataAsset = AssetDatabase.LoadAssetAtPath<LightingDataAsset>($"{lightmapSettingPath}/LightingData.asset");
            if (lightingDataAsset != null)
            {
                Lightmapping.lightingDataAsset = lightingDataAsset;
            }

            for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            {
                if (!string.IsNullOrEmpty(lightmaps[i]))
                {
                    LightmapSettings.lightmaps[i].lightmapColor = AssetDatabase.LoadAssetAtPath<Texture2D>($"{lightmapSettingPath}/{lightmaps[i]}");
                }
                if (!string.IsNullOrEmpty(lightmapDirs[i]))
                {
                    LightmapSettings.lightmaps[i].lightmapDir = AssetDatabase.LoadAssetAtPath<Texture2D>($"{lightmapSettingPath}/{lightmapDirs[i]}");
                }
                if (!string.IsNullOrEmpty(shadowMasks[i]))
                {
                    LightmapSettings.lightmaps[i].shadowMask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{lightmapSettingPath}/{shadowMasks[i]}");
                }
            }
            
            return true;
        }

        private static bool BuildSceneRuntimeLightData(Scene outputScene, string outputPath)
        {
            RuntimeLightDataAsset lightDataAsset = ScriptableObject.CreateInstance<RuntimeLightDataAsset>();
            lightDataAsset.lightmaps = new Texture2D[LightmapSettings.lightmaps.Length];
            lightDataAsset.lightmapsDir = new Texture2D[LightmapSettings.lightmaps.Length];
            lightDataAsset.shadowMasks = new Texture2D[LightmapSettings.lightmaps.Length];
            lightDataAsset.lightmapsMode = LightmapSettings.lightmapsMode;
            lightDataAsset.hasRealtimeLights = LightmapSettings.lightmapsMode == LightmapsMode.CombinedDirectional;

            for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
            {
                lightDataAsset.lightmaps[i] =  LightmapSettings.lightmaps[i].lightmapColor;
                lightDataAsset.lightmapsDir[i] = LightmapSettings.lightmaps[i].lightmapDir;
                lightDataAsset.shadowMasks[i] = LightmapSettings.lightmaps[i].shadowMask;
            }
            
            lightDataAsset.lightProbesAsset = ScriptableObject.CreateInstance<RuntimeLightProbeAsset>();
            lightDataAsset.lightProbesAsset.coefficients = LightmapSettings.lightProbes.bakedProbes;
            lightDataAsset.lightProbesAsset.lightProbes = LightmapSettings.lightProbes;
            
            AssetDatabase.CreateAsset(lightDataAsset.lightProbesAsset, $"{outputPath}/{outputScene.name}_RuntimeLightProbeAsset.asset");
            AssetDatabase.CreateAsset(lightDataAsset, $"{outputPath}/{outputScene.name}_RuntimeLightDataAsset.asset");
            
            return true;
        }
            
    }
}