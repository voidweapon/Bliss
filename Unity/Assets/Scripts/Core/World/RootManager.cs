using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ET;

namespace Bliss
{
    public class RootManager : Singleton<RootManager>, ISingletonInit
    {
        private const int ROOT_INITIALIZATION_TIMEOUT_SECONDS = 60;

        /// <summary>The absolute maximum time allowed for a gated Root to initialize.</summary>
        public const int ROOT_INITIALIZATION_TIMEOUT_MILLISECONDS =
            ROOT_INITIALIZATION_TIMEOUT_SECONDS * 1000;

        /// <summary>The owner-thread grace period after cooperative fatal cancellation is requested.</summary>
        public const int FATAL_CANCELLATION_GRACE_PERIOD_MILLISECONDS = 1000;

        /// <summary>The absolute process-level deadline for fatal cleanup.</summary>
        public const int FATAL_CLEANUP_DEADLINE_MILLISECONDS = 5000;

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

        // Keys are Roots still initializing; values own their cancellation tokens and timeout timers.
        private readonly Dictionary<Root, RootInitializationOperation> _activeInitializations =
            new Dictionary<Root, RootInitializationOperation>();

        private Action<Exception> _fatalFailureHandler;
        private Action _fatalCleanupHandler;
        private Exception _fatalException;
        private readonly object _fatalStateLock = new object();
        private int _isStopping;
        private int _fatalCancellationRequested;
        private long _fatalCancellationRequestedTimestamp;
        private int _fatalCancellationGraceWarningLogged;
        private int _fatalCleanupCompleted;
        private int _ownerThreadId;
        
        private int idGenerator = 10000000;
        
        /// <summary>Captures the thread that owns Root updates and fatal cleanup.</summary>
        public void Init()
        {
            _ownerThreadId = Environment.CurrentManagedThreadId;
        }

        /// <summary>Whether the manager has irreversibly stopped normal Root work.</summary>
        public bool IsStopping => Volatile.Read(ref _isStopping) != 0;

        /// <summary>The first fatal failure reported by this manager.</summary>
        public Exception FatalException => Volatile.Read(ref _fatalException);

        /// <summary>Whether owner-thread fatal cleanup completed.</summary>
        public bool FatalCleanupCompleted => Volatile.Read(ref _fatalCleanupCompleted) != 0;

        /// <summary>Runs a short publication action atomically with respect to fatal state transitions.</summary>
        /// <param name="action">The action to run while the manager is not stopping.</param>
        /// <returns>True when the action ran; otherwise, the manager was already stopping.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is null.</exception>
        public bool TryRunIfNotStopping(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            lock (_fatalStateLock)
            {
                if (IsStopping)
                {
                    return false;
                }

                action();
                return true;
            }
        }

        /// <summary>Whether cooperative cancellation has exceeded its one-second grace period.</summary>
        public bool FatalCancellationGraceExpired
        {
            get
            {
                long timestamp = Volatile.Read(ref _fatalCancellationRequestedTimestamp);
                return timestamp != 0 && Stopwatch.GetElapsedTime(timestamp)
                    >= TimeSpan.FromMilliseconds(FATAL_CANCELLATION_GRACE_PERIOD_MILLISECONDS);
            }
        }

        /// <summary>Configures process-level fatal reporting and cleanup completion callbacks.</summary>
        /// <param name="fatalFailureHandler">Called once with the first fatal exception.</param>
        /// <param name="fatalCleanupHandler">Called after owner-thread cleanup has completed.</param>
        /// <exception cref="ArgumentNullException">A callback is null.</exception>
        /// <exception cref="InvalidOperationException">Roots have already been created or a lifecycle is configured.</exception>
        public void ConfigureFatalLifecycle(Action<Exception> fatalFailureHandler, Action fatalCleanupHandler)
        {
            ArgumentNullException.ThrowIfNull(fatalFailureHandler);
            ArgumentNullException.ThrowIfNull(fatalCleanupHandler);
            if (_children.Count != 0 || _fatalFailureHandler != null)
            {
                throw new InvalidOperationException("Fatal lifecycle callbacks must be configured before Root creation.");
            }

            _fatalFailureHandler = fatalFailureHandler;
            _fatalCleanupHandler = fatalCleanupHandler;
        }

        /// <summary>Requests irreversible fatal stopping while preserving the first failure.</summary>
        /// <param name="exception">The fatal cause.</param>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
        public void ReportFatal(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            lock (_fatalStateLock)
            {
                if (FatalException != null)
                {
                    return;
                }

                Volatile.Write(ref _fatalException, exception);
                ProcessMessageManager.Instance?.StopAcceptingMessages();
                Volatile.Write(ref _isStopping, 1);
            }

            _fatalFailureHandler?.Invoke(exception);
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
            if (completeFatalCleanupIfStopping())
            {
                return;
            }

            UpdateTiming(EarlyUpdateAction);

            void EarlyUpdateAction(Root root)
            {
                root.EarlyUpdate();
            }
        }
        
        internal void Update()
        {
            if (completeFatalCleanupIfStopping())
            {
                return;
            }

            UpdateTiming(UpdateAction);

            void UpdateAction(Root root)
            {
                root.Update();
            }
        }

