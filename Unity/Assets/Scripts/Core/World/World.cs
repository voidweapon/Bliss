using System;
using System.Collections.Generic;
using Bliss.Id;
using Bliss;

namespace Bliss
{
    public class World : IDisposable
    {
        private static World instance;

        public static World Instance
        {
            get
            {
                return instance ??= new World();
            }
        }
        
        private Dictionary<Type, ASingleton> _singletons = new Dictionary<Type, ASingleton>();
        
        public void Dispose()
        {
            instance = null;

            foreach (var kv in _singletons)
            {
                kv.Value.Dispose();
            }
            _singletons.Clear();
        }


        public void AddSingleton<T>() where T : ASingleton, new()
        {
            var singleton = new T();
            AddSingleton(singleton);
        }

        public void AddSingleton(ASingleton singleton)
        {
            _singletons.Add(singleton.GetType(), singleton);
            singleton.Register();
        }


        public void Update()
        {
            TimeInfo.Instance.Update();
            IdGenerator.Instance.FrameTime = TimeInfo.Instance.FrameTime;
            RootManager.Instance.Update();
        }

        public void LateUpdate()
        {
            RootManager.Instance.LateUpdate();

        }
    }
}