using System;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /// <summary>
    /// How the ability is instanced when executed. This limits what an ability can do in its implementation. For example, a NonInstanced
    ///	Ability cannot have state. It is probably unsafe for an InstancedPerActor ability to have latent actions, etc.
    /// </summary>
    public enum EGameplayAbilityInstancingPolicy
    {
        /// <summary>
        /// This ability is never instanced. Anything that executes the ability is operating on the CDO.
        /// </summary>
        NonInstanced,
        /// <summary>
        /// Each actor gets their own instance of this ability. State can be saved, replication is possible.
        /// </summary>
        InstancedPerActor,
        /// <summary>
        /// We instance this ability each time it is executed. Replication currently unsupported.
        /// </summary>
        InstancedPerExecution,
    }

    /// <summary>
    /// Where does an ability execute on the network. Does a client "ask and predict", "ask and wait", "don't ask (just do it)" 
    /// </summary>
    public enum EGameplayAbilityNetExecutionPolicy
    {
        /// <summary>
        /// Part of this ability runs predictively on the local client if there is one
        /// ask and predict
        /// </summary>
        LocalPredicted,
        /// <summary>
        /// This ability will only run on the client or server that has local control
        /// don't ask (just do it)
        /// </summary>
        LocalOnly,
        /// <summary>
        /// This ability is initiated by the server, but will also run on the local client if one exists
        /// ask and wait
        /// </summary>
        ServerInitiated,
        /// <summary>
        /// This ability will only run on the server
        /// </summary>
        ServerOnly,
    }

    public enum EGameplayAbilityNetSecurityPolicy
    {
        /// <summary>
        /// No security requirements. Client or server can trigger execution and termination of this ability freely.
        /// </summary>
        ClientOrServer,
        /// <summary>
        /// A client requesting execution of this ability will be ignored by the server. Clients can still request that the server cancel or end this ability.
        /// </summary>
        ServerOnlyExecution,
        /// <summary>
        /// A client requesting cancellation or ending of this ability will be ignored by the server. Clients can still request execution of the ability.
        /// </summary>
        ServerOnlyTermination,
        /// <summary>
        /// Server controls both execution and termination of this ability. A client making any requests will be ignored.
        /// </summary>
        ServerOnly,
    }

    /// <summary>
    /// How an ability replicates state/events to everyone on the network
    /// </summary>
    public enum EGameplayAbilityReplicationPolicy
    {
        /// <summary>
        /// We don't replicate the instance of the ability to anyone.
        /// </summary>
        ReplicateNo,
        /// <summary>
        /// We replicate the instance of the ability to the owner.
        /// </summary>
        ReplicateYes,
    }

    /// <summary>
    /// 
    /// </summary>
    public enum EGameplayAbilityTriggerSource
    {
        /// <summary>
        /// Triggered from a gameplay event, will come with payload
        /// </summary>
        GameplayEvent,

        /// <summary>
        /// Triggered if the ability's owner gets a tag added, triggered once whenever it's added
        /// </summary>
        OwnedTagAdded,

       /// <summary>
       ///  Triggered if the ability's owner gets tag added, removed when the tag is removed
       /// </summary>
        OwnedTagPresent,
    }

    /// <summary>
    /// Describes the status of activating this ability, this is updated as prediction is handled
    /// </summary>
    public enum EGameplayAbilityActivationMode
    {
        /** We are the authority activating this ability */
        Authority,

        /** We are not the authority but aren't predicting yet. This is a mostly invalid state to be in */
        NonAuthority,

        /** We are predicting the activation of this ability */
        Predicting,

        /** We are not the authority, but the authority has confirmed this activation */
        Confirmed,

        /** We tried to activate it, and server told us we couldn't (even though we thought we could) */
        Rejected,
    }

    /// <summary>
    /// Describes what happens when a GameplayEffect, that is granting an active ability, is removed from its owner.
    /// </summary>
    public enum EGameplayEffectGrantedAbilityRemovePolicy
    {
        /** Active abilities are immediately canceled and the ability is removed. */
        CancelAbilityImmediately,
        /** Active abilities are allowed to finish, and then removed. */
        RemoveAbilityOnEnd,
        /** Granted abilities are left lone when the granting GameplayEffect is removed. */
        DoNothing,
    }

    /// <summary>
    /// Data tied to a specific activation of an ability.
    /// </summary>
    public struct GameplayAbilityActivationInfo
    {
        public GameplayAbilityActivationInfo(EGameplayAbilityActivationMode activationMode)
        {
            ActivationMode = activationMode;
            IsCanBeEndedByOtherInstance = false;
            _predictionKeyWhenActivated = default;
        }
        
        /// <summary>
        /// Activation status of this ability
        /// </summary>
        public EGameplayAbilityActivationMode ActivationMode { get;  private set; }
        
        /// <summary>
        /// An ability that runs on multiple game instances can be canceled by a remote instance, but only if that remote instance has already confirmed starting it.
        /// </summary>
        public bool IsCanBeEndedByOtherInstance { get;  private set; }
        
        /// <summary>
        /// This was the prediction key used to activate this ability. It does not get updated if new prediction keys are generated over the course of the ability
        /// </summary>
        private PredictionKey _predictionKeyWhenActivated;

        public void SetActivationConfirmed()
        {
            ActivationMode = EGameplayAbilityActivationMode.Confirmed;
            //Remote (server) commands to end the ability that come in after this point are considered for this instance
            IsCanBeEndedByOtherInstance = true;
        }

        /// <summary>
        ///  Called when activation was rejected by the server 
        /// </summary>
        public void SetActivationRejected()
        {
            ActivationMode = EGameplayAbilityActivationMode.Rejected;
        }

        /// <summary>
        /// Called on client to set this as a predicted ability
        /// </summary>
        /// <param name="predictionKey"></param>
        public void SetPredicting(PredictionKey predictionKey)
        {
            ActivationMode = EGameplayAbilityActivationMode.Predicting;
            _predictionKeyWhenActivated = predictionKey;

            // Abilities can be cancelled by server at any time. There is no reason to have to wait until confirmation.
            // prediction keys keep previous activations of abilities from ending future activations.
            IsCanBeEndedByOtherInstance = true;
        }

        public void ServerSetActivationPredictionKey(PredictionKey predictionKey)
        {
            _predictionKeyWhenActivated = predictionKey;
        }

        /// <summary>
        /// Returns prediction key, const to avoid being able to modify it after creation
        /// </summary>
        /// <returns></returns>
        public PredictionKey GetActivationPredictionKey()
        {
            return _predictionKeyWhenActivated;
        }
    }
    
    /// <summary>
    /// Cached data associated with an Actor using an Ability.
    /// </summary>
    public struct GameplayAbilityActorInfo
    {
        public EntityRef<Entity> OwnerActor;
        public EntityRef<Entity> AvatarActor;
        public WeakReference<AbilitySystemComponent> AbilitySystemComponent;

        public bool IsValid()
        {
            return AbilitySystemComponent != null;
        }
        
        public bool IsLocallyControlled()
        {
            return true;
        }
    }
    
    /// <summary>
    /// Data about montages that were played locally (all montages in case of server. predictive montages in case of client). Never replicated directly.
    /// </summary>
    public struct GameplayAbilityLocalAnimMontage
    {
        
    }
    
    /// <summary>
    /// Metadata for a tag-based Gameplay Event, that can activate other abilities or run ability-specific logic 
    /// </summary>
    public struct GameplayEventData
    {
        /// <summary>
        /// Tag of the event that triggered this
        /// </summary>
        public GameplayTag EventTag;
        
        /// <summary>
        /// The magnitude of the triggering event 
        /// </summary>
        public float EventMagnitude;
        
        /// <summary>
        /// Polymorphic context information
        /// </summary>
        public GameplayEffectContextHandle ContextHandle;

        /// <summary>
        /// The instigator of the event 
        /// </summary>
        public EntityRef<Entity> Instigator;
        
        /// <summary>
        /// The target of the event
        /// </summary>
        public EntityRef<Entity> Target;

        /// <summary>
        /// Tags that the instigator has
        /// </summary>
        public GameplayTagContainer InstigatorTags;
        
        /// <summary>
        /// Tags that the target has
        /// </summary>
        public GameplayTagContainer TargetTags;

        /// <summary>
        /// An optional ability-specific object to be passed though the event
        /// </summary>
        public object OptionalObject;
        
        /// <summary>
        /// A second optional ability-specific object to be passed though the event
        /// </summary>
        public object OptionalObject2;
    }
    
    /// <summary>
    /// Ability Ended Data 
    /// </summary>
    public struct AbilityEndedData
    {
        public AbilityEndedData(GameplayAbility inAbility,
            GameplayAbilitySpecHandle inSpec,
            bool bInReplicateEndAbility,
            bool bInWasCancelled)
        {
            AbilityThatEnded = inAbility;
            AbilitySpecHandle = inSpec;
            bReplicateEndAbility = bInReplicateEndAbility;
            bWasCancelled = bInWasCancelled;
        }
        /// <summary>
        /// Ability that ended, normally instance but could be CDO
        /// </summary>
        public GameplayAbility AbilityThatEnded;
        
        /// <summary>
        /// Specific ability spec that ended
        /// </summary>
        public GameplayAbilitySpecHandle AbilitySpecHandle;
        
        /// <summary>
        /// Rather to replicate the ability to ending
        /// </summary>
        public bool bReplicateEndAbility;
        
        /// <summary>
        /// True if this was cancelled deliberately, false if it ended normally 
        /// </summary>
        public bool bWasCancelled; 
    }

    public struct ServerAbilityRPCBatch
    {
        
    }

    public struct ScopedServerAbilityRPCBatcher
    {
        
    }
}