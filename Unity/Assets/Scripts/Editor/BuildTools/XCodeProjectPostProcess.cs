using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.OSXStandalone;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Bliss.BuildTools
{
    public class XCodeProjectPostProcess : IPostprocessBuildWithReport
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            // Stop processing if build target is not iOS or MacOS
            if (buildTarget != BuildTarget.iOS && buildTarget != BuildTarget.StandaloneOSX)
                return;
            
            // Initialize PBXProject
            Debug.Log("[XCodeProjectPostProcess] " + pathToBuiltProject);
            
            /*
            //Fix Unity Plugin Folder Name
             string wrongUnityPluginsFolderPath = Path.Combine(pathToBuiltProject, "Unity/PlugIns");
             if (Directory.Exists(wrongUnityPluginsFolderPath))
             {
                 string unityPluginsFolderPath = Path.Combine(pathToBuiltProject, "Unity/Plugins");
                 string tempFolderPath = Path.Combine(pathToBuiltProject, "Unity/Plugins_Temp");
                 Debug.Log($"[XCodeProjectPostProcess] Fix Unity Plugins folder path: {wrongUnityPluginsFolderPath} to {unityPluginsFolderPath}");
                 //煞笔函数大小写不敏感
                 Directory.Move(wrongUnityPluginsFolderPath, tempFolderPath);
                 Directory.Move(tempFolderPath, unityPluginsFolderPath);
             }
             */
        }
        
        private static void MoveDirectory(string sourceDirectory, string targetDirectory)
        {
            MoveDirectory(sourceDirectory, targetDirectory, 0);

            void MoveDirectory(string source, string target, int depth)
            {
                if (!Directory.Exists(source))
                {
                    return;
                }

                if (!Directory.Exists(target))
                {
                    Directory.CreateDirectory(target);
                }

                var sourceFolder = new DirectoryInfo(source);
                foreach (var fileInfo in sourceFolder.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                {
                    var targetFile = Path.Combine(target, fileInfo.Name);

                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                    }

                    File.Move(fileInfo.FullName, targetFile);
                }

                foreach (var directoryInfo in sourceFolder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    var back = string.Join("\\", Enumerable.Repeat("..", depth));
                    MoveDirectory(directoryInfo.FullName,
                        Path.GetFullPath(Path.Combine(target, back, directoryInfo.Name)), depth + 1);
                }

                Directory.Delete(source);
            }
        }

        public int callbackOrder { get; } = 999;
        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log("[XCodeProjectPostProcess] Build Report: " + report.summary.outputPath);
        }
    }
}