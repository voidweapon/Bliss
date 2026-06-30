using System.IO;
using System.Threading;
using Bliss;
using Bliss.Core;
using Unity;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace Bliss.BuildTools
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
            HookLoadDLL();
        }

        static void ReGenerateProjectFiles()
        {
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        }

        #region Hook Load Gameplay DLL

        static void HookLoadDLL()
        {
            EditorApplication.playModeStateChanged -= OnEditorApplicationOnplayModeStateChanged;
            EditorApplication.playModeStateChanged += OnEditorApplicationOnplayModeStateChanged;
        }

        private static void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                {
                    OnExitingEditMode();
                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                {
                    OnExitingPlayMode();
                    break;
                }
            }
        }

        /// <summary>
        /// 退出编辑模式时处理(即将进入运行模式)
        /// EnableDll模式时, 屏蔽掉Library的dll(通过改文件后缀方式屏蔽), 仅使用Define.CodeDir下的dll
        /// </summary>
        static void OnExitingEditMode()
        {
            BootSetting globalConfig = Resources.Load<BootSetting>("BootSetting");
            if (!globalConfig.TestDLLInEditor)
            {
                return;
            }

            foreach (string dll in DllNames)
            {
                string dllFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.dll";
                if (File.Exists(dllFile))
                {
                    string dllDisableFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.dll.DISABLE";
                    if (File.Exists(dllDisableFile))
                    {
                        File.Delete(dllDisableFile);
                    }

                    File.Move(dllFile, dllDisableFile);
                    Debug.Log($"[AssemblyTool] Disable Library dll {dll}.dll");
                }

                string pdbFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.pdb";
                if (File.Exists(pdbFile))
                {
                    string pdbDisableFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.pdb.DISABLE";
                    if (File.Exists(pdbDisableFile))
                    {
                        File.Delete(pdbDisableFile);
                    }

                    File.Move(pdbFile, pdbDisableFile);
                    Debug.Log($"[AssemblyTool] Disable Library dll {dll}.pdb");
                }
            }
        }

        /// <summary>
        /// 退出运行模式时处理(即将进入编辑模式)
        /// 还原Library里面屏蔽掉的dll(HybridCLR或者非EnableDll模式都会用到这个目录下的dll, 故需要还原)
        /// </summary>
        static void OnExitingPlayMode()
        {
            foreach (string dll in DllNames)
            {
                string dllDisableFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.dll.DISABLE";
                if (File.Exists(dllDisableFile))
                {
                    string dllFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.dll";
                    if (File.Exists(dllFile))
                    {
                        File.Delete(dllDisableFile);
                    }
                    else
                    {
                        File.Move(dllDisableFile, dllFile);
                    }
                    Debug.Log($"[AssemblyTool] Enable Library dll {dll}.dll");
                }

                string pdbDisableFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.pdb.DISABLE";
                if (File.Exists(pdbDisableFile))
                {
                    string pdbFile = $"{Application.dataPath}/../Library/ScriptAssemblies/{dll}.pdb";
                    if (File.Exists(pdbFile))
                    {
                        File.Delete(pdbDisableFile);
                    }
                    else
                    {
                        File.Move(pdbDisableFile, pdbFile);
                    }
                    Debug.Log($"[AssemblyTool] Enable Library dll {dll}.pdb");
                }
            }
        }

        #endregion

        /// <summary>
        /// 菜单和快捷键编译按钮
        /// </summary>
        [MenuItem("Tools/Bliss/Compile _F6", false, 1)]
        public static void MenuItemOfCompile()
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