using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>Initialization status for a Root using the Server readiness gate.</summary>
    public enum ERootInitializationState
    {
        /// <summary>The Root does not use Server initialization gating.</summary>
        Unmanaged,
        /// <summary>The Root is initializing and ordinary updates are restricted.</summary>
        Initializing,
        /// <summary>The Root completed its initialization.</summary>
        Ready,
        /// <summary>The Root initialization failed and remains closed.</summary>
        Failed,
    }

    /// <summary>An immutable, atomically published view of a Root's initialization admission.</summary>
    public sealed class RootInitializationSnapshot
    {
        internal RootInitializationSnapshot(ERootInitializationState state, RootInitializationMessagePolicy messagePolicy)
        {
            State = state;
            MessagePolicy = messagePolicy;
        }

        /// <summary>The Root state when this snapshot was published.</summary>
        public ERootInitializationState State { get; }

        /// <summary>The initialization message policy associated with this state.</summary>
        public RootInitializationMessagePolicy MessagePolicy { get; }
    }

    public class Root : IDisposable
    {
        public int Id { get; private set; }
        public int Zone { get; private set; }

        public int Process => Options.Instance.Process;
        
        public RootAddress RootAddress => new RootAddress(Process, Id);
        
        
        public LogicScene Scene { get; private set; }
        
        public bool IsDisposed { get; private set; }

        private volatile RootInitializationSnapshot _initializationSnapshot =
            new RootInitializationSnapshot(ERootInitializationState.Unmanaged, RootInitializationMessagePolicy.Empty);

        /// <summary>The Root's Server initialization state, or Unmanaged for ungated Roots.</summary>
        public ERootInitializationState InitializationState
        {
            get => _initializationSnapshot.State;
            private set => _initializationSnapshot = new RootInitializationSnapshot(value, _initializationSnapshot.MessagePolicy);
        }

        /// <summary>The current atomic initialization state and message policy.</summary>
        public RootInitializationSnapshot InitializationSnapshot => _initializationSnapshot;

        /// <summary>The immutable message policy snapshot for this Root's initialization.</summary>
        public RootInitializationMessagePolicy InitializationMessagePolicy => _initializationSnapshot.MessagePolicy;

        internal void SetInitializationMessagePolicy(RootInitializationMessagePolicy policy)
        {
            _initializationSnapshot = new RootInitializationSnapshot(_initializationSnapshot.State, policy);
        }

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
            Scene = new LogicScene(this, id, ActorAddress.CFirstInstanceId, sceneType, name);
            RootNode = this;
            EntitySystem = new EntitySystem(this);
        }

        /// <summary>Begins fail-closed initialization before registering routes or listeners.</summary>
        /// <exception cref="InvalidOperationException">The Root is disposed, already initializing, or already has a gate.</exception>
        public void BeginInitialization()
        {
            if (IsDisposed || InitializationState != ERootInitializationState.Unmanaged)
            {
                throw new InvalidOperationException("The Root cannot begin initialization.");
            }

            if (Scene.GetComponent<RootInitializationGateComponent>() != null)
            {
                throw new InvalidOperationException("The Root already has an initialization gate.");
            }

            InitializationState = ERootInitializationState.Initializing;
            ProcessMessageManager.Instance?.trackRoot(this);
            Scene.AddComponent<RootInitializationGateComponent>();
        }

        internal void CompleteInitialization()
        {
            if (InitializationState != ERootInitializationState.Initializing)
            {
                return;
            }

            if (IsDisposed || Scene.GetComponent<RootInitializationGateComponent>() == null)
            {
                throw new InvalidOperationException("The initializing Root has no live initialization gate.");
            }

            Scene.RemoveComponent<RootInitializationGateComponent>();
            InitializationState = ERootInitializationState.Ready;
        }

        internal void FailInitialization()
        {
            if (InitializationState == ERootInitializationState.Initializing)
            {
                InitializationState = ERootInitializationState.Failed;
                ProcessMessageManager.Instance?.rejectFailedMessages(this);
            }
        }

        internal RootInitializationGateComponent GetInitializationGate()
        {
            RootInitializationGateComponent gate = Scene.GetComponent<RootInitializationGateComponent>();
            if (InitializationState == ERootInitializationState.Initializing && gate == null)
            {
                throw new InvalidOperationException("The initializing Root has no initialization gate.");
            }

            return gate;
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
                
                try
                {
                    updateAction(item);
                }
                finally
                {
                    // Preserve child scheduling when an update reports a failure to the parent.
                    _updateQueue.Enqueue(rootId);
                }
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
            ProcessMessageManager.Instance?.untrackRoot(this);
            
            Scene.Dispose();

            foreach (var child in _children.Values)
            {
                child.Dispose();
            }
            _children.Clear();
        }
    }
}