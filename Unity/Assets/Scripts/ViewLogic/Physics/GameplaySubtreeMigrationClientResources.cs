using System;
using Bliss.Core.EventSystem;
using Bliss.ViewModel;
using UnityEngine;

namespace Bliss.ViewLogic.Physics
{
    /// <summary>Rejects client views and physical resources that are not hosted under the persistent Global node.</summary>
    [Invoke(PhysicalInvokeTypes.GAMEPLAY_SUBTREE_MIGRATION_VALIDATE)]
    public sealed class GameplaySubtreeMigrationClientResources : AInvokeHandler<ValidateGameplaySubtreeClientResources>
    {
        /// <inheritdoc />
        public override void Handle(ValidateGameplaySubtreeClientResources request)
        {
            ViewGlobalComponent global = request.Subtree.RootScene().GetComponent<ViewGlobalComponent>();
            validate(request.Subtree, global);
        }

        private static void validate(Entity entity, ViewGlobalComponent global)
        {
            if (entity is ILogicScene)
            {
                return;
            }

            if (entity is GameObjectViewComponent view)
            {
                validateView(view.View, global);
                validateView(view.ViewMode, global);
            }

            if (entity is GameplayRigidbody body)
            {
                GameObjectViewComponent ownerView = body.Parent.GetComponent<GameObjectViewComponent>();
                if (ownerView == null || ownerView.View == null ||
                    (!body.IsStatic && ownerView.View.GetComponent<Rigidbody>() == null))
                {
                    throw new InvalidOperationException("The client rigidbody has no live Unity resource.");
                }
            }
            else if (entity is GameplayCollider collider)
            {
                GameObjectViewComponent ownerView = collider.Parent.GetComponent<GameObjectViewComponent>();
                Collider unityCollider = ViewEntityMapManager.Instance?.GetCollider(collider);
                if (ownerView == null || ownerView.View == null || unityCollider == null ||
                    unityCollider.gameObject != ownerView.View)
                {
                    throw new InvalidOperationException("The client collider has no live Unity resource.");
                }

                GameplayRigidbody attachedBody = collider.AttachedRigidbody;
                GameObjectViewComponent bodyView = attachedBody?.Parent.GetComponent<GameObjectViewComponent>();
                Rigidbody unityBody = bodyView?.View == null ? null : bodyView.View.GetComponent<Rigidbody>();
                if (unityCollider.attachedRigidbody != unityBody)
                {
                    throw new InvalidOperationException("The client collider's Unity rigidbody does not match gameplay ownership.");
                }
            }

            if (entity.ComponentCount > 0)
            {
                foreach (Entity component in entity.Components.Values)
                {
                    validate(component, global);
                }
            }

            if (entity.ChildCount > 0)
            {
                foreach (Entity child in entity.Children.Values)
                {
                    validate(child, global);
                }
            }
        }

        private static void validateView(GameObject view, ViewGlobalComponent global)
        {
            if (view == null)
            {
                if (!ReferenceEquals(view, null))
                {
                    throw new InvalidOperationException("A client view was destroyed before migration.");
                }

                return;
            }

            if (global == null || global.GlobalTransform == null || global.EntitiesTransform == null ||
                !global.EntitiesTransform.IsChildOf(global.GlobalTransform) ||
                !view.transform.IsChildOf(global.EntitiesTransform))
            {
                throw new InvalidOperationException("Client views must remain under the persistent Global entities node.");
            }
        }
    }
}