        internal void LateUpdate()
        {
            if (completeFatalCleanupIfStopping())
            {
                return;
            }

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
                if (IsStopping)
                {
                    break;
                }

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
                catch (Exception exception)
                {
                    if (item.InitializationState != ERootInitializationState.Unmanaged)
                    {
                        ReportFatal(exception);
                    }

                    throw;
                }
                finally
                {
                    // A propagated update failure must not silently remove a live Root from later frames.
                    _updateQueue.Enqueue(rootId);
                }
            }

            if (completeFatalCleanupIfStopping())
            {
                return;
            }
            
            while (_addQueue.Count > 0)
            {
                var rootId =  _addQueue.Dequeue();
                _updateQueue.Enqueue(rootId);
            }
        }

        private bool completeFatalCleanupIfStopping()
        {
            if (!IsStopping)
            {
                return false;
            }

            CompleteFatalCleanupOnOwnerThread();
            return true;
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
        /// <exception cref="TaskAbortedException">The Server is stopping or initialization was canceled.</exception>
        public async ETTask<int> Create(int id, int zone, ELogicSceneType sceneType, string name,
            bool useInitializationGate)
        {
            if (IsStopping)
            {
                throw new TaskAbortedException("Root creation was rejected because the Server is stopping.");
            }

            var root =  new Root(id, zone, sceneType, name);
            _children.Add(id, root);
            _addQueue.Enqueue(id);
            RootInitializationOperation initialization = null;
            if (useInitializationGate)
            {
                initialization = new RootInitializationOperation(this, root);
                _activeInitializations.Add(root, initialization);
            }
            
            try
            {
                if (useInitializationGate)
                {
                    root.BeginInitialization();
                }

                Log.Info($"[{name}][{sceneType}]:Create Scene Id={id} Zone={zone}");
                await EventSystem.Instance.Invoke<Invoke_RootInit, ETTask>((long)sceneType, new Invoke_RootInit()
                {
                    Root = root,
                    CancellationToken = initialization?.CancellationToken,
                });
                if (initialization?.CancellationToken.IsCancel() == true)
                {
                    throw new TaskAbortedException("Root initialization was canceled.", initialization.CancellationToken);
                }

                root.CompleteInitialization();
            }
            catch (Exception exception)
            {
                root.FailInitialization();
                if (useInitializationGate)
                {
                    ReportFatal(exception);
                }

                throw;
            }
            finally
            {
                if (initialization != null)
                {
                    initialization.Complete();
                    _activeInitializations.Remove(root);
                }
            }
            
            return id;
        }

        private void CompleteFatalCleanupOnOwnerThread()
        {
            if (_ownerThreadId != Environment.CurrentManagedThreadId)
            {
                Log.Error("Fatal Root cleanup was requested from a non-owner thread.");
                return;
            }

            if (Interlocked.Exchange(ref _fatalCancellationRequested, 1) == 0)
            {
                Volatile.Write(ref _fatalCancellationRequestedTimestamp, Stopwatch.GetTimestamp());
                foreach (RootInitializationOperation initialization in _activeInitializations.Values.ToArray())
                {
                    initialization.CancellationToken.Cancel();
                }
            }

            if (_activeInitializations.Count != 0)
            {
                if (FatalCancellationGraceExpired && Interlocked.Exchange(ref _fatalCancellationGraceWarningLogged, 1) == 0)
                {
                    foreach (RootInitializationOperation initialization in _activeInitializations.Values)
                    {
                        Log.Warning(
                            $"Root {initialization.Root.RootAddress} did not stop within the fatal cancellation grace period; resources remain live until process termination.");
                    }
                }

                return;
            }

            if (Interlocked.CompareExchange(ref _fatalCleanupCompleted, 1, 0) != 0)
            {
                return;
            }

            foreach (Root root in new List<Root>(_children.Values))
            {
                try
                {
                    Remove(root.Id);
                }
                catch (Exception cleanupException)
                {
                    Log.Error(new InvalidOperationException(
                        $"Fatal cleanup for Root {root.RootAddress} failed while handling " +
                        $"{FatalException.GetType().FullName}: {FatalException.Message}.",
                        cleanupException));
                }
            }

            _updateQueue.Clear();
            _addQueue.Clear();
            _fatalCleanupHandler?.Invoke();
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

        private sealed class RootInitializationOperation
        {
            private readonly RootManager _manager;
            private readonly Root _root;
            private int _isComplete;
            // This timer reports fatal timeout independently of Root updates, which initialization may stall.
            private readonly System.Threading.Timer _timeoutTimer;

            public RootInitializationOperation(RootManager manager, Root root)
            {
                _manager = manager;
                _root = root;
                CancellationToken = new ETCancellationToken();
                _timeoutTimer = new System.Threading.Timer(onTimeout, this, ROOT_INITIALIZATION_TIMEOUT_MILLISECONDS,
                    Timeout.Infinite);
            }

            public ETCancellationToken CancellationToken { get; }
            public Root Root => _root;

            public void Complete()
            {
                Interlocked.Exchange(ref _isComplete, 1);
                _timeoutTimer.Dispose();
            }

            private static void onTimeout(object state)
            {
                RootInitializationOperation operation = (RootInitializationOperation)state;
                if (Interlocked.CompareExchange(ref operation._isComplete, 1, 0) == 0)
                {
                    operation._manager.ReportFatal(new TaskTimeoutException(
                        $"Root {operation._root.RootAddress} initialization exceeded " +
                        $"{ROOT_INITIALIZATION_TIMEOUT_SECONDS} seconds."));
                }
            }
        }
    }
}