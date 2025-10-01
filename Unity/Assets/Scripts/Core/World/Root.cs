using System;
using System.Collections.Generic;

namespace Bliss
{
    public class Root : IDisposable
    {
        public int Id { get; private set; }
        public int Zone { get; private set; }
        public LogicScene Scene { get; private set; }
        
        public bool IsDisposed { get; private set; }

        public Root RootNode { get; private set; }

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
        private Queue<int> _removeQueue = new Queue<int>();
        
        public EntitySystem EntitySystem { get; private set; }
        

        internal Root(int id, int zone, ELogicSceneType sceneType, string name)
        {
            Id = id;
            Zone = zone;
            Scene = new LogicScene(this, id, 1, sceneType, name);
            RootNode = this;
            EntitySystem = new EntitySystem();
        }

        public Root CreateChild(int id, ELogicSceneType sceneType, string name)
        {
            var childRoot =  new Root(id, Zone, sceneType, name);
            childRoot.RootNode = this.RootNode;
            _children.Add(id, childRoot);
            _addQueue.Enqueue(id);
            return childRoot;
        }

        public Root Get(int id)
        {
            _children.TryGetValue(id, out var child);
            return child;
        }

        public void RemoveLate(int id)
        {
            _removeQueue.Enqueue(id);
        }

        internal void EarlyUpdate()
        {
            EntitySystem.EarlyUpdate();
            
            UpdateTiming(TimingEarly);

            void TimingEarly(Root root)
            {
                root.EarlyUpdate();
            }
        }
        
        internal void Update()
        {
            EntitySystem.Update();
            
            UpdateTiming(TimingUpdate);

            void TimingUpdate(Root root)
            {
                root.Update();
            }
        }

        internal void LateUpdate()
        {
            EntitySystem.LateUpdate();
            
            UpdateTiming(TimingLate);

            void TimingLate(Root root)
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
                
                updateAction(item);
                _updateQueue.Enqueue(rootId);
            }
            
            while (_removeQueue.Count > 0)
            {
                var rootId =  _removeQueue.Dequeue();
                if (_children.Remove(rootId, out var child))
                {
                    child.Dispose();
                }
            }

            while (_addQueue.Count > 0)
            {
                var rootId =  _addQueue.Dequeue();
                _updateQueue.Enqueue(rootId);
            }
        }
        
        
        
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            IsDisposed = true;
            
            Scene.Dispose();

            foreach (var child in _children.Values)
            {
                child.Dispose();
            }
            _children.Clear();
        }
    }
}