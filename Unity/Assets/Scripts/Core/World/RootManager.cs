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


        /// <summary>Creates and initializes a Root without a Server initialization gate.</summary>
        /// <param name="id">The Root identifier.</param>
        /// <param name="zone">The Root zone.</param>
        /// <param name="sceneType">The initialization handler's scene type.</param>
        /// <param name="name">The Root name.</param>
        /// <returns>The identifier after initialization succeeds.</returns>
        public ETTask<int> Create(int id, int zone, ELogicSceneType sceneType, string name)
        {
            return Create(id, zone, sceneType, name, false);
        }

        /// <summary>Creates and initializes a Root, optionally gating its Server initialization.</summary>
        /// <param name="id">The Root identifier.</param>
        /// <param name="zone">The Root zone.</param>
        /// <param name="sceneType">The initialization handler's scene type.</param>
        /// <param name="name">The Root name.</param>
        /// <param name="useInitializationGate">Whether to mount the initialization gate before invoking the handler.</param>
        /// <returns>The identifier after initialization succeeds.</returns>
        public async ETTask<int> Create(int id, int zone, ELogicSceneType sceneType, string name,
            bool useInitializationGate)
        {
            var root =  new Root(id, zone, sceneType, name);
            _children.Add(id, root);
            _addQueue.Enqueue(id);
            
            try
            {
                if (useInitializationGate)
                {
                    root.BeginInitialization();
                }

                Log.Info($"[{name}][{sceneType}]:Create Scene Id={id} Zone={zone}");
                await EventSystem.Instance.Invoke<Invoke_RootInit, ETTask>((long)sceneType, new Invoke_RootInit()
                {
                    Root = root
                });
                root.CompleteInitialization();
            }
            catch
            {
                root.FailInitialization();
                throw;
            }
            
            return id;
        }
        
        /// <summary>Creates and initializes a Root with a generated identifier and no Server gate.</summary>
        /// <param name="zone">The Root zone.</param>
        /// <param name="sceneType">The initialization handler's scene type.</param>
        /// <param name="name">The Root name.</param>
        /// <returns>The identifier after initialization succeeds.</returns>
        public ETTask<int> Create(int zone, ELogicSceneType sceneType, string name)
        {
            return Create(zone, sceneType, name, false);
        }

        /// <summary>Creates and initializes a Root with a generated identifier.</summary>
        /// <param name="zone">The Root zone.</param>
        /// <param name="sceneType">The initialization handler's scene type.</param>
        /// <param name="name">The Root name.</param>
        /// <param name="useInitializationGate">Whether to mount the initialization gate before invoking the handler.</param>
        /// <returns>The identifier after initialization succeeds.</returns>
        public async ETTask<int> Create(int zone, ELogicSceneType sceneType, string name,
            bool useInitializationGate)
        {
            int id = ++idGenerator;
  
            return await Create(id, zone, sceneType, name, useInitializationGate);
        }

        public void Remove(int id)
        {
            if (_children.Remove(id, out var root))
            {
                root.Dispose();
            }
        }

        /// <summary>Finds a registered Root by its identifier.</summary>
        /// <param name="id">The Root identifier.</param>
        /// <returns>The registered Root, or null if it is absent.</returns>
        public Root Get(int id)
        {
            _children.TryGetValue(id, out var root);
            return root;
        }
    }
}