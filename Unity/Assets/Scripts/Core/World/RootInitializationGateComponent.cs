using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>
    /// Restricts update systems while a Server Root is initializing.
    /// </summary>
    public sealed class RootInitializationGateComponent : Entity, IAwake, IDestroy
    {
        private readonly HashSet<Type> _allowedSystemTypes = new HashSet<Type>();

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

        internal bool AllowsSystem(Type systemType)
        {
            return _allowedSystemTypes.Contains(systemType);
        }

        internal void ClearAllowedSystems()
        {
            _allowedSystemTypes.Clear();
        }
    }

    /// <summary>Clears initialization policy when the temporary gate is destroyed.</summary>
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
