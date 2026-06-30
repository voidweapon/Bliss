using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using Bliss.Base;
using Bliss.BootProcess;
using Bliss.Core;
using Bliss.SimpleUI;
using ET;
using Unity.UI;
using YooAsset;

namespace Bliss
{
    public class Init : MonoBehaviour
    {
        public BootSetting BootSetting;
        
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            
            Options options = new Options()
            {
                AppName = "Bliss"
            };
            World.Instance.AddSingleton(options);
            World.Instance.AddSingleton<BootProcesser, BootSetting>(BootSetting);
            BootProcesser.Instance.InitFramework().Coroutine();
        }

        private void Update()
        {
            World.Instance.EarlyUpdate();
            World.Instance.Update();
        }
        
        private void FixedUpdate()
        {
            
        }

        private void LateUpdate()
        {
            World.Instance.LateUpdate();
        }


        private void OnGUI()
        {
            World.Instance.OnGUI();
        }


        private void OnApplicationQuit()
        {
            World.Instance.Dispose();
        }
    }
}