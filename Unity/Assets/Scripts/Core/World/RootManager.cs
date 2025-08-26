using System.Collections.Generic;

namespace Bliss
{
    public class RootManager : Singleton<RootManager>
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
        
        internal void Update()
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
                
                item.Update();
                _updateQueue.Enqueue(rootId);
            }
        }

        internal void LateUpdate()
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
                
                item.LateUpdate();
                _updateQueue.Enqueue(rootId);
            }
            
            while (_addQueue.Count > 0)
            {
                var rootId =  _addQueue.Dequeue();
                _updateQueue.Enqueue(rootId);
            }
        }


        public int Create(int zone, ELogicSceneType sceneType, string name)
        {
            int id = ++idGenerator;
            var root =  new Root(id, zone, sceneType, name);
            _children.Add(id, root);
            _addQueue.Enqueue(id);
            
            return id;
        }

        public void Remove(int id)
        {
            
        }

        internal Root Get(int id)
        {
            _children.TryGetValue(id, out var root);
            return root;
        }
    }
}