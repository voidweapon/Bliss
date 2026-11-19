using System;
using System.Collections.Generic;
using Bliss.Id;
using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace Bliss.Physics;

public static unsafe partial class PhysicalSceneSystems
{
    private const float CHARACTER_STATIC_FRICTION = 0.6f;
    private const float CHARACTER_DYNAMIC_FRICTION = 0.6f;
    private const float CHARACTER_BOUNCINESS = 0f;

    /// <summary>Creates a kinematic PhysX body and capsule owned by a character entity.</summary>
    /// <param name="self">The character's live physical scene.</param>
    /// <param name="character">The entity that owns the Transform and GameplayLayer.</param>
    /// <param name="capsuleParams">The capsule dimensions and local pose.</param>
    /// <returns>The registered character capsule.</returns>
    /// <exception cref="InvalidOperationException">The scene, required components or native resources are unavailable.</exception>
    public static GameplayCapsuleCollider CreateCharacterPhysics(
        this PhysicalScene self, Entity character, CapsuleColliderInitParams capsuleParams)
    {
        if (self == null || self.IsDisposed || character == null || character.IsDisposed ||
            character.Scene() != self.Scene() || self.PxScene == null ||
            self.PhysicalWorld == null || self.PhysicalWorld.Physics == null)
        {
            throw new InvalidOperationException("A live character and its own physical scene are required.");
        }

        TransformComponent transform = character.GetComponent<TransformComponent>()
            ?? throw new InvalidOperationException("The character has no TransformComponent.");
        GameplayLayer layer = character.GetComponent<GameplayLayer>()
            ?? throw new InvalidOperationException("The character has no GameplayLayer.");
        if (character.GetComponent<GameplayRigidbody>() != null)
        {
            throw new InvalidOperationException("The character already has a GameplayRigidbody.");
        }

        PxMaterial* material = null;
        PxRigidDynamic* actor = null;
        PxShape* shape = null;
        bool ownsShape = false;
        long? capsuleId = null;
        try
        {
            material = PxPhysics_createMaterial_mut(self.PhysicalWorld.Physics,
                CHARACTER_STATIC_FRICTION, CHARACTER_DYNAMIC_FRICTION, CHARACTER_BOUNCINESS);
            if (material == null)
            {
                throw new InvalidOperationException("PhysX failed to create the character material.");
            }

            var pose = CreatePxTransform(transform.Transform.Location, transform.Transform.Rotation);
            actor = PxPhysics_createRigidDynamic_mut(self.PhysicalWorld.Physics, &pose);
            if (actor == null)
            {
                throw new InvalidOperationException("PhysX failed to create the character body.");
            }
            PxRigidBody_setRigidBodyFlag_mut((PxRigidBody*)actor, PxRigidBodyFlag.Kinematic, true);

            PxCapsuleGeometry geometry = PxCapsuleGeometry_new(
                capsuleParams.Radius, PxTypeHelper.GetPxCapsuleHalfHeight(capsuleParams.Height, capsuleParams.Radius));
            shape = PxPhysics_createShape_mut(self.PhysicalWorld.Physics, (PxGeometry*)&geometry,
                material, true, GetShapeFlags(capsuleParams.IsTrigger));
            if (shape == null)
            {
                throw new InvalidOperationException("PhysX failed to create the character capsule.");
            }
            ownsShape = true;
            var localPosition = capsuleParams.Center.ToPxVec3();
            var localRotation = PxTypeHelper.GetPxCapsuleLocalQuat(capsuleParams.Direction);
            var localPose = PxTransform_new_5(&localPosition, &localRotation);
            PxShape_setLocalPose_mut(shape, &localPose);
            SetShapeQueryFilterData(shape, layer.Layer, capsuleParams.IsTrigger);
            if (!PxRigidActor_attachShape_mut((PxRigidActor*)actor, shape))
            {
                throw new InvalidOperationException("PhysX failed to attach the character capsule.");
            }

            // The actor now owns the shape reference; detaching the capsule releases it.
            PxShape_release_mut(shape);
            ownsShape = false;
            PxScene_addActor_mut(self.PxScene, (PxActor*)actor, null);
            GameplayRigidbody body = character.AddComponent<GameplayRigidbody, GameplayRigidbodyInitParams>(
                new GameplayRigidbodyInitParams { RigidBodyPtr = (PxRigidBody*)actor, IsKinematic = true });
            body.Rotation = transform.Transform.Rotation;
            body.Position = transform.Transform.Location;
            self.RigidbodyDict.Add((IntPtr)actor, body);

            capsuleParams.PxShape = shape;
            capsuleId = IdGenerator.Instance.GenerateId();
            GameplayCapsuleCollider capsule = character.AddChildWithId<GameplayCapsuleCollider, CapsuleColliderInitParams>(
                capsuleId.Value, capsuleParams);
            self.ColliderDict.Add((IntPtr)shape, capsule);
            return capsule;
        }
        catch (Exception exception)
        {
            List<Exception> cleanupFailures = new List<Exception>();
            try
            {
                if (capsuleId.HasValue)
                {
                    character.GetChild<GameplayCapsuleCollider>(capsuleId.Value)?.Dispose();
                }
            }
            catch (Exception cleanupException)
            {
                cleanupFailures.Add(cleanupException);
            }

            GameplayRigidbody body = character.GetComponent<GameplayRigidbody>();
            if (body != null && body.RigidBodyPtr == (PxRigidBody*)actor)
            {
                actor = null;
                try
                {
                    body.Dispose();
                }
                catch (Exception cleanupException)
                {
                    cleanupFailures.Add(cleanupException);
                }
            }
            if (actor != null)
            {
                PxRigidActor_release_mut((PxRigidActor*)actor);
            }
            if (cleanupFailures.Count > 0)
            {
                cleanupFailures.Insert(0, exception);
                throw new AggregateException("Character physics creation and cleanup failed.", cleanupFailures);
            }
            throw;
        }
        finally
        {
            if (ownsShape)
            {
                PxShape_release_mut(shape);
            }
            if (material != null)
            {
                PxBase_release_mut((PxBase*)material);
            }
        }
    }
}
