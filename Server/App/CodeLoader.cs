using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Bliss.Base;
using Bliss.Core;

namespace Bliss;

public class CodeLoader : Singleton<CodeLoader>, ISingletonInit
{
    private AssemblyLoadContext _assemblyLoadContext;
    private Assembly _modelAssembly;
    
    public void Init()
    {
        Assembly modelAssembly = LoadModel();
        Assembly logicAssembly = LoadLogic();

        List<Assembly> assemblies = new List<Assembly>();
        assemblies.Add(typeof(CodeLoader).Assembly);
        assemblies.Add(typeof(BaseAssembly).Assembly);
        assemblies.Add(typeof(CoreAssembly).Assembly);
        assemblies.Add(modelAssembly);
        assemblies.Add(logicAssembly);
        World.Instance.AddSingleton<TypeManager, List<Assembly>>(assemblies);
        
        Entry.Start();
    }

    private Assembly LoadModel()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies)
        {
            if (assembly.GetName().Name == "Model")
            {
                _modelAssembly = assembly;
                break;
            }
        }
        return _modelAssembly;
    }

    private Assembly LoadLogic()
    {
        _assemblyLoadContext?.Unload();
        GC.Collect();
        _assemblyLoadContext = new AssemblyLoadContext("Logic", true);
        byte[] dllBytes = File.ReadAllBytes("./Logic.dll");
        byte[] pdbBytes = File.ReadAllBytes("./Logic.pdb");
        Assembly hotfixAssembly = _assemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
        return hotfixAssembly;
    }
}