using System;
using System.Collections.Generic;
using UnityEngine;
using Bliss;

namespace Bliss
{
    public class Init : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log($"{typeof(List<int>)}");
            DontDestroyOnLoad(gameObject);
            
            Log.Logger = new UnityLogger();
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };
            
            World.Instance.AddSingleton<RootManager>();
        }

        public void Update()
        {
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
    }
}