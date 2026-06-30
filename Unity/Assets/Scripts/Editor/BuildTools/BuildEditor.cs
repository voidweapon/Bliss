using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HybridCLR.Editor.Commands;

namespace Bliss.BuildTools
{
    public class BuildEditor : UnityEditor.Editor
    {
        private const string ArtSceneFolderPath = "Assets/Art/Scenes";
        
        [MenuItem("Tools/Bliss/Build/1.ProcessScene/CurrentScene",  priority = 1)]
        public static void ProcessArtScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentScenePath = currentScene.path;
            BuildSceneProcessor.ProcessSingleArtScene(currentScene, "Assets/Res/Scenes");
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }
        
        [MenuItem("Tools/Bliss/Build/1.ProcessScene/AllScene", priority = 0)]
        public static void ProcessAllArtScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            var currentScenePath = currentScene.path;

            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { ArtSceneFolderPath });
            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                BuildSceneProcessor.ProcessSingleArtScene(scene, "Assets/Res/Scenes");
            }

            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Bliss/Build/2.Build Gameplay DLL")]
        public static void BuildGameplayDLL()
        {
            AssemblyTool.MenuItemOfCompile();
        }
        
        [MenuItem("Tools/Bliss/Build/3.Build HybridCLR AOT DLL")]
        public static void BuildHybridCLR_AOT_DLL()
        {
            PrebuildCommand.GenerateAll();
            HybridCLREditor.CopyAotDll();
        }
        
        [MenuItem("Tools/Bliss/Build/4.Build AssetBundle")]
        public static void BuildAssetBundle()
        {
            var buildTarget = BuildTarget.StandaloneOSX;
            var buildResult = BuildYooAsset.BuildInternal(buildTarget, "DefaultPackage", null);
            if (buildResult.Success)
            {
                var bootSetting = UnityEngine.Resources.Load<BootSetting>("BootSetting");
                BuildYooAsset.CopyToFileServer(buildResult.OutputPackageDirectory, buildTarget, bootSetting.AppVersion, "DefaultPackage");
            }
        }
        
    }
}