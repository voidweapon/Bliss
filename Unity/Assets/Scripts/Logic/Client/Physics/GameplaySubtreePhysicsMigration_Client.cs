using System;
using System.Collections.Generic;

namespace Bliss
{
    /// <summary>Requests synchronous validation of client resources before subtree migration.</summary>
    public struct ValidateGameplaySubtreeClientResources
    {
        /// <summary>The subtree whose views and physics resources must remain alive.</summary>
        public Entity Subtree;
    }

    internal static class GameplaySubtreePhysicsMigrationClient
    {
        internal static void validate(Entity subtree, LogicScene source)
        {
            HashSet<GameplayRigidbody> bodies = new HashSet<GameplayRigidbody>();
            HashSet<GameplayCollider> colliders = new HashSet<GameplayCollider>();
            collect(subtree, bodies, colliders);

            foreach (GameplayCollider collider in colliders)
            {
                if (!isInside(collider.Parent, subtree) ||
                    (collider.AttachedRigidbody != null && !bodies.Contains(collider.AttachedRigidbody)))
                {
                    throw new InvalidOperationException("A client collider cannot migrate without its owner and rigidbody.");
                }
            }

            EventSystem.Instance.Invoke(PhysicalInvokeTypes.GAMEPLAY_SUBTREE_MIGRATION_VALIDATE,
                new ValidateGameplaySubtreeClientResources { Subtree = subtree });

            if (bodies.Count == 0)
            {
                return;
            }

            if (source.ChildCount > 0)
            {
                foreach (Entity child in source.Children.Values)
                {
                    validateExternalColliders(child, bodies, colliders);
                }
            }

            if (source.ComponentCount > 0)
            {
                foreach (Entity component in source.Components.Values)
                {
                    validateExternalColliders(component, bodies, colliders);
                }
            }

        }

        private static bool isInside(Entity entity, Entity subtree)
        {
            for (Entity current = entity; current != null && current is not ILogicScene; current = current.Parent)
            {
                if (ReferenceEquals(current, subtree))
                {
                    return true;
                }
            }

            return false;
        }

        private static void collect(Entity entity, HashSet<GameplayRigidbody> bodies,
            HashSet<GameplayCollider> colliders)
        {
            if (entity is ILogicScene)
            {
                return;
            }

            if (entity is GameplayRigidbody body)
            {
                bodies.Add(body);
            }
            else if (entity is GameplayCollider collider)
            {
                colliders.Add(collider);
            }

            if (entity.ComponentCount > 0)
            {
                foreach (Entity component in entity.Components.Values)
                {
                    collect(component, bodies, colliders);
                }
            }

            if (entity.ChildCount > 0)
            {
                foreach (Entity child in entity.Children.Values)
                {
                    collect(child, bodies, colliders);
                }
            }
        }

        private static void validateExternalColliders(Entity entity, HashSet<GameplayRigidbody> bodies,
            HashSet<GameplayCollider> colliders)
        {
            if (entity is ILogicScene)
            {
                return;
            }

            if (entity is GameplayCollider collider && collider.AttachedRigidbody != null &&
                bodies.Contains(collider.AttachedRigidbody) && !colliders.Contains(collider))
            {
                throw new InvalidOperationException("A client rigidbody has colliders outside the migrating subtree.");
            }

            if (entity.ComponentCount > 0)
            {
                foreach (Entity component in entity.Components.Values)
                {
                    validateExternalColliders(component, bodies, colliders);
                }
            }

            if (entity.ChildCount > 0)
            {
                foreach (Entity child in entity.Children.Values)
                {
                    validateExternalColliders(child, bodies, colliders);
                }
            }
        }
    }
}
