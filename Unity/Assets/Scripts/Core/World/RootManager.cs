using System;
using System.Collections.Generic;
using ET;

namespace Bliss
{
    public class RootManager : Singleton<RootManager>, ISingletonInit
    {
        /// <summary>
        /// Root ID -> Child Root
        /// </summary>
        private Dictionary<int, Root> _children = new Dictionary<int, Root>();
        
        /// <summary>
        /// Queued Root ID For Update
        /// </summary>
        private Queue<int> _updateQueue = new Queue<int>();
        
        /// <summary>
        /// Queued Root ID for add to child
        /// </summary>
        private Queue<int> _addQueue = new Queue<int>();
        
        private int idGenerator = 10000000;
        
        public void Init()
        {
            
        }

        protected  override void Destroy()
        {
            foreach (var kv in _children)
            {
                kv.Value.Dispose();
            }
            _children.Clear();
        }
        
        internal void EarlyUpdate()
        {
            UpdateTiming(EarlyUpdateAction);

            void EarlyUpdateAction(Root root)
            {
                root.EarlyUpdate();
            }
        }
        
        internal void Update()
        {
            UpdateTiming(UpdateAction);

            void UpdateAction(Root root)
            {
                root.Update();
            }
        }

        internal void LateUpdate()
        {
            UpdateTiming(LateUpdateAction);

            void LateUpdateAction(Root root)
            {
                root.LateUpdate();
            }
        }

        private void UpdateTiming(Action<Root> updateAction)
        {
            int count = _updateQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var rootId =  _updateQueue.Dequeue();
                var item  = Get(rootId);
                
                if (item == null)
                {
                    continue;
                }

                if (item.IsDisposed)
                {
                    continue;
                }
                
                try
                {
                    updateAction(item);
                }
                finally
                {
                    // A propagated update failure must not silently remove a live Root from later frames.
                    _updateQueue.Enqueue(rootId);
                }
            }
            
            while (_addQueue.Count > 0)
            {
                var rootId =  _addQueue.Dequeue();
                _updateQueue.Enqueue(rootId);
            }
        }


        public async ETTask<int> Create(int id, int zone, ELogicSceneType sceneType, string name)
        {
            var root =  new Root(id, zone, sceneType, name);
            _children.Add(id, root);
            _addQueue.Enqueue(id);
            
            Log.Info($"[{name}][{sceneType}]:Create Scene Id={id} Zone={zone}");
            await EventSystem.Instance.Invoke<Invoke_RootInit, ETTask>((long)sceneType, new Invoke_RootInit()
            {
                Root = root
            });
            
            return id;
        }
        
        public async ETTask<int> Create(int zone, ELogicSceneType sceneType, string name)
        {
            int id = ++idGenerator;
  
            return await Create(id, zone, sceneType, name);
        }

        public void Remove(int id)
        {
            if (_children.Remove(id, out var root))
            {
                root.Dispose();
            }
        }

        internal Root Get(int id)
        {
            _children.TryGetValue(id, out var root);
            return root;
        }
    }
}