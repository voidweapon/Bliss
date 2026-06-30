using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bliss.Base;
using Bliss.Core;
using ET;
using HybridCLR;
using UnityEngine;

namespace Bliss
{
    public class CodeLoader : Singleton<CodeLoader>, ISingletonInit
    {
        private List<Assembly> _modelAssemblies = new List<Assembly>();
        private Assembly _modelAssembly;
        private Dictionary<string, TextAsset> _dlls;
        private Dictionary<string, TextAsset> _aotDlls;
        
        private const string _modelAssemblyName = "Bliss.Model";
        private const string _entryClassName = "Bliss.Entry";
        private const string _entryMethodName = "Start";
        

        private readonly HashSet<string> _modelDlls = new HashSet<string>
        {
            _modelAssemblyName,
            "Bliss.ViewModel",
        };
        
        private readonly HashSet<string> _logiclDlls = new HashSet<string>
        {
            "Bliss.Logic",
            "Bliss.ViewLogic",
        };
        
        public void Init()
        {
            
        }

        public async ETTask DownloadDllsAsync()
        {
            if(GlobalDefine.IsEditor && !GlobalDefine.BootSetting.TestABInEditor) return;
            //LoadAllAssets 只需要提供包中任意资源路径
            _dlls = await SimpleResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"{GlobalDefine.CodeDir}/Bliss.Model.dll.bytes");
            _aotDlls = await SimpleResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"{GlobalDefine.AOTCodeDir}/mscorlib.dll.bytes");
        }

        public void Start()
        {
            LoadAOT();
            LoadModels();
            List<Assembly> logicAssemblies = LoadLogics();
            
            List<Assembly> assemblies = new List<Assembly>();
            
            assemblies.Add(typeof(CodeLoader).Assembly);
            assemblies.Add(typeof(BaseAssembly).Assembly);
            assemblies.Add(typeof(CoreAssembly).Assembly);
            assemblies.AddRange(_modelAssemblies);
            assemblies.AddRange(logicAssemblies);
            
            World.Instance.AddSingleton<TypeManager, List<Assembly>>(assemblies);
            
            StaticMethod entryStartMethod = new StaticMethod(_modelAssembly, _entryClassName, _entryMethodName);
            entryStartMethod.Run();
        }


        private void  LoadAOT()
        {
            if ((!GlobalDefine.IsEditor && GlobalDefine.EnableIL2CPP) 
                || (GlobalDefine.IsEditor && GlobalDefine.BootSetting.TestDLLInEditor))
            {
                foreach (var kv in _aotDlls)
                {
                    TextAsset textAsset = kv.Value;
                    RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HomologousImageMode.SuperSet);
                }
                Log.Info($"[HybridCLR]AOT assemblies loaded successfully.");
            }
        }
        
        private void LoadModels()
        {
            _modelAssemblies.Clear();
            if (GlobalDefine.IsEditor)
            {
                if (GlobalDefine.BootSetting.TestDLLInEditor)
                {
                    foreach (string dllName in _modelDlls)
                    {
                        byte[] dll;
                        byte[] pdb;
                        if (GlobalDefine.BootSetting.TestABInEditor)
                        {
                            dll = _dlls[$"{dllName}.dll"].bytes;
                            pdb = _dlls[$"{dllName}.pdb"].bytes;
                            Log.Info($"[HybridCLR]Load {dllName}.dll.bytes and {dllName}.pdb.bytes from AB In Editor ");
                        }
                        else
                        {
                            dll = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.dll.bytes"));
                            pdb = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.pdb.bytes"));
                        }
                        
                        var assembly = Assembly.Load(dll, pdb);
                        _modelAssemblies.Add(assembly);
                    
                        if (_modelAssemblyName == dllName)
                        {
                            _modelAssembly = assembly;
                        }
                        Log.Info($"[HybridCLR]Load assembly {assembly.FullName} from {assembly.Location}");
                    }
                    Log.Info($"[HybridCLR]In Editor Model assemblies loaded successfully.");
                }
                else
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly assembly in assemblies)
                    {
                        string name = assembly.GetName().Name;
                        if (_modelDlls.Contains(name))
                        {
                            if (_modelAssemblyName == name)
                            {
                                _modelAssembly = assembly;
                            }
                            _modelAssemblies.Add(assembly);
                        }
                    }
                }
            }
            else
            {
                foreach (string dllName in _modelDlls)
                {
                    var dll = _dlls[$"{dllName}.dll"].bytes;
                    var pdb = _dlls[$"{dllName}.pdb"].bytes;
                    
                    var assembly = Assembly.Load(dll, pdb);
                    _modelAssemblies.Add(assembly);
                    
                    if (_modelAssemblyName == dllName)
                    {
                        _modelAssembly = assembly;
                    }
                }
                Log.Info($"[HybridCLR]Model assemblies loaded successfully.");
            }
        }
        
        private List<Assembly> LoadLogics()
        {
            var logics = new List<Assembly>();
            if (GlobalDefine.IsEditor)
            {
                if (GlobalDefine.BootSetting.TestDLLInEditor)
                {
                    foreach (string dllName in _logiclDlls)
                    {
                        byte[] dll;
                        byte[] pdb;
                        if (GlobalDefine.BootSetting.TestABInEditor)
                        {
                            dll = _dlls[$"{dllName}.dll"].bytes;
                            pdb = _dlls[$"{dllName}.pdb"].bytes;
                            Log.Info($"[HybridCLR]Load {dllName}.dll.bytes and {dllName}.pdb.bytes from AB In Editor ");

                        }
                        else
                        {
                            dll = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.dll.bytes"));
                            pdb = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.pdb.bytes"));
                        }
                        
                        var assembly = Assembly.Load(dll, pdb);
                        logics.Add(assembly);
                        Log.Info($"[HybridCLR]Load assembly {assembly.FullName} from {assembly.Location}");
                    }

                    Log.Info($"[HybridCLR]Logic assemblies loaded successfully In Editor.");
                }
                else
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly assembly in assemblies)
                    {
                        string name = assembly.GetName().Name;
                        if (_logiclDlls.Contains(name))
                        {
                            logics.Add(assembly);
                        }
                    }
                }
            }
            else
            {
                foreach (string dllName in _logiclDlls)
                {
                    var dll = _dlls[$"{dllName}.dll"].bytes;
                    var pdb = _dlls[$"{dllName}.pdb"].bytes;
                    logics.Add(Assembly.Load(dll, pdb));
                }
                Log.Info($"[HybridCLR]Logic assemblies loaded successfully.");
            }
            
            return logics;
        }
        
        /// <summary>
        /// 重新加载热更程序集
        /// </summary>
        public void Reload()
        {
            List<Assembly> logicAssemblies = LoadLogics();
            List<Assembly> assemblies = new List<Assembly>();
            
            assemblies.Add(typeof(CodeLoader).Assembly);
            assemblies.Add(typeof(BaseAssembly).Assembly);
            assemblies.Add(typeof(CoreAssembly).Assembly);
            assemblies.AddRange(_modelAssemblies);
            assemblies.AddRange(logicAssemblies);

            World.Instance.AddSingleton<TypeManager, List<Assembly>>(assemblies);
            TypeManager.Instance.CreateAutoSingletonManager();
        }
    }
}