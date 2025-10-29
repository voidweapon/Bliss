using System;
using System.Collections.Generic;
using Bliss.Id;
using Bliss;
using Bliss.Pool;

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
            
            ObjectPool.Instance.Dispose();
        }
        
        public T AddSingletonNoInit<T>() where T : ASingleton, new()
        {
            var singleton = new T();
            AddSingleton(singleton);
            return singleton;
        }
        
        public T AddSingleton<T>() where T : ASingleton, ISingletonInit, new()
        {
            var singleton = new T();
            singleton.Init();
            AddSingleton(singleton);
            return singleton;
        }
        
        public T AddSingleton<T, TParam>(TParam param) where T : ASingleton, ISingletonInit<TParam>, new()
        {
            var singleton = new T();
            singleton.Init(param);
            AddSingleton(singleton);
            return singleton;
        }

        public void AddSingleton(ASingleton singleton)
        {
            _singletons[singleton.GetType()] = singleton;
            singleton.Register();
        }

        public void EarlyUpdate()
        {
            RootManager.Instance.EarlyUpdate();
        }

        public void Update()
        {
            TimeInfo.Instance.Update();
            RootManager.Instance.Update();
        }

        public void LateUpdate()
        {
            RootManager.Instance.LateUpdate();
        }
    }
}