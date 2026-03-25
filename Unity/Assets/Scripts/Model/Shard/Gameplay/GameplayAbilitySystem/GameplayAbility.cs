using System;
using System.Collections.Generic;
using Bliss.Base;
using Bliss.Base.Collections;
using Bliss.Base.Event;
using Bliss.Gameplay.GameplayTags;
using Bliss.Gameplay.GameplayTasks;
using Mono.CSharp;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /// <summary>
    /// Ability 数据模版
    /// </summary>
    public abstract class GameplayAbility
    {
        protected EGameplayAbilityReplicationPolicy ReplicationPolicy;
        protected EGameplayAbilityInstancingPolicy InstancingPolicy;
        protected EGameplayAbilityNetExecutionPolicy NetExecutionPolicy;

        /// <summary>
        ///  This is shared, cached information about the thing using us
        ///	 E.g, Actor*, MovementComponent*, AnimInstance, etc.
        ///	 This is hopefully allocated once per actor and shared by many abilities.
        ///	 The actual struct may be overridden per game to include game specific data.
        ///	 (E.g, child classes may want to cast to FMyGameAbilityActorInfo)
        /// </summary>
        protected GameplayAbilityActorInfo CurrentActorInfo;
        
        /// <summary>
        /// For instanced abilities 
        /// </summary>
        protected GameplayAbilitySpecHandle CurrentSpecHandle;

        /// <summary>
        /// GameplayCues that were added during this ability that will get automatically removed when it ends
        /// </summary>
        protected HashSet<GameplayTag> TrackedGameplayCues;
        
        /// <summary>
        /// True if the ability is currently active. For instance per owner abilities
        /// </summary>
        protected bool bIsActive;
        
        /// <summary>
        /// True if the end ability has been called, but has not yet completed.
        /// </summary>
        protected bool bIsAbilityEnding = false;
        
        /// True if the ability is currently cancelable, if not will only be canceled by hard EndAbility calls 
        /// </summary>
        protected bool bIsCancelable;
        
        /// <summary>
        /// rue if the ability block flags are currently enabled
        /// </summary>
        protected bool bIsBlockingOtherAbilities;

        /// <summary>
        ///  A count of all the current scope locks.
        /// </summary>
        protected byte ScopeLockCount;

        /// <summary>
        /// A list of all the functions waiting for the scope lock to end so they can run.
        /// </summary>
        protected List<GameplayPostLockEvent> WaitingToExecute;
        
        /// <summary>
        /// This is information specific to this instance of the ability. E.g, whether it is predicting, authoring, confirmed, etc.
        /// </summary>
        protected GameplayAbilityActivationInfo	CurrentActivationInfo;
        
        /// <summary>
        /// Information specific to this instance of the ability, if it was activated by an event
        /// </summary>
        protected GameplayEventData CurrentEventData;

        /// <summary>
        /// This GameplayEffect represents the cost (mana, stamina, etc) of the ability. It will be applied when the ability is committed.
        /// </summary>
        protected GameplayEffect CostGameplayEffectClass;
        
        /// <summary>
        /// This GameplayEffect represents the cooldown. It will be applied when the ability is committed and the ability cannot be used again until it is expired.
        /// </summary>
        protected GameplayEffect CooldownGameplayEffectClass;

        /// <summary>
        /// Triggers to determine if this ability should execute in response to an event 
        /// </summary>
        protected internal List<AbilityTriggerData> AbilityTriggers;

        /// <summary>
        /// Notification that the ability has ended.  Set using TryActivateAbility.
        /// </summary>
        public AutoEventAction<GameplayAbility> OnGameplayAbilityEnded;
        
        /// <summary>
        /// Notification that the ability has ended with data on how it was ended
        /// </summary>
        public AutoEventAction<AbilityEndedData> OnGameplayAbilityEndedWithData;
        
        /// <summary>
        /// Notification that the ability is being cancelled.  Called before OnGameplayAbilityEnded.
        /// </summary>
        public AutoEventAction OnGameplayAbilityCancelled;
        
        /// <summary>
        /// Used by the ability state task to handle when a state is ended
        /// </summary>
        public AutoEventAction<string> OnGameplayAbilityStateEnded;

        /// <summary>
        /// Callback for when this ability has been confirmed by the server
        /// </summary>
        public AutoEventAction OnConfirmDelegate;

        #region Ability Levels and source objects

        /// <summary>
        /// Returns current level of the Ability
        /// </summary>
        /// <returns></returns>
        public int GetAbilityLevel()
        {
            if (IsInstantiated() == false || CurrentActorInfo.IsValid())
            {
                return 1;
            }

            return GetAbilityLevel(CurrentSpecHandle, ref CurrentActorInfo);
        }

        /// <summary>
        ///  Returns current ability level for non instanced abilities. You must call this version in these contexts! 
        /// </summary>
        /// <param name="abilitySpecHandle"></param>
        /// <param name="actorInfo"></param>
        /// <returns></returns>
        public int GetAbilityLevel(GameplayAbilitySpecHandle abilitySpecHandle, ref GameplayAbilityActorInfo actorInfo)
        {
            Assert.Check(actorInfo.IsValid());
            AbilitySystemComponent abilitySystemComponent = null;
            actorInfo.AbilitySystemComponent?.TryGetTarget(out abilitySystemComponent);
            
            GameplayAbilitySpec abilitySpec = default;
            abilitySystemComponent?.FindAbilitySpecFromHandle(out abilitySpec, abilitySpecHandle);

            if (abilitySpec.IsValid())
            {
                return abilitySpec.Level;
            }

            
            // An ability was probably removed/ungranted then something came in and ask about its level. Caller should have detected this earlier.
            AbilitySystemComponent.Ability_Warning($"GameplayAbility::GetAbilityLevel. Invalid AbilitySpecHandle {abilitySpecHandle} for Ability {GetType()}");
            return 1;
        }

        #endregion
        

        #region ShouldAbilityRespondToEvent

        protected virtual void SendGameplayEvent(GameplayTag eventTag, GameplayEventData payload)
        {
            
        }

        #endregion
        
        // ----------------------------------------------------------------------------------------------------------------
        //
        //	The important functions:
        //	
        //		CanActivateAbility()	- unction to see if ability is activatable. Callable by UI etc
        //
        //		TryActivateAbility()	- Attempts to activate the ability. Calls CanActivateAbility(). Input events can call this directly.
        //								- Also handles instancing-per-execution logic and replication/prediction calls.
        //		
        //		CallActivateAbility()	- Protected, non virtual function. Does some boilerplate 'pre activate' stuff, then calls ActivateAbility()
        //
        //		ActivateAbility()		- What the abilities *does*. This is what child classes want to override.
        //	
        //		CommitAbility()			- Commits reources/cooldowns etc. ActivateAbility() must call this!
        //		
        //		CancelAbility()			- Interrupts the ability (from an outside source).
        //
        //		EndAbility()			- The ability has ended. This is intended to be called by the ability to end itself.
        //	
        // ----------------------------------------------------------------------------------------------------------------

        #region Accessors

        public EGameplayAbilityInstancingPolicy GetInstancingPolicy()
        {
            return InstancingPolicy;
        }
        
        public EGameplayAbilityReplicationPolicy GetReplicationPolicy()
        {
            return ReplicationPolicy;
        }
        
        public EGameplayAbilityNetExecutionPolicy GetNetExecutionPolicy()
        {
            return NetExecutionPolicy;
        }

        public AbilitySystemComponent GetAbilitySystemComponentFromActorInfo()
        {
            if (CurrentActorInfo.AbilitySystemComponent == null)
            {
                return null;
            }
            
            CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystem);
            return abilitySystem;
        }
        
        public AbilitySystemComponent GetAbilitySystemComponentFromActorInfo_Checked()
        {
            AbilitySystemComponent abilitySystemComponent = null;
            CurrentActorInfo.AbilitySystemComponent?.TryGetTarget(out abilitySystemComponent);
            if (abilitySystemComponent == null)
            {
                throw new System.Exception($"abilitySystemComponent must not be null");
            }
            return abilitySystemComponent;
        }
        
        public AbilitySystemComponent GetAbilitySystemComponentFromActorInfo_Ensure()
        {
            AbilitySystemComponent abilitySystemComponent = null;
            CurrentActorInfo.AbilitySystemComponent?.TryGetTarget(out abilitySystemComponent);
            if (abilitySystemComponent == null)
            {
                AbilitySystemComponent.Ability_Log($"abilitySystemComponent must not be null \n {new System.Diagnostics.StackTrace()}");
            }
            return abilitySystemComponent;
        }

        /// <summary>
        /// Gets the current actor info bound to this ability - can only be called on instanced abilities.
        /// </summary>
        /// <returns></returns>
        public GameplayAbilityActorInfo GetCurrentActorInfo()
        {
            if (!EnsureAbilityIsInstanced())
            {
                return default;
            }
            
            return CurrentActorInfo;
        }

        /// <summary>
        /// Gets the current activation info bound to this ability - can only be called on instanced abilities.
        /// </summary>
        /// <returns></returns>
        public GameplayAbilityActivationInfo GetCurrentActivationInfo()
        {
            if (!EnsureAbilityIsInstanced())
            {
                return default;
            }
            
            return CurrentActivationInfo;
        }

        /// <summary>
        ///  Gets the current AbilitySpecHandle- can only be called on instanced abilities.
        /// </summary>
        /// <returns></returns>
        public GameplayAbilitySpecHandle GetCurrentAbilitySpecHandle()
        {
            if (!EnsureAbilityIsInstanced())
            {
                return default;
            }
            
            return CurrentSpecHandle;
        }

        /// <summary>
        /// Retrieves the actual AbilitySpec for this ability. Can only be called on instanced abilities. 
        /// </summary>
        /// <returns></returns>
        public GameplayAbilitySpec GetCurrentAbilitySpec()
        {
            if (!EnsureAbilityIsInstanced())
            {
                return default;
            }

            if (!CurrentActorInfo.IsValid())
            {
                throw new System.Exception($"abilitySpec must not be null");
            }
            
            AbilitySystemComponent abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Checked();
            abilitySystemComponent.FindAbilitySpecFromHandle(out var spec, CurrentSpecHandle);
            return spec;
        }
        
        /// <summary>
        /// Retrieves the EffectContext of the GameplayEffect that granted this ability. Can only be called on instanced abilities.
        /// </summary>
        /// <returns></returns>
        public GameplayEffectContextHandle GetGrantedByEffectContext()
        {
            if (!EnsureAbilityIsInstanced())
            {
                return default;
            }
            
            if (!CurrentActorInfo.IsValid())
            {
                throw new System.Exception($"abilitySpec must not be null");
            }
            
            AbilitySystemComponent abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Checked();
            ActiveGameplayEffectHandle activeGEHandle =
                abilitySystemComponent.FindActiveGameplayEffectHandle(GetCurrentAbilitySpecHandle());

            if (activeGEHandle.IsValid())
            {
                return abilitySystemComponent.GetEffectContextFromActiveGEHandle(activeGEHandle);
            }
            
            return default;
        }

        /// <summary>
        /// Returns an effect context, given a specified actor info
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <returns></returns>
        public virtual GameplayEffectContextHandle MakeEffectContext(GameplayAbilitySpecHandle handle, ref GameplayAbilityActorInfo actorInfo)
        {
            GameplayEffectContextHandle contextHandle = new GameplayEffectContextHandle(new GameplayEffectContext());
            
            // By default use the owner and avatar as the instigator and causer
            contextHandle.AddInstigator((Entity)actorInfo.OwnerActor, (Entity)actorInfo.AvatarActor);
            
            // add in the ability tracking here.
            contextHandle.SetAbility(this);

            // Pass along the source object to the effect
            if (actorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent) &&
                abilitySystemComponent.FindAbilitySpecFromHandle(out GameplayAbilitySpec abilitySpec, handle))
            {
                contextHandle.AddSourceObject(abilitySpec.SourceObject.Target);
            }
            
            return contextHandle;
        }

        /// <summary>
        /// Convenience method for abilities to get outgoing gameplay effect specs (for example, to pass on to projectiles to apply to whoever they hit)
        /// </summary>
        /// <param name="gameplayEffectClass"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public virtual GameplayEffectSpecHandle MakeOutgoingGameplayEffectSpec(SubClassOf<GameplayEffect> gameplayEffectClass, float level = 1f) 
        {
            Assert.Check(CurrentActorInfo.AbilitySystemComponent != null && CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out _));
            return MakeOutgoingGameplayEffectSpec(CurrentSpecHandle, ref CurrentActorInfo, CurrentActivationInfo, gameplayEffectClass, level);
        }

        /// <summary>
        /// Convenience method for abilities to get outgoing gameplay effect specs (for example, to pass on to projectiles to apply to whoever they hit)
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <param name="gameplayEffectClass"></param>
        /// <param name="level"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual GameplayEffectSpecHandle MakeOutgoingGameplayEffectSpec(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo activationInfo,
            SubClassOf<GameplayEffect> gameplayEffectClass, 
            float level = 1f)
        {
            Assert.Check(actorInfo.IsValid());
            CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent);
            
            GameplayEffectSpecHandle newHandle = abilitySystemComponent.MakeOutgoingSpec(gameplayEffectClass, level, MakeEffectContext(handle, ref actorInfo));
            if (newHandle.IsValid())
            {
                abilitySystemComponent.FindAbilitySpecFromHandle(out GameplayAbilitySpec abilitySpec, handle);
                
                ApplyAbilityTagsToGameplayEffectSpec(ref newHandle.Data, ref abilitySpec);

                // Copy over set by caller magnitudes
                if (abilitySpec.IsValid())
                {
                    newHandle.Data.SetByCallerTagMagnitudes = abilitySpec.SetByCallerTagMagnitudes;
                }
            }
            
            return newHandle;
        }

        /// <summary>
        /// Add the Ability's tags to the given GameplayEffectSpec. This is likely to be overridden per project.
        /// </summary>
        /// <param name="effectSpec"></param>
        /// <param name="abilitySpec"></param>
        public virtual void ApplyAbilityTagsToGameplayEffectSpec(
            ref GameplayEffectSpec effectSpec,
            ref GameplayAbilitySpec abilitySpec)
        {
            GameplayTagContainer capturedSourceTags = effectSpec.CapturedSourceTags.GetSpecTags();
            capturedSourceTags.AppendTags(AbilityTags);

            if (abilitySpec.IsValid())
            {
                // Allow the source object of the ability to propagate tags along as well
                capturedSourceTags.AppendTags(abilitySpec.DynamicAbilityTags);
            
                IGameplayTagAsset sourceObjAsTagInterface = abilitySpec.SourceObject as IGameplayTagAsset;
                if (sourceObjAsTagInterface != null)
                {
                    GameplayTagContainer sourceObjectTags = new GameplayTagContainer();
                    sourceObjAsTagInterface.GetOwnedGameplayTags(sourceObjectTags);
                    
                    capturedSourceTags.AppendTags(sourceObjectTags);
                }
                
                // Copy SetByCallerMagnitudes 
                effectSpec.MergeSetByCallerMagnitudes(abilitySpec.SetByCallerTagMagnitudes);
            }
        }
        
        /** Returns true if the ability is currently active */
        public bool IsActive()
        {
            // Only Instanced-Per-Actor abilities persist between activations
            if (GetInstancingPolicy() == EGameplayAbilityInstancingPolicy.InstancedPerActor)
            {
                return bIsActive;
            }
            
            // this should not be called on NonInstanced warn about it, Should call IsActive on the ability spec instead
            if (GetInstancingPolicy() == EGameplayAbilityInstancingPolicy.NonInstanced)
            {
               AbilitySystemComponent.Ability_Warning($"UGameplayAbility::IsActive() called on {GetType()} NonInstanced ability, call IsActive on the Ability Spec instead");
            }

            // NonInstanced and Instanced-Per-Execution abilities are by definition active unless they are pending kill
            return true;
        }

        /** Is this ability triggered from TriggerData (or is it triggered explicitly through input/game code) */
        public bool IsTriggered()
        {
            // Assume that if there is triggered data, then we are triggered. 
            // If we need to support abilities that can be both, this will need to be expanded.
            return AbilityTriggers.Count > 0;
        }
        
        /// <summary>
        /// Is this ability running on a predicting client, this is false in single player
        /// </summary>
        /// <returns></returns>
        public bool IsPredictingClient()
        {
            throw new NotImplementedException();
            return false;
        }

        /// <summary>
        /// True if this is on the server and is being executed for a non-local player, false in single player
        /// </summary>
        /// <returns></returns>
        public bool IsForRemoteClient()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True if the owning actor is locally controlled, true in single player
        /// </summary>
        /// <returns></returns>
        public bool IsLocallyControlled()
        {
            throw new NotImplementedException();
        }

        public bool IsInstantiated()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True if this is the server or single player
        /// </summary>
        /// <param name="activationInfo"></param>
        /// <returns></returns>
        public bool HasAuthority(ref GameplayAbilityActivationInfo activationInfo)
        {
            return activationInfo.ActivationMode == EGameplayAbilityActivationMode.Authority;
        }
        
        /// <summary>
        /// True if we are authority or we have a valid prediciton key that is expected to work
        /// </summary>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <returns></returns>
        public bool HasAuthorityOrPredictionKey(ref GameplayAbilityActorInfo actorInfo, ref GameplayAbilityActivationInfo  activationInfo)
        {
            actorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystem);
            return abilitySystem.HasAuthorityOrPredictionKey(ref activationInfo);
        }
        
        #endregion
        
        #region CanActivateAbility

        /// <summary>
        /// Returns true if this ability can be activated right now. Has no side effects 
        /// </summary>
        /// <returns></returns>
        public virtual bool CanActivateAbility(GameplayAbilitySpecHandle handle, 
            GameplayAbilityActorInfo actorInfo,
            GameplayTagContainer sourceTags,
            GameplayTagContainer targetTags,
            GameplayTagContainer optionalRelevantTags
            )
        {
            // Don't set the actor info, CanActivate is called on the CDO

            // A valid AvatarActor is required. Simulated proxy check means only authority or autonomous proxies should be executing abilities.
            Entity actor = (Entity)actorInfo.OwnerActor;
            if (actor == null)
            {
                return false;
            }

            // make sure the ability system component is valid, if not bail out.
            if (!actorInfo.AbilitySystemComponent.TryGetTarget(out AbilitySystemComponent abilitySystemComponent))
            {
                return false;
            }

            if (!abilitySystemComponent.FindAbilitySpecFromHandle(out GameplayAbilitySpec spec, handle))
            {
                return false;
            }

            if (abilitySystemComponent.GetUserAbilityActivationInhibited())
            {
                /*
                 *	Input is inhibited (UI is pulled up, another ability may be blocking all other input, etc).
                 *	When we get into triggered abilities, we may need to better differentiate between CanActivate and CanUserActivate or something.
                 *	E.g., we would want LMB/RMB to be inhibited while the user is in the menu UI, but we wouldn't want to prevent a 'buff when I am low health'
                 *	ability to not trigger.
                 *
                 *	Basically: CanActivateAbility is only used by user activated abilities now. If triggered abilities need to check costs/cooldowns, then we may
                 *	want to split this function up and change the calling API to distinguish between 'can I initiate an ability activation' and 'can this ability be activated'.
                 */ 
                if (ScopedCanActivateAbilityLogEnabler.IsLoggingEnabled())
                {
                    AbilitySystemComponent.Ability_Log("could not be activated due to GetUserAbilityActivationInhibited");
                }
                return false;
            }

            if (!AbilitySystemGlobals.Instance.ShouldIgnoreCooldowns() && !CheckCooldown(handle, ref actorInfo, optionalRelevantTags))
            {
                if (ScopedCanActivateAbilityLogEnabler.IsLoggingEnabled())
                {
                    AbilitySystemComponent.Ability_Log("could not be activated due to Cooldown");
                }
                return false;
            }

            if (!AbilitySystemGlobals.Instance.ShouldIgnoreCosts() && !CheckCost(handle, ref actorInfo, optionalRelevantTags))
            {
                if (ScopedCanActivateAbilityLogEnabler.IsLoggingEnabled())
                {
                    AbilitySystemComponent.Ability_Log("could not be activated due to Cost");
                }
                return false;
            }
            
            // If the ability's tags are blocked, or if it has a "Blocking" tag or is missing a "Required" tag, then it can't activate.
            if(!DoesAbilitySatisfyTagRequirements(abilitySystemComponent, sourceTags, targetTags, optionalRelevantTags))
            {
                if (ScopedCanActivateAbilityLogEnabler.IsLoggingEnabled())
                {
                    AbilitySystemComponent.Ability_Log(" could not be activated due to Blocking Tags or Missing Required Tags");
                }
                return false;
            }
            
            //TODO: Check if this ability's input binding is currently blocked
            
            //TODO: bHasBlueprintCanUse
            
            return true;
        }

        /// <summary>
        /// Returns the time in seconds remaining on the currently active cooldown and the original duration for this cooldown.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="timeRemaining"></param>
        /// <param name="cooldownDuration"></param>
        public virtual void GetCooldownTimeRemainingAndDuration(ref GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            out float timeRemaining, out float cooldownDuration)
        {
            timeRemaining = 0f;
            cooldownDuration = 0f;
            GameplayTagContainer cooldownTags = GetCooldownTags();
            if (cooldownTags != null && cooldownTags.Num() > 0)
            {
                actorInfo.AbilitySystemComponent.TryGetTarget(out AbilitySystemComponent abilitySystemComponent);
                Assert.Check(abilitySystemComponent != null);

                GameplayEffectQuery query = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(cooldownTags);
                var durationAndTimeRemaining = abilitySystemComponent.GetActiveEffectsTimeRemainingAndDuration(ref query);
                
                if (durationAndTimeRemaining != null && durationAndTimeRemaining.Count > 0)
                {
                    int bestIndex = 0;
                    float longestTime = durationAndTimeRemaining[0].Item1;
                    for (int i = 1; i < durationAndTimeRemaining.Count; i++)
                    {
                        if (durationAndTimeRemaining[i].Item1 > longestTime)
                        {
                            longestTime =  durationAndTimeRemaining[i].Item1;
                            bestIndex = i;
                        }
                    }
                    
                    timeRemaining = durationAndTimeRemaining[bestIndex].Item1;
                    cooldownDuration = durationAndTimeRemaining[bestIndex].Item2;
                }
            }
        }
        
        /// <summary>
        /// Returns all tags that can put this ability into cooldown
        /// </summary>
        /// <returns></returns>
        public virtual GameplayTagContainer GetCooldownTags()
        {
            GameplayEffect cooldownGameplayEffect = GetCooldownGameplayEffect();
            return cooldownGameplayEffect?.GetGrantedTags();
        }

        private static GameplayTagContainer _abilitySystemComponentTags = new GameplayTagContainer();
        /// <summary>
        ///  Returns true if none of the ability's tags are blocked and if it doesn't have a "Blocking" tag and has all "Required" tags.
        /// </summary>
        /// <param name="abilitySystemComponent"></param>
        /// <param name="sourceTags"></param>
        /// <param name="targetTags"></param>
        /// <param name="optionalRelevantTags"></param>
        /// <returns></returns>
        public virtual bool DoesAbilitySatisfyTagRequirements(AbilitySystemComponent abilitySystemComponent,
            GameplayTagContainer sourceTags = null, 
            GameplayTagContainer targetTags = null,
            GameplayTagContainer optionalRelevantTags = null)
        {
            bool bBlocked = false;
            bool bMissing = false;

            GameplayTag blockedTag = AbilitySystemGlobals.Instance.ActivateFailTagsBlockedTag;
            GameplayTag missingTag = AbilitySystemGlobals.Instance.ActivateFailTagsMissingTag;
            
            // Check if any of this ability's tags are currently blocked
            if (abilitySystemComponent.AreAbilityTagsBlocked(AbilityTags))
            {
                bBlocked = true;
            }

            // Check to see the required/blocked tags for this ability
            if (ActivationBlockedTags.Num() > 0 || ActivationRequiredTags.Num() > 0)
            {
                _abilitySystemComponentTags.Reset();

                abilitySystemComponent.GetOwnedGameplayTags(_abilitySystemComponentTags);
                
                if(_abilitySystemComponentTags.HasAny(ActivationBlockedTags))
                {
                    bBlocked = true;
                }
                
                if(!_abilitySystemComponentTags.HasAll(ActivationRequiredTags))
                {
                    bMissing = true;
                }
            }

            if (sourceTags != null)
            {
                if (SourceBlockedTags.Num() > 0 || SourceRequiredTags.Num() > 0)
                {
                    if(sourceTags.HasAny(SourceBlockedTags))
                    {
                        bBlocked = true;
                    }
                
                    if(!sourceTags.HasAll(SourceRequiredTags))
                    {
                        bMissing = true;
                    }
                }
            }

            if (targetTags != null)
            {
                if (TargetBlockedTags.Num() > 0 || TargetRequiredTags.Num() > 0)
                {
                    if(targetTags.HasAny(TargetBlockedTags))
                    {
                        bBlocked = true;
                    }
                
                    if(!targetTags.HasAll(TargetRequiredTags))
                    {
                        bMissing = true;
                    }
                }
            }

            if (bBlocked)
            {
                if (optionalRelevantTags != null && blockedTag.IsValid())
                {
                    optionalRelevantTags.AddTag(ref blockedTag);   
                }
                return false;
            }

            if (bMissing)
            {
                if (optionalRelevantTags != null && missingTag.IsValid())
                {
                    optionalRelevantTags.AddTag(ref missingTag);   
                }
                return false;
            }
            
            return true;
        }

        public virtual bool IsBlockingOtherAbilities()
        {
            if (GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
            {
                return bIsBlockingOtherAbilities;
            }
            
            // Non instanced are always marked as blocking other abilities
            return true;
        }

        /// <summary>
        /// Sets rather ability block flags are enabled or disabled. Only valid on instanced abilities
        /// </summary>
        /// <param name="bShouldBlockAbilities"></param>
        public virtual void SetShouldBlockOtherAbilities(bool bShouldBlockAbilities)
        {
            if (bIsActive && 
                GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced &&
                bShouldBlockAbilities != bIsBlockingOtherAbilities)
            {
                bIsBlockingOtherAbilities = bShouldBlockAbilities;
                if (CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out AbilitySystemComponent abilitySystemComponent))
                {
                    abilitySystemComponent.ApplyAbilityBlockAndCancelTags(AbilityTags, this, bIsBlockingOtherAbilities, BlockAbilitiesWithTag, false, CancelAbilitiesWithTag);
                }
            }
        }
        
        
        #endregion

        #region ActivateAbility

        /// <summary>
        /// Actually activate ability, do not call this directly
        /// </summary>
        protected virtual void ActivateAbility(GameplayAbilitySpecHandle handle, 
            GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo activationInfo, 
            GameplayEventData triggerEventData)
        {
            // Native child classes should override ActivateAbility and call CommitAbility.
            // CommitAbility is used to do one last check for spending resources.
            // Previous versions of this function called CommitAbility but that prevents the callers
            // from knowing the result. Your override should call it and check the result.
            // Here is some starter code:
		
            //	if (!CommitAbility(Handle, ActorInfo, ActivationInfo))
            //	{			
            //		constexpr bool bReplicateEndAbility = true;
            //		constexpr bool bWasCancelled = true;
            //		EndAbility(Handle, ActorInfo, ActivationInfo, bReplicateEndAbility, bWasCancelled);
            //	}
        }

        /// <summary>
        /// Do boilerplate init stuff and then call ActivateAbility 
        /// </summary>
        protected virtual void PreActivate(GameplayAbilitySpecHandle handle, 
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo activationInfo, 
            Action<GameplayAbility> onGameplayAbilityEndedDelegate,
            GameplayEventData? triggerEventData = null)
        {
            actorInfo.AbilitySystemComponent.TryGetTarget(out AbilitySystemComponent comp);

            if (GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
            {
                bIsActive = true;
                bIsBlockingOtherAbilities = true;
                bIsCancelable = true;
            }
            
            RemoteInstanceEnded = false;

            // This must be called before we start applying tags and blocking or canceling other abilities.
            // We could set off a chain that results in calling functions on this ability that rely on the current info being set.
            SetCurrentInfo(handle, ref actorInfo, activationInfo);

            if (triggerEventData != null && IsInstantiated())
            {
                CurrentEventData = triggerEventData.Value;
            }
            
            comp.HandleChangeAbilityCanBeCanceled(AbilityTags, this, true);
            
            comp.AddLooseGameplayTags(ActivationOwnedTags);

            if (onGameplayAbilityEndedDelegate != null)
            {
                OnGameplayAbilityEnded += onGameplayAbilityEndedDelegate;
            }

            comp.NotifyAbilityActivated(handle, this);
            
            comp.ApplyAbilityBlockAndCancelTags(AbilityTags, this, true, BlockAbilitiesWithTag, true, CancelAbilitiesWithTag);
            
            // Spec's active count must be incremented after applying blockor cancel tags, otherwise the ability runs the risk of cancelling itself inadvertantly before it completely activates.
            if (!comp.FindAbilitySpecFromHandle(out GameplayAbilitySpec spec, handle))
            {
                AbilitySystemComponent.Ability_Warning("PreActivate called with a valid handle but no matching ability spec was found.");
                return;
            }
            
            // make sure we do not incur a roll over if we go over the uint8 max, this will need to be updated if the var size changes
            if (spec.ActiveCount < byte.MaxValue)
            {
                spec.ActiveCount++;
            }
            else
            {
                AbilitySystemComponent.Ability_Warning($"PreActivate {GetType()} called when the Spec->ActiveCount ({spec.ActiveCount}) >= UINT8_MAX");
            }
        }

        /// <summary>
        /// Executes PreActivate and ActivateAbility
        /// </summary>
        protected void CallActivateAbility(GameplayAbilitySpecHandle handle, 
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo activationInfo, 
            Action<GameplayAbility> onGameplayAbilityEndedDelegate,
            GameplayEventData? triggerEventData = null)
        {
            PreActivate(handle, ref actorInfo, activationInfo, onGameplayAbilityEndedDelegate, triggerEventData);
            ActivateAbility(handle, actorInfo, activationInfo, triggerEventData ?? default(GameplayEventData));
        }

        /// <summary>
        /// Called on a predictive ability when the server confirms its execution 
        /// </summary>
        protected virtual void ConfirmActivateSucceed()
        {
            // On instanced abilities, update CurrentActivationInfo and call any registered delegates.
            PostNetInit();
            
            CurrentActivationInfo.SetActivationConfirmed();
            
            OnConfirmDelegate?.Invoke();
            OnConfirmDelegate?.Clear();
        }
        #endregion
        
        #region CommitAbility

        public virtual bool CommitAbility(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo  activationInfo, 
            GameplayTagContainer optionalRelevantTags = null)
        {
            // Last chance to fail (maybe we no longer have resources to commit since we after we started this ability activation)
            if (!CommitCheck(handle, ref actorInfo, activationInfo, optionalRelevantTags))
            {
                return false;
            }
            
            CommitExecute(handle, ref actorInfo, activationInfo);

            // Broadcast this commitment
            actorInfo.AbilitySystemComponent.TryGetTarget(out AbilitySystemComponent comp);
            comp.NotifyAbilityCommit(this);
            
            return true;
        }
        
        public virtual bool CommitAbilityCooldown(GameplayAbilitySpecHandle handle, 
            ref GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo  activationInfo, 
            bool forceCooldown,
            GameplayTagContainer optionalRelevantTags = null)
        {
            if (!forceCooldown)
            {
                // Last chance to fail (maybe we no longer have resources to commit since we after we started this ability activation)
                if (!CheckCooldown(handle, ref actorInfo, optionalRelevantTags))
                {
                    return false;
                }
            }
            
            ApplyCooldown(handle, ref actorInfo, activationInfo);
            return true;
        }
        
        public virtual bool CommitAbilityCost(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo  activationInfo, 
            GameplayTagContainer optionalRelevantTags = null)
        {
            // Last chance to fail (maybe we no longer have resources to commit since we after we started this ability activation)
            if (!CheckCost(handle, ref actorInfo, optionalRelevantTags))
            {
                return false;
            }
            
            ApplyCost(handle, ref actorInfo, activationInfo);
            return false;
        }
        
        /// <summary>
        /// The last chance to fail before committing, this will usually be the same as CanActivateAbility. Some abilities may need to do extra checks here if they are consuming extra stuff in CommitExecute */
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <param name="optionalRelevantTags"></param>
        /// <returns></returns>
        public virtual bool CommitCheck(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo  activationInfo, 
            GameplayTagContainer optionalRelevantTags = null)
        {
            /*
             *	Checks if we can (still) commit this ability. There are some subtleties here.
             *		-An ability can start activating, play an animation, wait for a user confirmation/target data, and then actually commit
             *		-Commit = spend resources/cooldowns. It's possible the source has changed state since it started activation, so a commit may fail.
             *		-We don't want to just call CanActivateAbility() since right now that also checks things like input inhibition.
             *			-E.g., its possible the act of starting your ability makes it no longer activatable (CanActivateAbility() may be false if called here).
             */
            AbilitySystemComponent comp = null;
            bool bValidHandle = handle.IsValid();
            bool bValidActorInfoPieces = (actorInfo.AbilitySystemComponent != null && actorInfo.AbilitySystemComponent.TryGetTarget(out  comp));
            bool bValidSpecFound = bValidActorInfoPieces && comp.FindAbilitySpecFromHandle(out _, handle);
                
            // Ensure that the ability spec is even valid before trying to process the commit
            if (!bValidHandle || !bValidActorInfoPieces || !bValidSpecFound)
            {
                AbilitySystemComponent.Ability_Warning($"GameplayAbility::CommitCheck provided an invalid handle or actor info or couldn't find ability spec: {GetType()} Handle Valid: {bValidHandle} ActorInfo Valid: {bValidActorInfoPieces} Spec Not Found: {bValidSpecFound}");
                return false;
            }

            if (!CheckCooldown(handle, ref actorInfo, optionalRelevantTags))
            {
                return false;
            }

            if (!CheckCost(handle, ref actorInfo, optionalRelevantTags))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Does the commit atomically (consume resources, do cooldowns, etc)
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <returns></returns>
        public virtual void CommitExecute(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo  activationInfo)
        {
            ApplyCooldown(handle, ref actorInfo, activationInfo);
            
            ApplyCost(handle, ref actorInfo, activationInfo);
        }

        /// <summary>
        /// Returns the gameplay effect used to determine cooldown
        /// </summary>
        /// <returns></returns>
        public virtual GameplayEffect GetCooldownGameplayEffect()
        {
            return CooldownGameplayEffectClass;
        }
        
        /// <summary>
        /// Returns the gameplay effect used to apply cost
        /// </summary>
        /// <returns></returns>
        public virtual GameplayEffect GetCostGameplayEffect()
        {
            return CostGameplayEffectClass;
        }

        /// <summary>
        /// Checks cooldown. returns true if we can be used again. False if not
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="optionalRelevantTags"></param>
        /// <returns></returns>
        public virtual bool CheckCooldown(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayTagContainer optionalRelevantTags = null)
        {
            GameplayTagContainer cooldownTags = GetCooldownTags();
            if (cooldownTags != null && cooldownTags.Num() > 0)
            {
                actorInfo.AbilitySystemComponent.TryGetTarget(out AbilitySystemComponent abilitySystemComponent);
                Assert.Check(abilitySystemComponent != null);
                
                if (abilitySystemComponent.HasAnyMatchingGameplayTags(cooldownTags))
                {
                    GameplayTag cooldownFailTag = AbilitySystemGlobals.Instance.ActivateFailCooldownTag;
                    if (optionalRelevantTags != null && cooldownFailTag.IsValid())
                    {
                        optionalRelevantTags.AddTag(ref cooldownFailTag);
                    }

                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Applies CooldownGameplayEffect to the target
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <returns></returns>
        public virtual void ApplyCooldown(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo  activationInfo)
        {
            GameplayEffect cooldownEffect = GetCooldownGameplayEffect();
            if (cooldownEffect != null)
            {
                ApplyGameplayEffectToOwner(handle, ref actorInfo, activationInfo, cooldownEffect, GetAbilityLevel(handle, ref actorInfo));
            }
        }
        
        /// <summary>
        /// Checks cost. returns true if we can pay for the ability. False if not 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="optionalRelevantTags"></param>
        /// <returns></returns>
        public virtual bool CheckCost(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayTagContainer optionalRelevantTags = null)
        {
            GameplayEffect costEffect = GetCostGameplayEffect();
            if (costEffect != null)
            {
                actorInfo.AbilitySystemComponent.TryGetTarget(out AbilitySystemComponent abilitySystemComponent);
                Assert.Check(abilitySystemComponent != null);
                var effectContext = MakeEffectContext(handle, ref actorInfo);
                if (!abilitySystemComponent.CanApplyAttributeModifiers(costEffect, GetAbilityLevel(handle, ref actorInfo), ref effectContext))
                {
                    GameplayTag costFailTag = AbilitySystemGlobals.Instance.ActivateFailCostTag;
                    if (optionalRelevantTags != null && costFailTag.IsValid())
                    {
                        optionalRelevantTags.AddTag(ref costFailTag);
                    }
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Applies the ability's cost to the target
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <returns></returns>
        public virtual void ApplyCost(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo  activationInfo)
        {
            GameplayEffect costEffect = GetCostGameplayEffect();
            if (costEffect != null)
            {
                ApplyGameplayEffectToOwner(handle, ref actorInfo, activationInfo, costEffect, GetAbilityLevel(handle, ref actorInfo));
            }
        }
        
        #endregion

        #region CancelAbility
             
        /// <summary>
        /// Destroys instanced-per-execution abilities. Instance-per-actor abilities should 'reset'. Any active ability state tasks receive the 'OnAbilityStateInterrupted' event. Non instance abilities - what can we do?
        /// </summary>
        public virtual void CancelAbility(ref GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo,
            ref GameplayAbilityActivationInfo activationInfo,
            bool bReplicateCancelAbility)
        {
            if (CanBeCanceled())
            {
                if (ScopeLockCount > 0)
                {
                    return;
                }
                
                // Replicate the the server/client if needed
                if (bReplicateCancelAbility && actorInfo.AbilitySystemComponent != null &&
                    actorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent))
                {
                    abilitySystemComponent.ReplicateEndOrCancelAbility(ref handle, ref activationInfo, this, true);
                }

                // Gives the Ability BP a chance to perform custom logic/cleanup when any active ability states are active
                if (OnGameplayAbilityCancelled != null)
                {
                    OnGameplayAbilityCancelled.Invoke();
                }
                
                // End the ability but don't replicate it, we replicate the CancelAbility call directly
                bool bReplicateEndAbility = false;
                bool bWasCancelled = true;
                EndAbility(handle, ref actorInfo, ref activationInfo, bReplicateEndAbility, bWasCancelled);
            }
        }

        /// <summary>
        /// Returns true if this ability can be canceled
        /// </summary>
        /// <returns></returns>
        public virtual bool CanBeCanceled()
        {
            if (GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
            {
                return bIsCancelable;
            }
            // Non instanced are always cancelable
            return true;
        }

        /// <summary>
        /// Sets whether the ability should ignore cancel requests. Only valid on instanced abilities
        /// </summary>
        /// <param name="bCanBeCanceled"></param>
        public virtual void SetCanBeCanceled(bool bCanBeCanceled)
        {
            if (GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced && bCanBeCanceled != bIsCancelable)
            {
                bIsCancelable = bCanBeCanceled;
                
                if (CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent))
                {
                    abilitySystemComponent.HandleChangeAbilityCanBeCanceled(AbilityTags, this, bCanBeCanceled);
                }
            }
        }

        #endregion

        #region EndAbility

        /// <summary>
        /// Check if the ability can be ended
        /// </summary>
        /// <returns></returns>
        protected bool IsEndAbilityValid(ref GameplayAbilitySpecHandle handle,  GameplayAbilityActorInfo actorInfo)
        {
            // Protect against EndAbility being called multiple times
            // Ending an AbilityState may cause this to be invoked again
            if ((bIsActive == false || bIsAbilityEnding) && GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
            {
                AbilitySystemComponent.Ability_Log($"IsEndAbilityValid returning false on Ability {GetType()} due to EndAbility being called multiple times");
                return false;
            }

            // check if ability has valid owner
            AbilitySystemComponent abilityComp = null;
            actorInfo.AbilitySystemComponent?.TryGetTarget(out abilityComp);
            if (abilityComp == null)
            {
                AbilitySystemComponent.Ability_Log($"IsEndAbilityValid returning false on Ability {GetType()} due to AbilitySystemComponent being invalid");
                return false;
            }
            
            // check to see if this is an NonInstanced or if the ability is active.
            bool bIsSpecActive = false;
            if (abilityComp.FindAbilitySpecFromHandle(out var spec, handle))
            {
                bIsSpecActive = spec.IsActive();
            }
            else
            {
                bIsSpecActive = IsActive();
            }

            if (!bIsSpecActive)
            {
                AbilitySystemComponent.Ability_Log($"IsEndAbilityValid returning false on Ability {GetType()} due spec not being active");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Native function, called if an ability ends normally or abnormally. If bReplicate is set to true, try to replicate the ending to the client/server
        /// </summary>
        protected virtual void EndAbility(GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActorInfo actorInfo,
            ref GameplayAbilityActivationInfo activationInfo, 
            bool bReplicateEndAbility, bool bWasCancelled)
        {
            if (IsEndAbilityValid(ref handle, actorInfo))
            {
                if (ScopeLockCount > 0)
                {
                    WaitingToExecute.Add(GameplayPostLockEvent.Bind(EndAbility, handle, actorInfo, activationInfo, bReplicateEndAbility, bWasCancelled));
                    return;
                }

                if (GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
                {
                    bIsAbilityEnding = true;
                }
                
                //TODO: Give blueprint a chance to react
                
                // Protect against blueprint causing us to EndAbility already
                if (!bIsActive && GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
                {
                    return;
                }
                
                //TODO: Stop any timers or latent actions for the ability
                
                
                // Execute our delegate and unbind it, as we are no longer active and listeners can re-register when we become active again.
                OnGameplayAbilityEnded.Invoke(this);
                OnGameplayAbilityEnded.Clear();
                
                OnGameplayAbilityEndedWithData.Invoke(new AbilityEndedData(this, handle, bReplicateEndAbility, bWasCancelled));
                OnGameplayAbilityEndedWithData.Clear();
                
                if (GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
                {
                    bIsActive = false;
                    bIsAbilityEnding = false;
                }
                
                //TODO: Tell all our tasks that we are finished and they should cleanup

                if (actorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent))
                {
                    if (bReplicateEndAbility)
                    {
                        abilitySystemComponent.ReplicateEndOrCancelAbility(ref handle, ref activationInfo, this, false);
                    }
                    
                    // Remove tags we added to owner
                    abilitySystemComponent.RemoveLooseGameplayTags(ActivationOwnedTags);
                    
                    if (AbilitySystemGlobals.Instance.ShouldReplicateActivationOwnedTags())
                    {
                        abilitySystemComponent.RemoveReplicatedLooseGameplayTags(ActivationOwnedTags);
                    }
                    
                    // Remove tracked GameplayCues that we added
                    foreach (var gameplayCueTag in TrackedGameplayCues)
                    {
                        abilitySystemComponent.RemoveGameplayCue(gameplayCueTag);
                    }
                    TrackedGameplayCues.Clear();

                    if (CanBeCanceled())
                    {
                        // If we're still cancelable, cancel it now
                        abilitySystemComponent.HandleChangeAbilityCanBeCanceled(AbilityTags, this, false);
                    }

                    if (IsBlockingOtherAbilities())
                    {
                        // If we're still blocking other abilities, cancel now
                        abilitySystemComponent.ApplyAbilityBlockAndCancelTags(AbilityTags, this, false, BlockAbilitiesWithTag, false, CancelAbilitiesWithTag);
                    }

                    // Tell owning AbilitySystemComponent that we ended so it can do stuff (including MarkPendingKill us)
                    abilitySystemComponent.ClearAbilityReplicatedDataCache(handle, ref activationInfo);
                }

                if (IsInstantiated())
                {
                    CurrentEventData = new GameplayEventData();
                }
            }
        }


        #endregion

        #region  Apply Gameplay effects to Self

        /// <summary>
        /// 
        /// </summary>
        /// <param name="specHandle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <param name="gameplayEffect"></param>
        /// <param name="gameplayEffectLevel"></param>
        /// <param name="stacks"></param>
        /// <returns></returns>
        protected ActiveGameplayEffectHandle ApplyGameplayEffectToOwner(GameplayAbilitySpecHandle  specHandle,
            ref GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEffect gameplayEffect,
            int gameplayEffectLevel,
            int stacks = 1)
        {
            if (gameplayEffect != null && HasAuthorityOrPredictionKey(ref actorInfo, ref activationInfo))
            {
                GameplayEffectSpecHandle effectSpecHandle = MakeOutgoingGameplayEffectSpec(specHandle, ref actorInfo, activationInfo, gameplayEffect.GetType(), gameplayEffectLevel);
                if (effectSpecHandle.IsValid())
                {
                    effectSpecHandle.Data.SetStackCount(stacks);
                    return ApplyGameplayEffectToOwner(specHandle, ref actorInfo, activationInfo, gameplayEffect, gameplayEffectLevel, stacks);
                }
            }

            // We cannot apply GameplayEffects in this context. Return an empty handle.
            return new ActiveGameplayEffectHandle();
        }
        
        /// <summary>
        /// Apply a previously created gameplay effect spec to the owner of this ability 
        /// </summary>
        /// <param name="abilitySpecHandle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <param name="specHandle"></param>
        protected ActiveGameplayEffectHandle ApplyGameplayEffectSpecToOwner(GameplayAbilitySpecHandle abilitySpecHandle,
            ref GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEffectSpecHandle specHandle)
        {
            // This batches all created cues together
            
            if (specHandle.IsValid() && HasAuthorityOrPredictionKey(ref actorInfo, ref activationInfo))
            {
                actorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent);
                return abilitySystemComponent.ApplyGameplayEffectSpecToSelf(specHandle.Data, abilitySystemComponent.GetPredictionKeyForNewAction());
            }
            return new ActiveGameplayEffectHandle();
        }

        #endregion

        #region Apply Gameplay effects to Target

        /// <summary>
        /// Non blueprintcallable, safe to call on CDO/NonInstance abilities
        /// </summary>
        protected List<ActiveGameplayEffectHandle> ApplyGameplayEffectToTarget(GameplayAbilitySpecHandle abilitySpecHandle,
            ref GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            ref GameplayAbilityTargetDataHandle targetDataHandle,
            SubClassOf<GameplayEffect> gameplayEffectClass,
            float gameEffectLevel,
            int stacks)
        {
            List<ActiveGameplayEffectHandle> effectHandles =  new List<ActiveGameplayEffectHandle>();
            if (!HasAuthority(ref activationInfo)
                && !AbilitySystemGlobals.Instance.ShouldPredictTargetGameplayEffects())
            {
                // Early out to avoid making effect specs that we can't apply
                return effectHandles;
            }

            // This batches all created cues together
            using var GameplayCueSendContext = ScopedGameplayCueSendContext.StartScope();
            if (gameplayEffectClass == null)
            {
                AbilitySystemComponent.Ability_Error($"ApplyGameplayEffectToTarget called on ability {GetType()} with no GameplayEffect.");
            }
            else if(HasAuthorityOrPredictionKey(ref actorInfo, ref activationInfo))
            {
                GameplayEffectSpecHandle specHandle = MakeOutgoingGameplayEffectSpec(abilitySpecHandle, ref actorInfo, activationInfo, gameplayEffectClass, gameEffectLevel);
                if (specHandle.IsValid())
                {
                    specHandle.Data.SetStackCount(stacks);
                    effectHandles.AddRange(ApplyGameplayEffectToTarget(abilitySpecHandle, ref actorInfo, activationInfo, specHandle, ref targetDataHandle));
                }
            }

            return effectHandles;
        }
        
        /// <summary>
        /// Apply a previously created gameplay effect spec to a target
        /// </summary>
        /// <param name="abilitySpecHandle"></param>
        /// <param name="actorInfo"></param>
        /// <param name="activationInfo"></param>
        /// <param name="specHandle"></param>
        /// <param name="targetDataHandle"></param>
        /// <returns></returns>
        protected List<ActiveGameplayEffectHandle> ApplyGameplayEffectToTarget(GameplayAbilitySpecHandle abilitySpecHandle,
            ref GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEffectSpecHandle specHandle,
            ref GameplayAbilityTargetDataHandle targetDataHandle)
        {
            List<ActiveGameplayEffectHandle> effectHandles =  new List<ActiveGameplayEffectHandle>();
            if (specHandle.IsValid() && HasAuthorityOrPredictionKey(ref actorInfo, ref activationInfo))
            {
                actorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent);
                using var _ = new ScopedTargetListLock(abilitySystemComponent, this);
                foreach (var targetData in targetDataHandle.Data)
                {
                    if (targetData != null)
                    {
                        effectHandles.AddRange(targetData.ApplyGameplayEffectSpec(ref specHandle.Data, abilitySystemComponent.GetPredictionKeyForNewAction()));
                    }
                    else
                    {
                        AbilitySystemComponent.Ability_Warning("GameplayAbility::ApplyGameplayEffectSpecToTarget invalid target data passed in. Ability");
                    }
                }
            }
            
            return effectHandles;
        }

        #endregion

        #region Remove Gameplay effects from Self

        /// <summary>
        /// Removes GameplayEffects from owner which match the given asset level tags
        /// </summary>
        /// <param name="withAssetTags"></param>
        /// <param name="stacksToRemove"></param>
        protected void RemoveGameplayEffectFromOwnerWithAssetTags(GameplayTagContainer withAssetTags, int stacksToRemove = -1)
        {
            if (HasAuthority(ref CurrentActivationInfo) == false)
            {
                return;
            }

            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            if (abilitySystemComponent != null)
            {
                var query = GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(withAssetTags);
                abilitySystemComponent.RemoveActiveEffects(ref query, stacksToRemove);
            }
        }

        /// <summary>
        /// Removes GameplayEffects from owner which grant the given tags
        /// </summary>
        /// <param name="withGrantedTags"></param>
        /// <param name="stacksToRemove"></param>
        protected void RemoveGameplayEffectFromOwnerWithGrantedTags(GameplayTagContainer withGrantedTags, int stacksToRemove = -1)
        {
            if (HasAuthority(ref CurrentActivationInfo) == false)
            {
                return;
            }

            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            if (abilitySystemComponent != null)
            {
                var query = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(withGrantedTags);
                abilitySystemComponent.RemoveActiveEffects(ref query, stacksToRemove);
            }
        }

        /// <summary>
        /// Removes GameplayEffect from owner that match the given handle
        /// </summary>
        protected void RemoveGameplayEffectFromOwnerWithHandle(ActiveGameplayEffectHandle handle, int stacksToRemove = -1)
        {
            if (HasAuthority(ref CurrentActivationInfo) == false)
            {
                return;
            }

            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.RemoveActiveGameplayEffect(handle, stacksToRemove);
            }
        }

        #endregion

        #region GameplayCue
        //Abilities can invoke GameplayCues without having to create GameplayEffects

        /// <summary>
        /// Invoke a gameplay cue on the ability owner
        /// </summary>
        protected virtual void ExecuteGameplayCue(GameplayTag gameplayCueTag, GameplayEffectContextHandle context)
        {
            Assert.Check(CurrentActorInfo.IsValid());

            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            abilitySystemComponent.ExecuteGameplayCue(gameplayCueTag, context);
        }

        /// <summary>
        /// Invoke a gameplay cue on the ability owner
        /// </summary>
        protected virtual void ExecuteGameplayCueWithParams(GameplayTag gameplayCueTag, ref GameplayCueParameters gameplayCueParams)
        {
            Assert.Check(CurrentActorInfo.IsValid());

            gameplayCueParams.AbilityLevel = GetAbilityLevel();
            
            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            abilitySystemComponent.ExecuteGameplayCue(gameplayCueTag, ref gameplayCueParams);
        }

        /// <summary>
        /// Adds a persistent gameplay cue to the ability owner. Optionally will remove if ability ends
        /// </summary>
        protected virtual void AddGameplayCue(GameplayTag gameplayCueTag,  GameplayEffectContextHandle context, bool bRemoveOnAbilityEnd = true)
        {
            Assert.Check(CurrentActorInfo.IsValid());
          
            // Make default context if nothing is passed in
            if (!context.IsValid())
            {
                context = MakeEffectContext(CurrentSpecHandle, ref CurrentActorInfo);
            }
            
            context.SetAbility(this);
            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            abilitySystemComponent.AddGameplayCue(gameplayCueTag, context);

            if (bRemoveOnAbilityEnd)
            {
                TrackedGameplayCues.Add(gameplayCueTag);
            }
        }

        /// <summary>
        /// Adds a persistent gameplay cue to the ability owner. Optionally will remove if ability ends
        /// </summary>
        protected virtual void AddGameplayCueWithParams(GameplayTag gameplayCueTag, ref GameplayCueParameters gameplayCueParams, bool bRemoveOnAbilityEnd = true)
        {
            Assert.Check(CurrentActorInfo.IsValid());
            
            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            abilitySystemComponent.AddGameplayCue(gameplayCueTag, ref gameplayCueParams);

            if (bRemoveOnAbilityEnd)
            {
                TrackedGameplayCues.Add(gameplayCueTag);
            }
        }

        /// <summary>
        /// Removes a persistent gameplay cue from the ability owner
        /// </summary>
        protected virtual void RemoveGameplayCue(GameplayTag gameplayCueTag)
        {
            Assert.Check(CurrentActorInfo.IsValid());
            
            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            abilitySystemComponent.RemoveGameplayCue(gameplayCueTag);
            
            TrackedGameplayCues.Remove(gameplayCueTag);
        }

        #endregion

        #region Ability exclusion / canceling
        
        /// <summary>
        /// This ability has these tags
        /// </summary>
        public GameplayTagContainer AbilityTags;
        
        /// <summary>
        /// Set when the remote instance of this ability has ended (but the local instance may still be running or finishing up
        /// </summary>
        public bool RemoteInstanceEnded;

        /// <summary>
        /// Abilities with these tags are cancelled when this ability is executed
        /// </summary>
        protected GameplayTagContainer CancelAbilitiesWithTag;
        
        /// <summary>
        ///  Abilities with these tags are blocked while this ability is active
        /// </summary>
        protected GameplayTagContainer BlockAbilitiesWithTag;
        
        /// <summary>
        /// Tags to apply to activating owner while this ability is active. These are replicated if ReplicateActivationOwnedTags is enabled in AbilitySystemGlobals.
        /// </summary>
        protected GameplayTagContainer ActivationOwnedTags;
        
        /// <summary>
        /// This ability can only be activated if the activating actor/component has all of these tags
        /// </summary>
        protected GameplayTagContainer ActivationRequiredTags;
        
        /// <summary>
        ///  This ability is blocked if the activating actor/component has any of these tags
        /// </summary>
        protected GameplayTagContainer ActivationBlockedTags;
        
        /// <summary>
        /// This ability can only be activated if the source actor/component has all of these tags
        /// </summary>
        protected GameplayTagContainer SourceRequiredTags;
        
        /// <summary>
        /// This ability is blocked if the source actor/component has any of these tags
        /// </summary>
        protected GameplayTagContainer SourceBlockedTags;
        
        /// <summary>
        /// This ability can only be activated if the target actor/component has all of these tags
        /// </summary>
        protected GameplayTagContainer TargetRequiredTags;
        
        /// <summary>
        /// This ability is blocked if the target actor/component has any of these tags
        /// </summary>
        protected GameplayTagContainer TargetBlockedTags;
        

        #endregion


        #region Ability Tasks

        /// <summary>
        /// Finds all currently active tasks named InstanceName and confirms them. What this means depends on the individual task. By default, this does nothing other than ending if bEndTask is true.
        /// </summary>
        protected void ConfirmTaskByInstanceName(string instanceName, bool bEndTask)
        {
            using ListComponent<GameplayTask> namedTasks =  new ListComponent<GameplayTask>();
            foreach (var task in ActiveTasks)
            {
                if (task != null && task.GetInstanceName() == instanceName)
                {
                    namedTasks.Add(task);
                }
            }

            for (int i = namedTasks.Count - 1; i >= 0; i--)
            {
                GameplayTask currentTask = namedTasks[i];
                if (currentTask != null)
                {
                    currentTask.ExternalConfirm(bEndTask);
                }
            }
        }

        protected void EndOrCancelTasksByInstanceName()
        {
            using ListComponent<GameplayTask> namedTasks =  new ListComponent<GameplayTask>();
            
            // Call Endtask on everything in EndTaskInstanceNames list
            for (int i = 0; i < EndTaskInstanceNames.Count; i++)
            {
                string instanceName = EndTaskInstanceNames[i];
                namedTasks.Clear();
                
                // Find every current task that needs to end before ending any
                foreach (var task in ActiveTasks)
                {
                    if (task != null && task.GetInstanceName() == instanceName)
                    {
                        namedTasks.Add(task);
                    }
                }

                for (int j = namedTasks.Count - 1; j >= 0; j--)
                {
                    GameplayTask currentTask = namedTasks[j];
                    if (currentTask != null)
                    {
                        currentTask.EndTask();
                    }
                }
            }
            EndTaskInstanceNames.Clear();
            
            // Call ExternalCancel on everything in CancelTaskInstanceNames list
            for (int i = 0; i < CancelTaskInstanceNames.Count; i++)
            {
                string instanceName = CancelTaskInstanceNames[i];
                namedTasks.Clear();
                
                // Find every current task that needs to cancel before cancelling any
                foreach (var task in ActiveTasks)
                {
                    if (task != null && task.GetInstanceName() == instanceName)
                    {
                        namedTasks.Add(task);
                    }
                }

                for (int j = namedTasks.Count - 1; j >= 0; j--)
                {
                    GameplayTask currentTask = namedTasks[j];
                    if (currentTask != null)
                    {
                        currentTask.ExternalCancel();
                    }
                }
            }
            CancelTaskInstanceNames.Clear();
        }

        /// <summary>
        ///  Add any task with this instance name to a list to be ended (not canceled) next frame.  See also CancelTaskByInstanceName.
        /// </summary>
        /// <param name="instanceName"></param>
        protected void EndTaskByInstanceName(string instanceName)
        {
            //Avoid race condition by delaying for one frame
            if (!EndTaskInstanceNames.Contains(instanceName))
            {
                EndTaskInstanceNames.Add(instanceName);
            }
            
            //GetWorld()->GetTimerManager().SetTimerForNextTick(this, &UGameplayAbility::EndOrCancelTasksByInstanceName);
        }

        /// <summary>
        /// Add any task with this instance name to a list to be canceled (not ended) next frame.  See also EndTaskByInstanceName.
        /// </summary>
        /// <param name="instanceName"></param>
        protected void CancelTaskByInstanceName(string  instanceName)
        {
            if (!CancelTaskInstanceNames.Contains(instanceName))
            {
                CancelTaskInstanceNames.Add(instanceName);
            }
            //GetWorld()->GetTimerManager().SetTimerForNextTick(this, &UGameplayAbility::EndOrCancelTasksByInstanceName);
        }

        /// <summary>
        /// Ends any active ability state task with the given name. If name is 'None' all active states will be ended (in an arbitrary order).
        /// </summary>
        /// <param name="optionalStateNameToEnd"></param>
        protected void EndAbilityState(string optionalStateNameToEnd)
        {
            Assert.Check(CurrentActorInfo.IsValid());
            if (OnGameplayAbilityStateEnded != null)
            {
                OnGameplayAbilityStateEnded.Invoke(optionalStateNameToEnd);
            }
        }
        

        /// <summary>
        /// Internal function, cancels all the tasks we asked to cancel last frame (by instance name).
        /// </summary>
        protected List<string> CancelTaskInstanceNames;
        
        /// <summary>
        /// 
        /// </summary>
        protected List<string> EndTaskInstanceNames;
        
        /// <summary>
        /// List of currently active tasks, do not modify directly
        /// </summary>
        protected List<GameplayTask> ActiveTasks;

        #endregion

        #region Interaction with ability system component

        /// <summary>
        /// Called by ability system component to inform this ability instance the remote instance was ended
        /// </summary>
        public virtual void SetRemoteInstanceHasEnded()
        {
            // This could potentially happen in shutdown corner cases
            if (!CurrentActorInfo.IsValid())
            {
                return;
            }
            
            CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent);
            if (abilitySystemComponent == null)
            {
                return;
            }

            RemoteInstanceEnded = true;
            foreach (var task in ActiveTasks)
            {
                if (task != null && task.IsWaitingOnRemotePlayerdata())
                {
                    // We have a task that is waiting for player input, but the remote player has ended the ability, so it will not send the input.
                    // Kill the ability to avoid getting stuck active.
                    abilitySystemComponent.ForceCancelAbilityDueToReplication(this);
                    break;
                }
            }
        }

        /// <summary>
        ///  Called to inform the ability that the AvatarActor has been replaced. If the ability is dependent on avatar state, it may want to end itself. */
        /// </summary>
        public virtual void NotifyAvatarDestroyed()
        {
            // This could potentially happen in shutdown corner cases
            if (!CurrentActorInfo.IsValid())
            {
                return;
            }
            
            CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent);
            if (abilitySystemComponent == null)
            {
                return;
            }

            RemoteInstanceEnded = true;
            foreach (var task in ActiveTasks)
            {
                if (task != null && task.IsWaitingOnAvatar())
                {
                    // We have a task that is waiting for player input, but the remote player has ended the ability, so it will not send the input.
                    // Kill the ability to avoid getting stuck active.
                    abilitySystemComponent.ForceCancelAbilityDueToReplication(this);
                    break;
                }
            }
        }

        /// <summary>
        ///  Called to inform the ability that a task is waiting for the player to do something
        /// </summary>
        public virtual void NotifyAbilityTaskWaitingOnPlayerData()
        {
            if (!CurrentActorInfo.IsValid())
            {
                return;
            }

            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Checked();
            if (RemoteInstanceEnded)
            { 
                abilitySystemComponent.ForceCancelAbilityDueToReplication(this);
            }
        }

        /// <summary>
        /// Called to inform the ability that a task is waiting for the player's avatar to do something in world
        /// </summary>
        public virtual void NotifyAbilityTaskWaitingOnAvatar()
        {
            if (CurrentActorInfo.IsValid() && (Entity)CurrentActorInfo.AvatarActor != null)
            {
                var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Checked();
                abilitySystemComponent.ForceCancelAbilityDueToReplication(this);
            }
        }

        /// <summary>
        /// Called when the ability is given to an AbilitySystemComponent
        /// </summary>
        /// <param name="actorInfo"></param>
        /// <param name="abilitySpec"></param>
        public virtual void OnGiveAbility(ref GameplayAbilityActorInfo actorInfo, ref GameplayAbilitySpec abilitySpec)
        {
            SetCurrentActorInfo(abilitySpec.Handle, ref actorInfo);

            // If we already have an avatar set, call the OnAvatarSet event as well
            if (actorInfo.IsValid() && (Entity)actorInfo.AvatarActor != null)
            {
                OnAvatarSet(ref actorInfo, ref abilitySpec);
            }
        }
        
        /// <summary>
        ///  Called when the ability is removed from an AbilitySystemComponent 
        /// </summary>
        /// <param name="actorInfo"></param>
        /// <param name="abilitySpec"></param>
        public virtual void OnRemoveAbility(ref GameplayAbilityActorInfo actorInfo, ref GameplayAbilitySpec abilitySpec)
        {
            
        }

        /// <summary>
        /// Called when the avatar actor is set/changes
        /// </summary>
        /// <param name="actorInfo"></param>
        /// <param name="abilitySpec"></param>
        public virtual void OnAvatarSet(ref GameplayAbilityActorInfo actorInfo, ref GameplayAbilitySpec abilitySpec)
        {
            // Projects may want to initiate passives or do other "BeginPlay" type of logic here.
        }
        
        /// <summary>
        ///  Takes in the ability spec and checks if we should allow replication on the ability spec, this will NOT stop replication of the ability UObject just the spec inside the UAbilitySystemComponenet ActivatableAbilities for this ability
        /// </summary>
        /// <param name="abilitySpec"></param>
        /// <returns></returns>
        public virtual bool ShouldReplicateAbilitySpec(ref GameplayAbilitySpec abilitySpec)
        {
            return true;
        }

        /// <summary>
        /// Invalidates the current prediction key. This should be used in cases where there is a valid prediction window, but the server is doing logic that only it can do, and afterwards performs an action that the client could predict (had the client been able to run the server-only code prior).
        /// This returns instantly and has no other side effects other than clearing the current prediction key.
        /// </summary>
        public void InvalidateClientPredictionKey()
        {
            if (CurrentActorInfo.IsValid())
            {
                if (CurrentActorInfo.AbilitySystemComponent.TryGetTarget(out var abilitySystemComponent))
                {
                    abilitySystemComponent.ScopedPredictionKey = new PredictionKey();
                }
            }
        }

        /// <summary>
        /// Removes the GameplayEffect that granted this ability. Can only be called on instanced abilities.
        /// </summary>
        public virtual void RemoveGrantedByEffect()
        {
            if (!Assert.Ensure(IsInstantiated()))
            {
                return;
            }
            
            var abilitySystemComponent = GetAbilitySystemComponentFromActorInfo_Ensure();
            var activeHandle = abilitySystemComponent.FindActiveGameplayEffectHandle(GetCurrentAbilitySpecHandle());
            if (activeHandle.IsValid())
            {
                abilitySystemComponent.RemoveActiveGameplayEffect(activeHandle);
            }
        }
        #endregion

        /** Called to initialize after being created due to replication */
        protected virtual void PostNetInit()
        {
            /* We were dynamically spawned from replication - we need to init a currentactorinfo by looking at outer.
             *  This may need to be updated further if we start having abilities live on different outers than player AbilitySystemComponents.
             */
            throw new NotImplementedException();
        }
        
        /// <summary>
        ///  Modifies actor info, only safe on instanced abilities
        /// </summary>
        protected virtual void SetCurrentActorInfo(GameplayAbilitySpecHandle handle, ref GameplayAbilityActorInfo actorInfo)
        {
            if (IsInstantiated())
            {
                CurrentActorInfo = actorInfo;
                CurrentSpecHandle = handle;
            }
        }
	
        /** Modifies activation info, only safe on instanced abilities */
        protected virtual void SetCurrentActivationInfo(GameplayAbilityActivationInfo activationInfo)
        {
            if (IsInstantiated())
            {
                CurrentActivationInfo = activationInfo;
            }
        }
        protected void SetCurrentInfo(GameplayAbilitySpecHandle handle, 
            ref GameplayAbilityActorInfo actorInfo, 
            GameplayAbilityActivationInfo activationInfo)
        {
            SetCurrentActorInfo(handle, ref actorInfo);
            SetCurrentActivationInfo(activationInfo);
        }
        
        internal void IncrementListLock()
        {
            ++ScopeLockCount;
        }

        internal void DecrementListLock()
        {
            --ScopeLockCount;
            if (ScopeLockCount == 0)
            {
                // execute delayed functions in the order they came in
                // These may end or cancel this ability
                for (int i = 0; i < WaitingToExecute.Count; i++)
                {
                    WaitingToExecute[i].ExecuteIfBound();
                }
                WaitingToExecute.Clear();
            }
        }

        protected bool EnsureAbilityIsInstanced([System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
            if (!IsInstantiated())
            {
                AbilitySystemComponent.Ability_Error($"[{callerName}] cannot be called on a non-instanced ability. Check the instancing policy.");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Structure that defines how an ability will be triggered by external events
    /// </summary>
    public struct AbilityTriggerData
    {
        public AbilityTriggerData(GameplayTag gameplayTag, EGameplayAbilityTriggerSource triggerSource)
        {
            TriggerTag = gameplayTag;
            TriggerSource = triggerSource;
        }
        
        /// <summary>
        /// The tag to respond to 
        /// </summary>
        public GameplayTag TriggerTag;
        
        /// <summary>
        /// The type of trigger to respond to 
        /// </summary>
        public EGameplayAbilityTriggerSource TriggerSource;
    }
}