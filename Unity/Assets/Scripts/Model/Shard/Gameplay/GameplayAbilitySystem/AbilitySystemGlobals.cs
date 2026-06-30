using System;
using System.Collections.Generic;
using Bliss.Base;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /// <summary>
    /// Holds global data for the ability system.
    /// </summary>
    public class AbilitySystemGlobals : Singleton<AbilitySystemGlobals>
    {
        /// <summary>
        /// Ignore cooldowns for all Gameplay Abilities.
        /// </summary>
        public static bool bIgnoreAbilitySystemCooldowns = false;
        
        /// <summary>
        /// Ignore costs for all Gameplay Abilities.
        /// </summary>
        public static bool bIgnoreAbilitySystemCosts = false;

        /// <summary>
        /// Returns true if ability cooldowns are ignored, returns false otherwise. Always returns false in shipping builds.
        /// </summary>
        /// <returns></returns>
        public bool ShouldIgnoreCooldowns()
        {
            return bIgnoreAbilitySystemCooldowns;
        }
        
        /// <summary>
        /// Returns true if ability costs are ignored, returns false otherwise. Always returns false in shipping builds.
        /// </summary>
        /// <returns></returns>
        public bool ShouldIgnoreCosts()
        {
            return bIgnoreAbilitySystemCosts;
        }

        // Global Tags
        
        /// <summary>
        /// TryActivate failed due to being dead
        /// </summary>
        public GameplayTag ActivateFailIsDeadTag;
        public string ActivateFailIsDeadName;
        
        /// <summary>
        /// TryActivate failed due to being on cooldown
        /// </summary>
        public GameplayTag ActivateFailCooldownTag;
        public string ActivateFailCooldownName;

        /// <summary>
        ///  TryActivate failed due to not being able to spend costs
        /// </summary>
        public GameplayTag ActivateFailCostTag;
        public string ActivateFailCostName;

        
        /// <summary>
        ///  TryActivate failed due to being blocked by other abilities
        /// </summary>
        public GameplayTag ActivateFailTagsBlockedTag;
        public string ActivateFailTagsBlockedName;
        
        /// <summary>
        ///  TryActivate failed due to missing required tags
        /// </summary>
        public GameplayTag ActivateFailTagsMissingTag;
        public string ActivateFailTagsMissingName;
        
        /// <summary>
        ///  Failed to activate due to invalid networking settings, this is designer error
        /// </summary>
        public GameplayTag ActivateFailNetworkingTag;
        public string ActivateFailNetworkingName;

        /// <summary>
        /// Set to true if you want clients to try to predict gameplay effects done to targets. If false it will only predict self effects
        /// </summary>
        public bool PredictTargetGameplayEffects = true;

        /// <summary>
        /// Set to true if you want tags granted to owners from ability activations to be replicated. If false, ActivationOwnedTags are only applied locally. 
        /// This should only be disabled for legacy game code that depends on non-replication of ActivationOwnedTags.
        /// </summary>
        public bool ReplicateActivationOwnedTags = true;
        
        /// <summary>
        /// Returns true if the ability system should try to predict gameplay effects applied to non local targets
        /// </summary>
        /// <returns></returns>
        public bool ShouldPredictTargetGameplayEffects()
        {
            return PredictTargetGameplayEffects;
        }

        /// <summary>
        /// Returns true if tags granted to owners from ability activations should be replicated
        /// </summary>
        /// <returns></returns>
        public bool ShouldReplicateActivationOwnedTags()
        {
            return ReplicateActivationOwnedTags;
        }
        
        
        private Dictionary<Type, object> _gameplayClassDefaultObjects = new Dictionary<Type, object>();

        public object GetDefaultObject(SubClassOf<GamePlayObject> type)
        {
            throw new NotImplementedException();
            //TODO: Create CDO
        }


        public GameplayCueManager GetGameplayCueManager()
        {
            throw new NotImplementedException();
        }

        public virtual void PushCurrentAppliedGE(ref GameplayEffectSpec effectSpec, AbilitySystemComponent abilitySystem)
        {
            
        }

        public virtual void SetCurrentAppliedGE(ref GameplayEffectSpec effectSpec)
        {
            
        }
        
        public virtual void PopCurrentAppliedGE()
        {
            
        }

        /// <summary>
        /// Global callback that can handle game-specific code that needs to run before applying a gameplay effect spec
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="abilitySystem"></param>
        public virtual void GlobalPreGameplayEffectSpecApply(ref GameplayEffectSpec spec, AbilitySystemComponent abilitySystem)
        {
            
        }
    }

    public struct ScopedCanActivateAbilityLogEnabler : System.IDisposable
    {
        private static int LogEnablerCounter;

        public ScopedCanActivateAbilityLogEnabler(byte _ = 0)
        {
            ++LogEnablerCounter;
        }

        public static bool IsLoggingEnabled()
        {
            return LogEnablerCounter > 0;
        }

        public void Dispose()
        {
            --LogEnablerCounter;
        }
    }
}