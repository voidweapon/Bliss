using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Bliss
{
    public class ProcessMessageManager : Singleton<ProcessMessageManager>
    {
        // Root ID to the queue responsible for dispatching messages to that Root.
        private readonly ConcurrentDictionary<int, RootMessageQueue> _messageQueues = new();
        // Root ID to the live Root, including the interval before its message route is registered.
        private readonly ConcurrentDictionary<int, Root> _roots = new();

        public bool Enqueue(ActorAddress targetAddress, MessageObject message)
        {
            return Enqueue(targetAddress.RootAddress, targetAddress, message);
        }

        /// <summary>Captures the target Root and its route state at message receipt.</summary>
        /// <param name="rootId">The target Root identifier.</param>
        /// <returns>The admission snapshot, or null if the Root does not exist.</returns>
        public RootMessageAdmission CaptureAdmission(int rootId)
        {
            if (!_roots.TryGetValue(rootId, out Root root))
            {
                return null;
            }

            bool hasRoute = _messageQueues.TryGetValue(rootId, out RootMessageQueue queue) && queue.Root == root;
            return new RootMessageAdmission(root, root.InitializationSnapshot, hasRoute);
        }

        internal void trackRoot(Root root)
        {
            if (!_roots.TryAdd(root.Id, root) && _roots[root.Id] != root)
            {
                throw new System.InvalidOperationException($"Root {root.Id} is already registered.");
            }
        }

        internal void untrackRoot(Root root)
        {
            ((ICollection<KeyValuePair<int, Root>>)_roots).Remove(new KeyValuePair<int, Root>(root.Id, root));
        }
    
        /// <summary>Queues admitted messages and pending RPC responses, or rejects them when initialization admission is closed.</summary>
        /// <param name="fromAddress">The Root that originated the message.</param>
        /// <param name="targetAddress">The destination Actor address.</param>
        /// <param name="message">The message to enqueue.</param>
        /// <returns>True when the message was queued or rejected; false when the target Root is unknown.</returns>
        public bool Enqueue(RootAddress fromAddress, ActorAddress targetAddress, MessageObject message)
        {
            if(!_messageQueues.TryGetValue(targetAddress.Root, out var queue))
            {
                if (_roots.TryGetValue(targetAddress.Root, out Root targetRoot))
                {
                    ERootInitializationState state = targetRoot.InitializationState;
                    if (state == ERootInitializationState.Initializing || state == ERootInitializationState.Failed)
                    {
                        rejectMessage(targetAddress.RootAddress, fromAddress, message, ErrorCode.ERR_ROOT_NOT_READY);
                        return true;
                    }
                }
                return false;
            }
        
            lock (queue)
            {
                if (!queue.IsRegistered)
                {
                    return false;
                }
                RootInitializationSnapshot snapshot = queue.Root.InitializationSnapshot;
                bool isOneWay = message is not IResponse && (message is not IRequest || message is ILocationMessage);
                bool isRpcRequest = message is IRequest && message is not ILocationMessage;
                if (snapshot.State == ERootInitializationState.Initializing && isRpcRequest)
                {
                    rejectMessage(targetAddress.RootAddress, fromAddress, message, ErrorCode.ERR_ROOT_NOT_READY);
                    return true;
                }

                if (snapshot.State != ERootInitializationState.Failed || message is IResponse)
                {
                    if (snapshot.State == ERootInitializationState.Initializing && isOneWay
                        && !snapshot.MessagePolicy.Allows(message.GetType(), ERootMessageIngress.Internal))
                    {
                        Log.Warning($"Root {targetAddress.RootAddress} rejected internal message {message.GetType().FullName} from {fromAddress}");
                        return true;
                    }

                    queue.Messages.Enqueue(new ProcessMessageInfo()
                    {
                        TargetAddress = new ActorAddress(fromAddress, targetAddress.InstanceId),
                        MessageObject = message,
                        InitializationSnapshot = snapshot,
                    });
                    return true;
                }
            }

            rejectMessage(targetAddress.RootAddress, fromAddress, message, ErrorCode.ERR_ROOT_NOT_READY);
            return true;
        }

        internal void rejectFailedMessages(Root root)
        {
            if (!_messageQueues.TryGetValue(root.Id, out RootMessageQueue queue) || queue.Root != root)
            {
                return;
            }

            List<ProcessMessageInfo> pending = new List<ProcessMessageInfo>();
            List<ProcessMessageInfo> pendingResponses = new List<ProcessMessageInfo>();
            lock (queue)
            {
                while (queue.Messages.TryDequeue(out ProcessMessageInfo message))
                {
                    if (message.MessageObject is IResponse)
                    {
                        pendingResponses.Add(message);
                        continue;
                    }

                    pending.Add(message);
                }

                foreach (ProcessMessageInfo response in pendingResponses)
                {
                    queue.Messages.Enqueue(response);
                }
            }

            foreach (ProcessMessageInfo message in pending)
            {
                rejectMessage(root.RootAddress, message.TargetAddress.RootAddress, message.MessageObject,
                    ErrorCode.ERR_ROOT_NOT_READY);
            }
        }

        private void rejectMessage(RootAddress target, RootAddress from, MessageObject message, int error)
        {
            if (message is IResponse)
            {
                return;
            }

            if (message is IRequest request && message is not ILocationMessage)
            {
                MessageObject rejection = (MessageObject)MessageOpcodeManager.Instance.CreateResponseBy(
                    request.GetType(), request.RpcId, error);
                if (!Enqueue(target, new ActorAddress(from), rejection))
                {
                    Log.Warning($"Root {target} could not reply to {from} for rejected internal message {message.GetType().FullName}");
                }
                return;
            }

            Log.Warning($"Root {target} rejected internal message {message.GetType().FullName} from {from}");
        }

        public void Fetch(int processId, List<ProcessMessageInfo> list, int maxCount)
        {
            if(!_messageQueues.TryGetValue(processId, out var queue))
            {
                return;
            }

            for (int i = 0; i < maxCount; i++)
            {
                if (!queue.Messages.TryDequeue(out var info))
                {
                    break;
                }
            
                list.Add(info);
            }
        }

        /// <summary>Registers a Root as the owner of its internal message queue.</summary>
        /// <param name="root">The Root whose messages are to be routed.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="root"/> is null.</exception>
        /// <exception cref="System.InvalidOperationException">Another Root uses the identifier, or a route is already registered for it.</exception>
        public void Register(Root root)
        {
            System.ArgumentNullException.ThrowIfNull(root);
            trackRoot(root);
            if (!_messageQueues.TryAdd(root.Id, new RootMessageQueue(root)))
            {
                throw new System.InvalidOperationException($"Root {root.Id} already has a message route.");
            }
        }

        /// <summary>Removes a Root's route and rejects messages still awaiting delivery.</summary>
        /// <param name="root">The Root that owns the route.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="root"/> is null.</exception>
        public void Unregister(Root root)
        {
            System.ArgumentNullException.ThrowIfNull(root);
            if (!_messageQueues.TryGetValue(root.Id, out RootMessageQueue queue) || queue.Root != root)
            {
                return;
            }

            List<ProcessMessageInfo> pending = new List<ProcessMessageInfo>();
            lock (queue)
            {
                queue.IsRegistered = false;
                _messageQueues.TryRemove(root.Id, out _);
                while (queue.Messages.TryDequeue(out ProcessMessageInfo message))
                {
                    pending.Add(message);
                }
            }

            foreach (ProcessMessageInfo message in pending)
            {
                rejectMessage(root.RootAddress, message.TargetAddress.RootAddress, message.MessageObject,
                    ErrorCode.ERR_ACTOR_NOT_FOUND);
            }
        }

        private sealed class RootMessageQueue
        {
            public RootMessageQueue(Root root)
            {
                Root = root;
            }

            public Root Root { get; }
            public bool IsRegistered { get; set; } = true;
            public ConcurrentQueue<ProcessMessageInfo> Messages { get; } = new ConcurrentQueue<ProcessMessageInfo>();
        }
    }

    /// <summary>The destination Root and routing state captured when a message arrived.</summary>
    public sealed class RootMessageAdmission
    {
        internal RootMessageAdmission(Root root, RootInitializationSnapshot snapshot, bool hasRoute)
        {
            Root = root;
            Snapshot = snapshot;
            HasRoute = hasRoute;
        }

        /// <summary>The Root instance that existed when the message arrived.</summary>
        public Root Root { get; }
        /// <summary>The Root's initialization state and policy at receipt.</summary>
        public RootInitializationSnapshot Snapshot { get; }
        /// <summary>Whether the Root's message route was registered at receipt.</summary>
        public bool HasRoute { get; }
    }

    public struct ProcessMessageInfo
    {
        public ActorAddress TargetAddress;
        public MessageObject MessageObject;
        /// <summary>The target Root's admission state at enqueue time.</summary>
        public RootInitializationSnapshot InitializationSnapshot;
    }
}