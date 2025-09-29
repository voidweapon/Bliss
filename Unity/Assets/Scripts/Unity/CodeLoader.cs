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

        private bool _useDLLInEditor = false;

        
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
            if(GlobalDefine.IsEditor) return;
            
            _dlls = await SimpleResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"{GlobalDefine.CodeDir}/Unity.Model.dll.bytes");
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
            if (!GlobalDefine.IsEditor && GlobalDefine.EnableIL2CPP)
            {
                foreach (var kv in _aotDlls)
                {
                    TextAsset textAsset = kv.Value;
                    RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HomologousImageMode.SuperSet);
                }
            }
        }
        
        private void LoadModels()
        {
            _modelAssemblies.Clear();
            if (GlobalDefine.IsEditor)
            {
                if (_useDLLInEditor)
                {
                    foreach (string dllName in _modelDlls)
                    {
                        var dll = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.dll.bytes"));
                        var pdb = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.pdb.bytes"));
                        
                        var assembly = Assembly.Load(dll, pdb);
                        _modelAssemblies.Add(assembly);
                    
                        if (_modelAssemblyName == dllName)
                        {
                            _modelAssembly = assembly;
                        }
                    }
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
            }
        }
        
        private List<Assembly> LoadLogics()
        {
            var logics = new List<Assembly>();
            if (GlobalDefine.IsEditor)
            {
                if (_useDLLInEditor)
                {
                    foreach (string dllName in _logiclDlls)
                    {
                        var dll = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.dll.bytes"));
                        var pdb = File.ReadAllBytes(Path.Combine(GlobalDefine.CodeDir, $"{dllName}.pdb.bytes"));
                        
                        var assembly = Assembly.Load(dll, pdb);
                        _modelAssemblies.Add(assembly);
                    
                        if (_modelAssemblyName == dllName)
                        {
                            _modelAssembly = assembly;
                        }
                    }
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