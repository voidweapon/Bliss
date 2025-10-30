using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Bliss.Base;
using Bliss.Core;
using ET;
using YooAsset;

namespace Bliss
{
    public class Init : MonoBehaviour
    {
        private void Awake()
        {
            InitFramework().Coroutine();
        }

        public void Update()
        {
            World.Instance.EarlyUpdate();
            World.Instance.Update();
        }
        
        public void FixedUpdate()
        {
            
        }

        public void LateUpdate()
        {
            World.Instance.LateUpdate();
        }


        public void OnApplicationQuit()
        {
            World.Instance.Dispose();
        }

        private async ETTask InitFramework()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            
            DontDestroyOnLoad(gameObject);
            
            Log.Logger = new UnityLogger();
            ETTask.ExceptionHandler += Log.Error;
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };
            
            World.Instance.AddSingleton<RootManager>();

            await World.Instance.AddSingleton<SimpleResourcesComponent, EPlayMode>(EPlayMode.EditorSimulateMode).CreatePackageAsync("DefaultPackage", true);
            
            World.Instance.AddSingleton<CodeLoader>();
            await CodeLoader.Instance.DownloadDllsAsync();
            
            CodeLoader.Instance.Start();
        }
    }
}