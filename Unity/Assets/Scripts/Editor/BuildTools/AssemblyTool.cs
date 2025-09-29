using System.IO;
using System.Threading;
using Bliss;
using Bliss.Core;
using Unity;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace Editor.BuildTools
{
    public static class AssemblyTool
    {
        /// <summary>
        /// Unity线程的同步上下文
        /// </summary>
        static SynchronizationContext unitySynchronizationContext { get; set; }

        /// <summary>
        /// 程序集名字数组
        /// </summary>
        public static readonly string[] DllNames = { "Bliss.Model", "Bliss.Logic", "Bliss.ViewModel", "Bliss.ViewLogic", };
        
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            unitySynchronizationContext = SynchronizationContext.Current;
            ReGenerateProjectFiles();
        }

        static void ReGenerateProjectFiles()
        {
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        }
        
        
        /// <summary>
        /// 菜单和快捷键编译按钮
        /// </summary>
        [MenuItem("Tools/Bliss/Compile _F6", false, 1)]
        static void MenuItemOfCompile()
        {
            // 强制刷新一下，防止关闭auto refresh，文件修改时间不准确
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            DoCompile();
        }

        /// <summary>
        /// 菜单和快捷键热重载按钮
        /// </summary>
        [MenuItem("Tools/Bliss/Reload _F7", false, 2)]
        static void MenuItemOfReload()
        {
            if (Application.isPlaying)
            {
                CodeLoader.Instance?.Reload();
            }
        }
        
        /// <summary>
        /// 执行编译代码流程
        /// </summary>
        public static void DoCompile()
        {
            // 强制刷新一下，防止关闭auto refresh，编译出老代码
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            bool isCompileOk = CompileDlls();
            if (!isCompileOk)
            {
                return;
            }

            CopyHotUpdateDlls();
            ReGenerateProjectFiles();
            
            Debug.Log($"Compile Finish!");
        }

        
        /// <summary>
        /// 编译成dll
        /// </summary>
        static bool CompileDlls()
        {
            // 运行时编译需要先设置为UnitySynchronizationContext, 编译完再还原为CurrentContext
            SynchronizationContext lastSynchronizationContext = Application.isPlaying ? SynchronizationContext.Current : null;
            SynchronizationContext.SetSynchronizationContext(unitySynchronizationContext);

            bool isCompileOk = false;

            try
            {
                Directory.CreateDirectory(BuildDefine.BuildOutputDir);
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
                ScriptCompilationSettings scriptCompilationSettings = new()
                {
                    group = group,
                    target = target,
                    extraScriptingDefines = new[] { "UNITY_COMPILE" },
                    options = EditorUserBuildSettings.development ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None
                };
                ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, BuildDefine.BuildOutputDir);
                isCompileOk = result.assemblies.Count > 0;
                EditorUtility.ClearProgressBar();
            }
            finally
            {
                if (lastSynchronizationContext != null)
                {
                    SynchronizationContext.SetSynchronizationContext(lastSynchronizationContext);
                }
            }

            return isCompileOk;
        }

        /// <summary>
        /// 将dll文件复制到加载目录
        /// </summary>
        static void CopyHotUpdateDlls()
        {
            FileHelper.CleanDirectory(GlobalDefine.CodeDir);
            foreach (string dllName in DllNames)
            {
                string sourceDll = $"{BuildDefine.BuildOutputDir}/{dllName}.dll";
                string sourcePdb = $"{BuildDefine.BuildOutputDir}/{dllName}.pdb";
                File.Copy(sourceDll, $"{BuildDefine.CodeDir}/{dllName}.dll.bytes", true);
                File.Copy(sourcePdb, $"{BuildDefine.CodeDir}/{dllName}.pdb.bytes", true);
            }

            AssetDatabase.Refresh();
        }
    }
}