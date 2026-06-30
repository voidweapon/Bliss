using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;


namespace ScenePhysicalExport.Physx
{
    public class ScenePhysicalExportWnd :Editor
    {
        [MenuItem("Tools/PhysicalExport/CurrentScene")]
        public static void ExportCurrentScene()
        {
            string outputPath = string.Empty;
            outputPath = EditorUtility.SaveFolderPanel("Select Folder To Save", outputPath, "");
            bool result = SceneExporter.ExportScenePhysicsToMemoryPack(EditorSceneManager.GetActiveScene(), outputPath);
            if(result)
            {
                EditorUtility.DisplayDialog("Success", "Export Scene  Success", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Failure", "Export Scene  Failure, for detail information in the console window", "OK");
            }
        }
    }
}