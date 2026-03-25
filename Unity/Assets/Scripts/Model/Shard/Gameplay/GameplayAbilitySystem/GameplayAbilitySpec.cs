using System;
using System.Collections.Generic;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public struct GameplayAbilitySpec
    {
        public GameplayAbilitySpecHandle Handle;
        
        /// <summary>
        ///  Ability of the spec (Always the CDO.)
        /// </summary>
        public GameplayAbility Ability;
        
        /// <summary>
        /// Non replicating instances of this ability.
        /// </summary>
        public List<GameplayAbility> NonReplicatedInstances;
        /// <summary>
        ///  Replicated instances of this ability.
        /// </summary>
        public List<GameplayAbility> ReplicatedInstances;

        /// <summary>
        /// Object this ability was created from, can be an actor or static object. Useful to bind an ability to a gameplay object
        /// </summary>
        public WeakReference SourceObject;

        /// <summary>
        /// Level of Ability 
        /// </summary>
        public int Level;

        /// <summary>
        /// A count of the number of times this ability has been activated minus the number of times it has been ended.
        /// For instanced abilities this will be the number of currently active instances.
        /// Can't replicate until prediction accurately handles this.
        /// </summary>
        public uint ActiveCount;

        /// <summary>
        /// If true, this ability should be removed as soon as it finishes executing
        /// </summary>
        public bool RemoveAfterActivation;

        /// <summary>
        /// Pending removal due to scope lock
        /// </summary>
        public bool PendingRemove;

        /// <summary>
        ///  This ability should be activated once when it is granted.
        /// </summary>
        public bool IsActivateOnce;

        /// <summary>
        /// Cached GameplayEventData if this ability was pending for add and activate due to scope lock
        /// </summary>
        public GameplayEventData GameplayEventData;

        /// <summary>
        /// Activation state of this ability. This is not replicated since it needs to be overwritten locally on clients during prediction.
        /// </summary>
        public GameplayAbilityActivationInfo ActivationInfo;

        /// <summary>
        /// Optional ability tags that are replicated.
        /// These tags are also captured as source tags by applied gameplay effects.
        /// </summary>
        public GameplayTagContainer DynamicAbilityTags;

        /// <summary>
        /// Handle to GE that granted us (usually invalid). FActiveGameplayEffectHandles are not synced across the network and this is valid only on Authority.
        /// If you need FGameplayAbilitySpec -> FActiveGameplayEffectHandle, then use AbilitySystemComponent::FindActiveGameplayEffectHandle.
        /// </summary>
        public ActiveGameplayEffectHandle GameplayEffectHandle;

        /// <summary>
        /// Passed on SetByCaller magnitudes if this ability was granted by a GE
        /// </summary>
        public Dictionary<GameplayTag, float> SetByCallerTagMagnitudes;
        
        public bool IsValid()
        {
            return Handle.IsValid();
        }
        
        public bool ShouldReplicateAbilitySpec()
        {
            if (Ability != null && Ability.ShouldReplicateAbilitySpec(ref this))
            {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Returns the primary instance, used for instance once abilities
        /// </summary>
        /// <returns></returns>
        public GameplayAbility GetPrimaryInstance()
        {
            if (Ability != null && Ability.GetInstancingPolicy() == EGameplayAbilityInstancingPolicy.InstancedPerActor)
            {
                if (NonReplicatedInstances.Count > 0)
                {
                    return NonReplicatedInstances[0];
                }

                if (ReplicatedInstances.Count > 0)
                {
                    return ReplicatedInstances[0];
                }
            }

            return null;
        }
        
        /// <summary>
        /// Returns true if this ability is active in any way
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return Ability != null && ActiveCount > 0;
        }
    }

    public class GameplayAbilitySpecContainer
    {
        public List<GameplayAbilitySpec> Items;
        public AbilitySystemComponent Owner;
    }
    
    public struct ScopedAbilityListLock : IDisposable
    {
        public ScopedAbilityListLock(AbilitySystemComponent abilitySystem)
        {
            _abilitySystemComponent = abilitySystem;
            _abilitySystemComponent.IncrementAbilityListLock();
        }
        private AbilitySystemComponent _abilitySystemComponent;
        public void Dispose()
        {
            if (_abilitySystemComponent != null)
            {
                _abilitySystemComponent.DecrementAbilityListLock();
            }
            _abilitySystemComponent = null;
        }
    }
    
    public struct ScopedTargetListLock : IDisposable
    {
        public ScopedTargetListLock(AbilitySystemComponent abilitySystem, GameplayAbility gameplayAbility)
        {
            _gameplayAbility =  gameplayAbility;
            _abilityListLock = new ScopedAbilityListLock(abilitySystem);
            _gameplayAbility.IncrementListLock();
        }
        private ScopedAbilityListLock _abilityListLock;
        private GameplayAbility _gameplayAbility;
        public void Dispose()
        {
            _gameplayAbility.DecrementListLock();
            _abilityListLock.Dispose();
            _gameplayAbility = null;
        }
    }
}