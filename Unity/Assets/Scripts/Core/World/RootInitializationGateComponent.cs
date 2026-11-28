using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>Identifies the ingress source of a message targeting a Server Root.</summary>
    public enum ERootMessageIngress
    {
        /// <summary>A message received from an external client session.</summary>
        External,
        /// <summary>A message received through Server-internal routing.</summary>
        Internal,
    }

    /// <summary>An immutable snapshot of a Root's initialization message allowlist.</summary>
    public sealed class RootInitializationMessagePolicy
    {
        private readonly HashSet<(Type, ERootMessageIngress)> _allowedMessages;

        internal static readonly RootInitializationMessagePolicy Empty =
            new RootInitializationMessagePolicy(new HashSet<(Type, ERootMessageIngress)>());

        private RootInitializationMessagePolicy(HashSet<(Type, ERootMessageIngress)> allowedMessages)
        {
            _allowedMessages = allowedMessages;
        }

        /// <summary>Checks whether a message type and ingress are allowed during initialization.</summary>
        /// <param name="messageType">The concrete message type.</param>
        /// <param name="ingress">The message ingress source.</param>
        /// <returns>Whether the message is explicitly allowed.</returns>
        public bool Allows(Type messageType, ERootMessageIngress ingress)
        {
            return _allowedMessages.Contains((messageType, ingress));
        }

        internal RootInitializationMessagePolicy WithAllowedMessage(Type messageType, ERootMessageIngress ingress)
        {
            var allowedMessages = new HashSet<(Type, ERootMessageIngress)>(_allowedMessages);
            allowedMessages.Add((messageType, ingress));
            return new RootInitializationMessagePolicy(allowedMessages);
        }
    }

    /// <summary>
    /// Restricts update systems and message admission during Root initialization and failure handling.
    /// </summary>
    public sealed class RootInitializationGateComponent : Entity, IAwake, IDestroy
    {
        private readonly HashSet<Type> _allowedSystemTypes = new HashSet<Type>();
        private readonly HashSet<Type> _allowedFailedSystemTypes = new HashSet<Type>();

        /// <summary>Allows a concrete message type from a specific ingress during initialization.</summary>
        /// <param name="messageType">The concrete message type.</param>
        /// <param name="ingress">The message ingress source.</param>
        /// <exception cref="ArgumentNullException">The message type is null.</exception>
        /// <exception cref="ArgumentException">The type or ingress is invalid.</exception>
        public void AllowMessage(Type messageType, ERootMessageIngress ingress)
        {
            if (messageType == null)
            {
                throw new ArgumentNullException(nameof(messageType));
            }

            if (messageType.IsAbstract || !typeof(IMessage).IsAssignableFrom(messageType)
                || !Enum.IsDefined(typeof(ERootMessageIngress), ingress))
            {
                throw new ArgumentException("A concrete message type and valid ingress are required.");
            }

            Root root = this.Root();
            root.SetInitializationMessagePolicy(root.InitializationMessagePolicy.WithAllowedMessage(messageType, ingress));
        }

        /// <summary>
        /// Allows a concrete update-system type to run during initialization.
        /// </summary>
        /// <param name="systemType">The concrete update-system type.</param>
        /// <exception cref="ArgumentNullException">The system type is null.</exception>
        /// <exception cref="ArgumentException">The type is not a concrete update system.</exception>
        public void AllowSystem(Type systemType)
        {
            if (systemType == null)
            {
                throw new ArgumentNullException(nameof(systemType));
            }

            if (systemType.IsAbstract || !typeof(IUpdateGroupSystem).IsAssignableFrom(systemType))
            {
                throw new ArgumentException("A concrete update-system type is required.", nameof(systemType));
            }

            _allowedSystemTypes.Add(systemType);
        }

        /// <summary>Allows a previously admitted update system to run while the Root is failed.</summary>
        /// <param name="systemType">The concrete update-system type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="systemType"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="systemType"/> is not a concrete update system.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="systemType"/> is not allowed during initialization.</exception>
        public void AllowSystemAfterFailure(Type systemType)
        {
            if (systemType == null)
            {
                throw new ArgumentNullException(nameof(systemType));
            }

            if (systemType.IsAbstract || !typeof(IUpdateGroupSystem).IsAssignableFrom(systemType))
            {
                throw new ArgumentException("A concrete update-system type is required.", nameof(systemType));
            }

            if (!_allowedSystemTypes.Contains(systemType))
            {
                throw new InvalidOperationException("A system must be allowed during initialization before failure.");
            }

            _allowedFailedSystemTypes.Add(systemType);
        }

        internal bool AllowsSystem(Type systemType)
        {
            return _allowedSystemTypes.Contains(systemType);
        }

        internal bool AllowsSystemAfterFailure(Type systemType)
        {
            return _allowedFailedSystemTypes.Contains(systemType);
        }

        internal void ClearAllowedSystems()
        {
            _allowedSystemTypes.Clear();
            _allowedFailedSystemTypes.Clear();
        }
    }

    /// <summary>Clears the update-system policy when the temporary gate is destroyed.</summary>
    [EntitySystem]
    public sealed class RootInitializationGateDestroySystem : DestroySystem<RootInitializationGateComponent>
    {
        /// <inheritdoc />
        protected override void Destroy(RootInitializationGateComponent entity)
        {
            entity.ClearAllowedSystems();
        }
    }
}
