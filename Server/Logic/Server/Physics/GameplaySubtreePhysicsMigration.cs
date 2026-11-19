using System;
using System.Collections.Generic;
using Bliss.Base.Collections;
using Bliss.Physics;
using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace Bliss;

internal sealed unsafe class GameplaySubtreePhysicsMigration
{
    private readonly PhysicalScene source;
    private readonly PhysicalScene destination;
    private readonly List<GameplayRigidbody> bodies = new();
    private readonly List<GameplayCollider> colliders = new();
    private int movedBodyCount;

    private GameplaySubtreePhysicsMigration(PhysicalScene source, PhysicalScene destination)
    {
        this.source = source;
        this.destination = destination;
    }

    internal static GameplaySubtreePhysicsMigration prepare(Entity subtree, PhysicalScene source, PhysicalScene destination)
    {
        if (source == null || destination == null || source.IsDisposed || destination.IsDisposed ||
            source.PxScene == null || destination.PxScene == null ||
            source.PhysicalWorld == null || !ReferenceEquals(source.PhysicalWorld, destination.PhysicalWorld))
        {
            throw new InvalidOperationException("Migration requires live PhysX scenes in the same PhysicalWorld.");
        }

        GameplaySubtreePhysicsMigration migration = new(source, destination);
        migration.collect(subtree);
        migration.validate();
        return migration;
    }

    internal void move()
    {
        try
        {
            foreach (GameplayRigidbody body in this.bodies)
            {
                PxActor* actor = (PxActor*)body.RigidBodyPtr;
                PxScene_removeActor_mut(this.source.PxScene, actor, false);
                this.movedBodyCount++;
                if (PxActor_getScene(actor) != null)
                {
                    throw new InvalidOperationException("PhysX did not remove a body from the source scene.");
                }

                if (!PxScene_addActor_mut(this.destination.PxScene, actor, null))
                {
                    throw new InvalidOperationException("PhysX refused to add a body to the destination scene.");
                }
            }

            foreach (GameplayRigidbody body in this.bodies)
            {
                this.source.RigidbodyDict.RemoveByValue(body);
                this.destination.RigidbodyDict.Add((IntPtr)body.RigidBodyPtr, body);
            }

            foreach (GameplayCollider collider in this.colliders)
            {
                this.source.ColliderDict.RemoveByValue(collider);
                this.destination.ColliderDict.Add((IntPtr)collider.ShapePtr, collider);
            }
        }
        catch (Exception exception)
        {
            try
            {
                this.rollback();
            }
            catch (Exception rollbackException)
            {
                throw new GameplaySubtreeMigration.GameplayMigrationRollbackException(
                    "PhysX migration and rollback failed.", exception, rollbackException);
            }

            throw;
        }
    }

    internal void rollback()
    {
        List<Exception> failures = null;
        foreach (GameplayCollider collider in this.colliders)
        {
            try
            {
                restoreMapping(this.source.ColliderDict, this.destination.ColliderDict,
                    (IntPtr)collider.ShapePtr, collider);
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }

        foreach (GameplayRigidbody body in this.bodies)
        {
            try
            {
                restoreMapping(this.source.RigidbodyDict, this.destination.RigidbodyDict,
                    (IntPtr)body.RigidBodyPtr, body);
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }

        while (this.movedBodyCount > 0)
        {
            GameplayRigidbody body = this.bodies[--this.movedBodyCount];
            try
            {
                PxActor* actor = (PxActor*)body.RigidBodyPtr;
                PxScene* scene = PxActor_getScene(actor);
                if (scene == this.destination.PxScene)
                {
                    PxScene_removeActor_mut(this.destination.PxScene, actor, false);
                    scene = PxActor_getScene(actor);
                }

                if (scene != this.source.PxScene &&
                    (scene != null || !PxScene_addActor_mut(this.source.PxScene, actor, null)))
                {
                    throw new InvalidOperationException("PhysX failed to restore a migrated body.");
                }
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }

        if (failures != null)
        {
            throw new AggregateException("Restoring PhysX resources failed.", failures);
        }
    }

    private static void restoreMapping<T>(DoublyDictionary<IntPtr, T> source,
        DoublyDictionary<IntPtr, T> destination, IntPtr key, T value) where T : class
    {
        if (destination.TryGetValueByKey(key, out T destinationValue))
        {
            if (!ReferenceEquals(destinationValue, value))
            {
                throw new InvalidOperationException("The destination physical mapping changed during rollback.");
            }

            destination.RemoveByKey(key);
        }
        else if (destination.ContainsValue(value))
        {
            destination.RemoveByValue(value);
        }

        if (!source.TryGetValueByKey(key, out T sourceValue))
        {
            source.Add(key, value);
            if (!source.TryGetValueByKey(key, out sourceValue))
            {
                throw new InvalidOperationException("The source physical mapping could not be restored.");
            }
        }

        if (!ReferenceEquals(sourceValue, value) ||
            !source.TryGetKeyByValue(value, out IntPtr registeredKey) || registeredKey != key)
        {
            throw new InvalidOperationException("The source physical mapping changed during rollback.");
        }
    }

    private void collect(Entity entity)
    {
        if (entity is ILogicScene)
        {
            return;
        }

        if (entity is GameplayRigidbody body)
        {
            this.bodies.Add(body);
        }
        else if (entity is GameplayCollider collider)
        {
            this.colliders.Add(collider);
        }
        if (entity.ComponentCount > 0)
        {
            foreach (Entity component in entity.Components.Values)
            {
                this.collect(component);
            }
        }

        if (entity.ChildCount > 0)
        {
            foreach (Entity child in entity.Children.Values)
            {
                this.collect(child);
            }
        }
    }

    private void validate()
    {
        foreach (GameplayRigidbody body in this.bodies)
        {
            PxRigidActor* actor = (PxRigidActor*)body.RigidBodyPtr;
            if (body.IsDisposed || actor == null || PxActor_getScene((PxActor*)actor) != this.source.PxScene ||
                !this.source.RigidbodyDict.TryGetValueByKey((IntPtr)actor, out GameplayRigidbody registered) ||
                !ReferenceEquals(registered, body) || this.destination.RigidbodyDict.ContainsKey((IntPtr)actor) ||
                this.destination.RigidbodyDict.ContainsValue(body))
            {
                throw new InvalidOperationException("A rigidbody is not exclusively owned by the source physical scene.");
            }

            uint colliderCount = 0;
            foreach (GameplayCollider collider in this.colliders)
            {
                if (ReferenceEquals(collider.AttachedRigidbody, body))
                {
                    colliderCount++;
                }
            }

            if (PxRigidActor_getNbShapes(actor) != colliderCount)
            {
                throw new InvalidOperationException("A rigidbody has shapes outside the migrating subtree.");
            }
        }

        foreach (GameplayCollider collider in this.colliders)
        {
            PxShape* shape = collider.ShapePtr;
            GameplayRigidbody body = collider.AttachedRigidbody;
            if (collider.IsDisposed || shape == null || body == null || !this.bodies.Contains(body) ||
                PxShape_getActor(shape) != (PxRigidActor*)body.RigidBodyPtr ||
                !this.source.ColliderDict.TryGetValueByKey((IntPtr)shape, out GameplayCollider registered) ||
                !ReferenceEquals(registered, collider) || this.destination.ColliderDict.ContainsKey((IntPtr)shape) ||
                this.destination.ColliderDict.ContainsValue(collider))
            {
                throw new InvalidOperationException("A collider is not exclusively owned by a migrating rigidbody.");
            }
        }
    }
}
