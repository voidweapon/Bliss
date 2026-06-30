using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bliss.Base;
using Bliss.Base.Event;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public delegate bool GameplayEffectApplicationQuery(ActiveGameplayEffectsContainer activeGEContainer, ref GameplayEffectSpec geSpecToConsider);
    
    public partial class AbilitySystemComponent : Entity
    {
        /// <summary>
        /// Cached off data about the owning actor that abilities will need to frequently access (movement component, mesh component, anim instance, etc)
        /// </summary>
        public GameplayAbilityActorInfo AbilityActorInfo;
        
        protected GameplayAbilitySpecContainer ActivatableAbilities;
        
        /// <summary>
        /// Full list of all instance-per-execution gameplay abilities associated with this component
        /// </summary>
        private List<GameplayAbility> AllReplicatedInstancedAbilities;
        /// <summary>
        /// Indicates how many levels of ABILITY_SCOPE_LOCK() we are in. The ability list may not be modified while AbilityScopeLockCount > 0.
        /// </summary>
        protected int AbilityScopeLockCount;

        /// <summary>
        /// Abilities that will be removed when exiting the current ability scope lock.
        /// </summary>
        protected List<GameplayAbilitySpecHandle> AbilityPendingRemoves
        {
            get
            {
                if (_abilityPendingRemoves == null)
                {
                    _abilityPendingRemoves = Rent<List<GameplayAbilitySpecHandle>>();
                }
                return _abilityPendingRemoves;
            }
            set => _abilityPendingRemoves =  value;
        }
        private List<GameplayAbilitySpecHandle> _abilityPendingRemoves;
    

        /// <summary>
        /// Abilities that will be added when exiting the current ability scope lock. 
        /// </summary>
        protected List<GameplayAbilitySpec> AbilityPendingAdds
        {
            get
            {
                if (_abilityPendingAdds == null)
                {
                    _abilityPendingAdds = Rent<List<GameplayAbilitySpec>>();
                }
                return _abilityPendingAdds;
            }
            set => _abilityPendingAdds =  value;
        }
        private List<GameplayAbilitySpec> _abilityPendingAdds;
        

        /// <summary>
        /// Whether all abilities should be removed when exiting the current ability scope lock. Will be prioritized over pending adds.
        /// </summary>
        protected bool bAbilityPendingClearAll;

        /// <summary>
        /// Local World time of the last ability activation. This is used for AFK/idle detection 
        /// </summary>
        protected float AbilityLastActivatedTime;

        /// <summary>
        /// Abilities that are triggered from a gameplay event
        /// </summary>
        protected Dictionary<GameplayTag, List<GameplayAbilitySpecHandle>> GameplayEventTriggeredAbilities;
        
        /// <summary>
        /// Abilities that are triggered from a tag being added to the owner
        /// </summary>
        protected Dictionary<GameplayTag, List<GameplayAbilitySpecHandle>> OwnedTagTriggeredAbilities;
        
        
        /// <summary>
        /// Contains all of the gameplay effects that are currently active on this component
        /// </summary>
        protected ActiveGameplayEffectsContainer ActiveGameplayEffects;

        protected ActiveGameplayCueContainer ActiveGameplayCues;

        protected ActiveGameplayCueContainer MinimalReplicationGameplayCues;
        
        protected GameplayTagCountContainer BlockedAbilityTags;
        
        protected GameplayTagCountContainer GameplayTagCountContainer;

        /// <summary>
        /// Cached value of rather this is a simulated actor
        /// </summary>
        private bool bCachedIsNetSimulated;
        
        #region Attributes
        
        #endregion
        
        #region GameplayEffects: Primary outward facing API for other systems

        /// <summary>
        /// Applies a previously created gameplay effect spec to a target 
        /// </summary>
        /// <param name="gameplayEffect"></param>
        /// <param name="target"></param>
        /// <param name="predictionKey"></param>
        /// <returns></returns>
        public virtual ActiveGameplayEffectHandle ApplyGameplayEffectSpecToTarget(GameplayEffectSpec gameplayEffect, AbilitySystemComponent target, PredictionKey predictionKey)
        {
            if (AbilitySystemGlobals.Instance.ShouldPredictTargetGameplayEffects())
            {
                // If we don't want to predict target effects, clear prediction key
                predictionKey = new PredictionKey();
            }
            
            ActiveGameplayEffectHandle returnHandle = new ActiveGameplayEffectHandle();
            if (target != null)
            {
                returnHandle = target.ApplyGameplayEffectSpecToSelf(gameplayEffect, predictionKey);
            }
            
            return returnHandle;
        }
        
        /// <summary>
        ///  Applies a previously created gameplay effect spec to this component 
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="predictionKey"></param>
        /// <returns></returns>
        public virtual ActiveGameplayEffectHandle ApplyGameplayEffectSpecToSelf(GameplayEffectSpec spec, PredictionKey predictionKey)
        {
            // Scope lock the container after the addition has taken place to prevent the new effect from potentially getting mangled during the remainder
            // of the add operation
            using ScopedActiveGameplayEffectLock scopeLock = new ScopedActiveGameplayEffectLock(ActiveGameplayEffects);
            using ScopeCurrentGameplayEffectBeingApplied scopedGeApplication = new ScopeCurrentGameplayEffectBeingApplied(ref spec, this);
            
            bool bIsNetAuthority = IsOwnerActorAuthoritative();

            // Check Network Authority
            if (!HasNetworkAuthorityToApplyGameplayEffect(predictionKey))
            {
                return new ActiveGameplayEffectHandle();
            }
            
            // Don't allow prediction of periodic effects
            if (predictionKey.IsValidKey() && spec.Period > 0f)
            {
                if (IsOwnerActorAuthoritative())
                {
                    // Server continue with invalid prediction key
                    predictionKey = new PredictionKey();
                }
                else
                {
                    // Client just return now
                    return new ActiveGameplayEffectHandle();
                }
            }
            
            // Check if there is a registered "application" query that can block the application
            foreach (var applicationQuery in GameplayEffectApplicationQueries)
            {
                bool allowed = applicationQuery.Invoke(ActiveGameplayEffects, ref spec);
                if (!allowed)
                {
                    return new ActiveGameplayEffectHandle();
                }
            }
            
            // check if the effect being applied actually succeeds
            if(!spec.Def.CanApply(ActiveGameplayEffects, ref spec))
            {
                return new ActiveGameplayEffectHandle();
            }
            
            
            // Check AttributeSet requirements: make sure all attributes are valid
            // We may want to cache this off in some way to make the runtime check quicker.
            // We also need to handle things in the execution list
            
            
            
            // Clients should treat predicted instant effects as if they have infinite duration. The effects will be cleaned up later.
            bool bTreatAsInfiniteDuration = GetOwnerRole() != ENetRole.Authority && predictionKey.IsLocalClientKey() &&
                                            spec.Def.DurationPolicy == EGameplayEffectDurationType.Instant;
            
            // Make sure we create our copy of the spec in the right place
            // We initialize the FActiveGameplayEffectHandle here with INDEX_NONE to handle the case of instant GE
            // Initializing it like this will set the bPassedFiltersAndWasExecuted on the FActiveGameplayEffectHandle to true so we can know that we applied a GE
            ActiveGameplayEffectHandle MyHandle =  new ActiveGameplayEffectHandle(-1);
            bool bInvokeGameplayCueApplied = spec.Def.DurationPolicy != EGameplayEffectDurationType.Instant;
            bool bFoundExistingStackableGE = false;

            ActiveGameplayEffect AppliedEffect = default;
            GameplayEffectSpec OurCopyOfSpec = default;
 
            if (spec.Def.DurationPolicy != EGameplayEffectDurationType.Instant || bTreatAsInfiniteDuration)
            {
                AppliedEffect = ActiveGameplayEffects.ApplyGameplayEffectSpec(ref spec, predictionKey, out bFoundExistingStackableGE);
                if (!AppliedEffect.IsValid())
                {
                    return new ActiveGameplayEffectHandle();
                }

                MyHandle = AppliedEffect.Handle;
                OurCopyOfSpec = AppliedEffect.Spec;
            }

            if (!OurCopyOfSpec.IsValid())
            {
                OurCopyOfSpec = spec;
                
                AbilitySystemGlobals.Instance.GlobalPreGameplayEffectSpecApply(ref OurCopyOfSpec, this);
                OurCopyOfSpec.CaptureAttributeDataFromTarget(this);
            }

            if (bTreatAsInfiniteDuration)
            {
                OurCopyOfSpec.SetDuration(-1, true);
            }
            
            // Update (not push) the global spec being applied [we want to switch it to our copy, from the const input copy)
            AbilitySystemGlobals.Instance.SetCurrentAppliedGE(ref OurCopyOfSpec);
            
            // UE5.4: We are following the same previous implementation that there is a special case for Gameplay Cues here (caveat: may not be true):
            // We are Stacking an existing Gameplay Effect.  That means the GameplayCues should already be Added/WhileActive and we do not have a proper
            // way to replicate the fact that it's been retriggered, hence the RPC here.  I say this may not be true because any number of things could have
            // removed the GameplayCue by the time we getting a Stacking GE (e.g. RemoveGameplayCue).
            if (!bSuppressGameplayCues && !spec.Def.bSuppressStackingCues && bFoundExistingStackableGE && AppliedEffect.IsValid() && !AppliedEffect.bIsInhibited)
            {
                if (OurCopyOfSpec.IsValid() && OurCopyOfSpec.GetStackCount() > spec.GetStackCount())
                {
                    // Because PostReplicatedChange will get called from modifying the stack count
                    // (and not PostReplicatedAdd) we won't know which GE was modified.
                    // So instead we need to explicitly RPC the client so it knows the GC needs updating
                    AbilitySystemGlobals.Instance.GetGameplayCueManager().InvokeGameplayCueAddedAndWhileActive_FromSpec(this, ref OurCopyOfSpec, predictionKey);
                }
            }
            
            
            // Execute the GE at least once (if instant, this will execute once and be done. If persistent, it was added to ActiveGameplayEffects in ApplyGameplayEffectSpec)
	
            // Execute if this is an instant application effect
            if (bTreatAsInfiniteDuration)
            {
                // This is an instant application but we are treating it as an infinite duration for prediction. We should still predict the execute GameplayCUE.
                // (in non predictive case, this will happen inside ::ExecuteGameplayEffect)

                if (!bSuppressGameplayCues)
                {
                    AbilitySystemGlobals.Instance.GetGameplayCueManager().InvokeGameplayCueExecuted_FromSpec(this, ref OurCopyOfSpec, predictionKey);
                }
            }
            else if(spec.Def.DurationPolicy ==  EGameplayEffectDurationType.Instant)
            {
                // This is a non-predicted instant effect (it never gets added to ActiveGameplayEffects)
                ExecuteGameplayEffect(ref OurCopyOfSpec, predictionKey);
            }
            
            // Notify the Gameplay Effect (and its Components) that it has been successfully applied
            spec.Def.OnApplied(ActiveGameplayEffects, ref OurCopyOfSpec, ref predictionKey);

            var InstigatorASC = spec.GetContext().GetInstigatorAbilitySystemComponent();
            // Send ourselves a callback	
            OnGameplayEffectAppliedToSelf(InstigatorASC, ref OurCopyOfSpec, MyHandle);
            
            // Send the instigator a callback
            if (InstigatorASC != null)
            {
                InstigatorASC.OnGameplayEffectAppliedToTarget(this, ref OurCopyOfSpec, MyHandle);   
            }

            return MyHandle;
        }

        /// <summary>
        ///  Gets the ActiveGameplayEffect based on the passed in Handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public GameplayEffect GetGameplayEffectDefForHandle(ActiveGameplayEffectHandle handle)
        {
            ActiveGameplayEffect ActiveGE = ActiveGameplayEffects.GetActiveGameplayEffect(handle);
            if (ActiveGE.IsValid())
            {
                return ActiveGE.Spec.Def;
            }
            
            return null;
        }

        /// <summary>
        /// removes GameplayEffect by Handle. StacksToRemove=-1 will remove all stacks.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="stacksToRemove"></param>
        /// <returns></returns>
        public virtual bool RemoveActiveGameplayEffect(ActiveGameplayEffectHandle handle, int stacksToRemove = -1)
        {
            return ActiveGameplayEffects.RemoveActiveGameplayEffect(handle, stacksToRemove);
        }

        /// <summary>
        /// Remove active gameplay effects whose backing definition are the specified gameplay effect class
        /// </summary>
        /// <param name="gameplayEffect"></param>
        /// <param name="instigatorAbilitySystemComponent"></param>
        /// <param name="stacksToRemove"></param>
        public virtual void RemoveActiveGameplayEffectBySourceEffect(SubClassOf<GameplayEffect> gameplayEffect ,AbilitySystemComponent instigatorAbilitySystemComponent, int stacksToRemove = -1)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get an outgoing GameplayEffectSpec that is ready to be applied to other things.
        /// </summary>
        /// <param name="gameplayEffectClass"></param>
        /// <param name="level"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual GameplayEffectSpecHandle MakeOutgoingSpec(SubClassOf<GameplayEffect> gameplayEffectClass,float level, GameplayEffectContextHandle context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an EffectContext for the owner of this AbilitySystemComponent
        /// </summary>
        /// <returns></returns>
        public virtual GameplayEffectContextHandle MakeEffectContext()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the count of the specified source effect on the ability system component. For non-stacking effects, this is the sum of all active instances.
        /// For stacking effects, this is the sum of all valid stack counts. If an instigator is specified, only effects from that instigator are counted.
        /// </summary>
        /// <param name="optionalInstigatorFilterComponent"></param>
        /// <param name="bEnforceOnGoingCheck"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int GetGameplayEffectCount<T>(AbilitySystemComponent optionalInstigatorFilterComponent, bool bEnforceOnGoingCheck = true)
            where T : GameplayEffect
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the count of the specified source effect on the ability system component. For non-stacking effects, this is the sum of all active instances.
        /// For stacking effects, this is the sum of all valid stack counts. If an instigator is specified, only effects from that instigator are counted.
        /// </summary>
        /// <param name="optionalInstigatorFilterComponent"></param>
        /// <param name="bEnforceOnGoingCheck"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int GetGameplayEffectCount_IfLoaded<T>(AbilitySystemComponent optionalInstigatorFilterComponent,
            bool bEnforceOnGoingCheck = true)
            where T : GameplayEffect
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the sum of StackCount of all gameplay effects that pass query
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public int GetAggregatedStackCount(GameplayEffectQuery query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the total duration of a gameplay effect
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public float GetGameplayEffectDuration(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called whenever the server time replicates via the game state to keep our cooldown timers in sync with the server 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void RecomputeGameplayEffectStartTimes()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return start time and total duration of a gameplay effect
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="duration"></param>
        public void GetGameplayEffectStartTimeAndDuration(out float startTime, out float duration)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dynamically update the set-by-caller magnitude for an active gameplay effect
        /// </summary>
        /// <param name="activeHandle"></param>
        /// <param name="setByCallerTag"></param>
        /// <param name="newValue"></param>
        public virtual void UpdateActiveGameplayEffectSetByCallerMagnitude(ActiveGameplayEffectHandle activeHandle,
            GameplayTag setByCallerTag, float newValue)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Dynamically update multiple set-by-caller magnitudes for an active gameplay effect
        /// </summary>
        /// <param name="activeHandle"></param>
        /// <param name="newSetByCallerValues"></param>
        public virtual void UpdateActiveGameplayEffectSetByCallerMagnitudes(ActiveGameplayEffectHandle activeHandle,
            Dictionary<GameplayTag, float> newSetByCallerValues)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the level of an already applied gameplay effect. The intention is that this is 'seemless' and doesnt behave like removing/reapplying
        /// </summary>
        /// <param name="activeHandle"></param>
        /// <param name="newLevel"></param>
        public virtual void SetActiveGameplayEffectLevel(ActiveGameplayEffectHandle activeHandle, int newLevel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the level of an already applied gameplay effect. The intention is that this is 'seemless' and doesnt behave like removing/reapplying
        /// </summary>
        /// <param name="query"></param>
        /// <param name="newLevel"></param>
        public virtual void SetActiveGameplayEffectLevelUsingQuery(GameplayEffectQuery query, int newLevel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activeGEHandle"></param>
        /// <param name="bInhibit"></param>
        /// <param name="bInvokeGameplayCueEvents"></param>
        /// <returns></returns>
        public virtual ActiveGameplayEffectHandle SetActiveGameplayEffectInhibit(ActiveGameplayEffectHandle activeGEHandle, bool bInhibit, bool bInvokeGameplayCueEvents)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Raw accessor to ask the magnitude of a gameplay effect, not necessarily always correct. How should outside code (UI, etc) ask things like 'how much is this gameplay effect modifying my damage by'
        /// (most likely we want to catch this on the backend - when damage is applied we can get a full dump/history of how the number got to where it is. But still we may need polling methods like below (how much would my damage be)
        /// </summary>
        /// <param name="Handle"></param>
        /// <param name="Attribute"></param>
        /// <returns></returns>
        public float GetGameplayEffectMagnitude(ActiveGameplayEffectHandle Handle/*, GameplayAttribute Attribute*/)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns current stack count of an already applied GE
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public int GetCurrentStackCount(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Returns current stack count of an already applied GE, but given the ability spec handle that was granted by the GE
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public int GetCurrentStackCount(GameplayAbilitySpecHandle handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns debug string describing active gameplay effect 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public string GetActiveGEDebugString(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the GE Handle of the GE that granted the passed in Ability 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public ActiveGameplayEffectHandle FindActiveGameplayEffectHandle(GameplayAbilitySpecHandle handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns const pointer to the actual active gameplay effect structure
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public ActiveGameplayEffect GetActiveGameplayEffect(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Returns all active gameplay effects on this ASC
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ActiveGameplayEffectsContainer GetActiveGameplayEffects()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a const pointer to the gameplay effect CDO associated with an active handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public GameplayEffect GetGameplayEffectCDO(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the source tags from the gameplay spec represented by the specified handle, if possible
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public GameplayTagContainer GetGameplayEffectSourceTagsFromHandle(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Get the target tags from the gameplay spec represented by the specified handle, if possible
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public GameplayTagContainer GetGameplayEffectTargetTagsFromHandle(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Populate the specified capture spec with the data necessary to capture an attribute from the component
        /// </summary>
        /// <param name="outCaptureSpec"></param>
        public void CaptureAttributeForGameplayEffect(out GameplayEffectAttributeCaptureSpec outCaptureSpec)
        {
            throw new NotImplementedException();
        }
        
        #endregion

        #region  Callbacks / Notifies
        //(these need to be at the UObject level so we can safely bind, rather than binding to raw at the ActiveGameplayEffect/Container level which is unsafe if the AbilitySystemComponent were killed).

        /// <summary>
        ///  A generic callback anytime an ability is activated (started)
        /// </summary>
        private AutoEventAction<GameplayAbility> AbilityActivatedCallbacks;
        
        /// <summary>
        /// A generic callback anytime an ability is committed (cost/cooldown applied)
        /// </summary>
        private AutoEventAction<GameplayAbility> AbilityCommittedCallbacks;

        /// <summary>
        /// Called with a failure reason when an ability fails to activate 
        /// </summary>
        private AutoEventAction<GameplayAbility, GameplayTagContainer> AbilityFailedCallbacks;

        /// <summary>
        /// We allow users to setup a series of functions that must be true in order to allow a GameplayEffect to be applied 
        /// </summary>
        private List<GameplayEffectApplicationQuery> GameplayEffectApplicationQueries;
        
        

        internal void OnGameplayEffectAppliedToTarget(AbilitySystemComponent target, ref GameplayEffectSpec specApplied, ActiveGameplayEffectHandle activeHandle)
        {
            throw new NotImplementedException();
        }
        
        internal void OnGameplayEffectAppliedToSelf(AbilitySystemComponent source, ref GameplayEffectSpec specApplied, ActiveGameplayEffectHandle activeHandle)
        {
            throw new NotImplementedException();
        }
        
        internal virtual void NotifyAbilityCommit(GameplayAbility ability)
        {
            AbilityCommittedCallbacks?.Invoke(ability);   
        }

        internal virtual void NotifyAbilityActivated(GameplayAbilitySpecHandle handle, GameplayAbility ability)
        {
            AbilityActivatedCallbacks?.Invoke(ability);
        }

        internal virtual void NotifyAbilityFailed(GameplayAbilitySpecHandle handle, GameplayAbility ability,
            GameplayTagContainer failureReason)
        {
            AbilityFailedCallbacks?.Invoke(ability, failureReason);
        }

        #endregion

        #region Gameplay tag operations

        public bool HasMatchingGameplayTag(GameplayTag tagToCheck)
        {
           return GameplayTagCountContainer.HasMatchingGameplayTag(tagToCheck);
        }

        public bool HasAllMatchingGameplayTags(GameplayTagContainer tagContainer)
        {
            return GameplayTagCountContainer.HasAllMatchingGameplayTags(tagContainer);
        }

        public bool HasAnyMatchingGameplayTags(GameplayTagContainer tagContainer)
        {
            return GameplayTagCountContainer.HasAnyMatchingGameplayTags(tagContainer);
        }

        public void GetOwnedGameplayTags(GameplayTagContainer tagContainer)
        {
            tagContainer.Reset();
            tagContainer.AppendTags(GetOwnedGameplayTags());
        }

        public GameplayTagContainer GetOwnedGameplayTags()
        {
            return GameplayTagCountContainer.GetExplicitGameplayTags();
        }
        
        protected virtual void OnTagUpdated(ref GameplayTag tag, bool tagExists)
        {
            
        }
        
        /// <summary>
        /// Forcibly sets the number of instances of a given tag
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="newCount"></param>
        void SetTagMapCount(ref GameplayTag tag, int newCount)
        {
            GameplayTagCountContainer.SetTagCount(ref tag, newCount);
        }
        
        void UpdateTagMap(ref GameplayTag tag, int countDelta)
        {
            if (GameplayTagCountContainer.UpdateTagCount(ref tag, countDelta))
            {
                OnTagUpdated(ref tag, countDelta > 0);
            }
        }
        void UpdateTagMap(GameplayTagContainer container, int countDelta)
        {
            if (!container.IsEmpty())
            {
                UpdateTagMap_Internal(container, countDelta);
            }
        }

        public void AddLooseGameplayTag(ref GameplayTag tag, int count = 1)
        {
            UpdateTagMap(ref tag, count);
        }
        
        public void AddLooseGameplayTags(GameplayTagContainer gameplayTags, int count = 1)
        {
            UpdateTagMap(gameplayTags, count);
        }
        
        public void RemoveLooseGameplayTag(ref GameplayTag tag, int count = 1)
        {
            UpdateTagMap(ref tag, -count);
        }
        
        public void RemoveLooseGameplayTags(GameplayTagContainer gameplayTags, int count = 1)
        {
            UpdateTagMap(gameplayTags, -count);
        }
        
        public void SetLooseGameplayTag(ref GameplayTag tag, int newCount)
        {
            SetTagMapCount(ref tag, newCount);
        }

        void UpdateTagMap_Internal(GameplayTagContainer container, int countDelta)
        {
            throw new NotImplementedException();
        }
     
        
        public void RemoveReplicatedLooseGameplayTag(ref GameplayTag tag)
        {
            throw new NotImplementedException();
        }
        
        public void RemoveReplicatedLooseGameplayTags(GameplayTagContainer gameplayTags)
        {
            throw new NotImplementedException();
        }
        
        #endregion

        #region GameplayEffect Helper Functions

        protected void ExecutePeriodicEffect(ref ActiveGameplayEffectHandle handle)
        {
            
        }
            
        protected void ExecuteGameplayEffect(ref GameplayEffectSpec spec, PredictionKey predictionKey)
        {
            
        }

        protected void CheckDurationExpired(ref ActiveGameplayEffectHandle handle)
        {
            
        }

        #endregion
        
        #region GameplayCues Helper Functions

        /// <summary>
        /// Called for predictively added gameplay cue. Needs to remove tag count and possible invoke OnRemove event if misprediction
        /// </summary>
        /// <param name="Tag"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void OnPredictiveGameplayCueCatchup(GameplayTag tag)
        {
            throw new NotImplementedException();
        }
        
        // Do not call these functions directly, call the wrappers on GameplayCueManager instead
        internal void NetMulticast_InvokeGameplayCueExecuted_FromSpec(GameplayEffectSpecForRPC spec, PredictionKey predictionKey)
        {
            throw new NotImplementedException();
        }
        
        internal void NetMulticast_InvokeGameplayCueExecuted(GameplayTag gameplayCueTag, PredictionKey predictionKey, GameplayEffectContextHandle effectContext)
        {
            throw new NotImplementedException();
        }
        
        internal void NetMulticast_InvokeGameplayCuesExecuted(GameplayTagContainer gameplayCueTags, PredictionKey predictionKey, GameplayEffectContextHandle effectContext)
        {
            throw new NotImplementedException();
        }
        
        internal void NetMulticast_InvokeGameplayCueExecuted_WithParams(GameplayTag gameplayCueTag, PredictionKey predictionKey, GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }
        
        internal void NetMulticast_InvokeGameplayCuesExecuted_WithParams(GameplayTagContainer gameplayCueTags, PredictionKey predictionKey, GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }

        internal void NetMulticast_InvokeGameplayCueAdded(GameplayTag gameplayCueTag, PredictionKey predictionKey, GameplayEffectContextHandle effectContext)
        {
            throw new NotImplementedException();
        }

        internal void NetMulticast_InvokeGameplayCueAdded_WithParams(GameplayTag gameplayCueTag, PredictionKey predictionKey, GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }

        internal void NetMulticast_InvokeGameplayCueAddedAndWhileActive_FromSpec(ref GameplayEffectSpecForRPC spec, PredictionKey predictionKey)
        {
            throw new NotImplementedException();
        }

        internal void NetMulticast_InvokeGameplayCueAddedAndWhileActive_WithParams(GameplayTag gameplayCueTag, PredictionKey predictionKey, GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }

        internal void NetMulticast_InvokeGameplayCuesAddedAndWhileActive_WithParams(GameplayTagContainer gameplayCueTags, PredictionKey predictionKey, GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }
        
        // ====================================================================================================================

        /// <summary>
        /// GameplayCues can also come on their own. These take an optional effect context to pass through hit result, etc 
        /// </summary>
        /// <param name="gameplayCueTag"></param>
        /// <param name="effectContext"></param>
        public void ExecuteGameplayCue(GameplayTag gameplayCueTag, GameplayEffectContextHandle effectContext = new())
        {
            throw new NotImplementedException();
        }
        
        public void ExecuteGameplayCue(GameplayTag gameplayCueTag, ref GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Add a persistent gameplay cue
        /// </summary>
        /// <param name="gameplayCueTag"></param>
        /// <param name="effectContext"></param>
        public void AddGameplayCue(GameplayTag gameplayCueTag, GameplayEffectContextHandle effectContext = new())
        {
            throw new NotImplementedException();
        }
        
        public void AddGameplayCue(GameplayTag gameplayCueTag, ref GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add gameplaycue for minimal replication mode. Should only be called in paths that would replicate gameplaycues in other ways (through GE for example) if not in minimal replication mode
        /// </summary>
        /// <param name="gameplayCueTag"></param>
        /// <param name="effectContext"></param>
        public void AddGameplayCue_MinimalReplication(GameplayTag gameplayCueTag, GameplayEffectContextHandle effectContext = new())
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Remove a persistent gameplay cue
        /// </summary>
        /// <param name="gameplayCueTag"></param>
        public void RemoveGameplayCue(GameplayTag gameplayCueTag)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Remove gameplaycue for minimal replication mode. Should only be called in paths that would replicate gameplaycues in other ways (through GE for example) if not in minimal replication mode
        /// </summary>
        /// <param name="gameplayCueTag"></param>
        public void RemoveGameplayCue_MinimalReplication(GameplayTag gameplayCueTag)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes any GameplayCue added on its own, i.e. not as part of a GameplayEffect.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void RemoveAllGameplayCues()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Handles gameplay cue events from external sources
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="eventType"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void InvokeGameplayCueEvent(ref GameplayEffectSpecForRPC spec, EGameplayCueEvent eventType)
        {
            throw new NotImplementedException();
        }

        public void InvokeGameplayCueEvent(GameplayTag gameplayCueTag, EGameplayCueEvent eventType, GameplayEffectContextHandle effectContext = new())
        {
            throw new NotImplementedException();
        }
        
        public void InvokeGameplayCueEvent(GameplayTag gameplayCueTag, EGameplayCueEvent eventType, ref GameplayCueParameters gameplayCueParameters)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Allows polling to see if a GameplayCue is active. We expect most GameplayCue handling to be event based, but some cases we may need to check if a GameplayCue is active (Animation Blueprint for example)
        /// </summary>
        /// <param name="gameplayCueTag"></param>
        /// <returns></returns>
        public bool IsGameplayCueActive(GameplayTag gameplayCueTag)
        {
            return HasMatchingGameplayTag(gameplayCueTag);
        }

        /// <summary>
        /// Will initialize gameplay cue parameters with this ASC's Owner (Instigator) and AvatarActor (EffectCauser)
        /// </summary>
        /// <param name="parameters"></param>
        public virtual void InitDefaultGameplayCueParameters(ref GameplayCueParameters parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Are we ready to invoke gameplaycues yet?
        /// </summary>
        /// <returns></returns>
        public virtual bool IsReadyForGameplayCues()
        {
            throw new NotImplementedException();
        }

        public virtual void HandleDeferredGameplayCues(ActiveGameplayEffectsContainer gameplayEffectsContainer)
        {
            throw new NotImplementedException();
        }
        
        #endregion
        
        #region Actor interaction

        private Entity OwnerActor;
        private Entity AvatarActor;

        public Entity GetOwnerActor()
        {
            return OwnerActor;
        }


        public virtual void InitAbilityActorInfo()
        {
            throw new NotImplementedException();
        }

        public virtual void ClearActorInfo()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///	This will refresh the Ability's ActorInfo structure based on the current ActorInfo. That is, AvatarActor will be the same but we will look for new
        /// AnimInstance, MovementComponent, PlayerController, etc.
        /// </summary>
        public void RefreshAbilityActorInfo()
        {
            throw new NotImplementedException();
        }

        public ENetRole GetOwnerRole()
        {
            return OwnerActor.GetComponent<NetRoleComponent>()?.LocalNetRole ?? ENetRole.None;
        }
        
        #endregion

        #region Replication

        /// <summary>
        /// Current prediction key, set with FScopedPredictionWindow
        /// </summary>
        public PredictionKey ScopedPredictionKey;

        /// <summary>
        ///  Returns the prediction key that should be used for any actions
        /// </summary>
        /// <returns></returns>
        public PredictionKey GetPredictionKeyForNewAction()
        {
            return ScopedPredictionKey.IsValidForMorePrediction() ? ScopedPredictionKey : new PredictionKey();
        }
        
        /// <summary>
        /// Replicate that an ability has ended/canceled, to the client or server as appropriate
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ability"></param>
        /// <param name="activationInfo"></param>
        /// <param name="bWasCanceled"></param>
        internal virtual void ReplicateEndOrCancelAbility(ref GameplayAbilitySpecHandle handle,
            ref GameplayAbilityActivationInfo activationInfo,
            GameplayAbility ability,
            bool bWasCanceled)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Do we have a valid prediction key to do more predictive actions with 
        /// </summary>
        /// <returns></returns>
        public bool CanPredict()
        {
            return ScopedPredictionKey.IsValidForMorePrediction();
        }

        /// <summary>
        /// Force cancels the ability and does not replicate this to the other side. This should be called when the ability is cancelled by the other side
        /// </summary>
        /// <param name="instance"></param>
        public virtual void ForceCancelAbilityDueToReplication(GameplayAbility instance)
        {
            Assert.Check(instance != null);
            
            // Since this was a remote cancel, we should force it through. We do not support 'server says ability was cancelled but client disagrees that it can be'.
            instance.SetCanBeCanceled(true);

            var specHandle = instance.GetCurrentAbilitySpecHandle();
            var actorInfo = instance.GetCurrentActorInfo();
            var activationInfo = instance.GetCurrentActivationInfo();
            instance.CancelAbility(ref specHandle, ref actorInfo, ref activationInfo, false);
        }
        
        /// <summary>
        /// Returns true if this is running on the server or has a valid prediction key 
        /// </summary>
        /// <param name="activationInfo"></param>
        /// <returns></returns>
        public bool HasAuthorityOrPredictionKey(ref GameplayAbilityActivationInfo activationInfo)
        {
            return (activationInfo.ActivationMode == EGameplayAbilityActivationMode.Authority) || CanPredict();
        }
        
        /// <summary>
        /// Returns true if this component's actor has authority
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsOwnerActorAuthoritative()
        {
            return !bCachedIsNetSimulated;
        }
        

        #endregion
        
        #region Batching client->server RPCs



        #endregion

        #region Synchronization RPCs



        #endregion

        #region Input handling/targeting

        /// <summary>
        /// Rather activation is currently inhibited
        /// </summary>
        private bool UserAbilityActivationInhibited;
        
        /// <summary>
        /// Suppress all ability granting through GEs on this component 
        /// </summary>
        /// <returns></returns>
        private bool bSuppressGrantAbility;

        /// <summary>
        /// Suppress all GameplayCues on this component 
        /// </summary>
        /// <returns></returns>
        private bool bSuppressGameplayCues;

        /// <summary>
        /// Disable or Enable a local user from being able to activate abilities. This should only be used for input/UI etc related inhibition. Do not use for game mechanics.
        /// </summary>
        /// <param name="newInhibit"></param>
        public virtual void SetUserAbilityActivationInhibited(bool newInhibit)
        {
            if (AbilityActorInfo.IsLocallyControlled())
            {
                if (newInhibit && UserAbilityActivationInhibited)
                {
                    // This could cause problems if two sources try to inhibit ability activation, it is not clear when the ability should be uninhibited
                    Ability_Warning("Call to SetUserAbilityActivationInhibited(true) when UserAbilityActivationInhibited was already true\"");
                }
                UserAbilityActivationInhibited = newInhibit;
            }
        }

        /// <summary>
        /// This is meant to be used to inhibit activating an ability from an input perspective. (E.g., the menu is pulled up, another game mechanism is consuming all input, etc)
        /// This should only be called on locally owned players.
        /// This should not be used to game mechanics like silences or disables. Those should be done through gameplay effects.
        /// </summary>
        /// <returns></returns>
        public bool GetUserAbilityActivationInhibited()
        {
            return UserAbilityActivationInhibited;
        }

        #endregion

        #region Additional Helper Functions

        /// <summary>
        /// Tests if all modifiers in this GameplayEffect will leave the attribute > 0.f
        /// </summary>
        /// <returns></returns>
        public bool CanApplyAttributeModifiers(GameplayEffect gameplayEffect, float level, ref GameplayEffectContextHandle effectContext)
        {
            return ActiveGameplayEffects.CanApplyAttributeModifiers(gameplayEffect, level, ref effectContext);
        }
        
        public List<(float, float)> GetActiveEffectsTimeRemainingAndDuration(ref GameplayEffectQuery query)
        {
            return ActiveGameplayEffects.GetActiveEffectsTimeRemainingAndDuration(ref query);
        }

        /// <summary>
        /// Removes all active effects that contain any of the tags in Tags 
        /// </summary>
        /// <returns></returns>
        public int RemoveActiveEffectsWithTags(GameplayTagContainer tags)
        {
            if (IsOwnerActorAuthoritative())
            {
                var query = GameplayEffectQuery.MakeQuery_MatchAnyEffectTags(tags);
                return RemoveActiveEffects(ref query);
            }
            return 0;
        }
        
        /// <summary>
        /// Removes all active effects with captured source tags that contain any of the tags in Tags
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public int RemoveActiveEffectsWithSourceTags(GameplayTagContainer tags)
        {
            if (IsOwnerActorAuthoritative())
            {
                var query = GameplayEffectQuery.MakeQuery_MatchAnySourceSpecTags(tags);
                return RemoveActiveEffects(ref query);
            }
            return 0;
        }
        
        /// <summary>
        /// Removes all active effects that apply any of the tags in Tags
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public int RemoveActiveEffectsWithAppliedTags(GameplayTagContainer tags)
        {
            if (IsOwnerActorAuthoritative())
            {
                var query = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(tags);
                return RemoveActiveEffects(ref query);
            }
            return 0;
        }
        
        /// <summary>
        /// Removes all active effects that grant any of the tags in Tag
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public int RemoveActiveEffectsWithGrantedTags(GameplayTagContainer tags)
        {
            if (IsOwnerActorAuthoritative())
            {
                var query = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(tags);
                return RemoveActiveEffects(ref query);
            }
            return 0;
        }
        
        /// <summary>
        /// Removes all active effects that match given query. StacksToRemove=-1 will remove all stacks.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="stacksToRemove"></param>
        /// <returns></returns>
        public virtual int RemoveActiveEffects(ref GameplayEffectQuery query, int stacksToRemove = -1)
        {
            if (IsOwnerActorAuthoritative())
            {
                return ActiveGameplayEffects.RemoveActiveEffects(ref query, stacksToRemove);
            }

            return 0;
        }
        
        #endregion

        protected bool HasNetworkAuthorityToApplyGameplayEffect(PredictionKey predictionKey)
        {
            return IsOwnerActorAuthoritative() || predictionKey.IsValidForMorePrediction();
        }
        
        protected readonly List<AbilityListLockActiveChange> AbilityListLockActiveChanges = new List<AbilityListLockActiveChange>();
        public struct AbilityListLockActiveChange : IDisposable, IEquatable<AbilityListLockActiveChange>
        {
            private AbilitySystemComponent _abilitySystemComponent;
            public List<GameplayAbilitySpec> PendingAdds { get; }
            public List<GameplayAbilitySpecHandle> PendingRemoves { get; }
            private int _handle;
            private static int _idGenerator;
            public AbilityListLockActiveChange(AbilitySystemComponent abilitySystem,
                ref List<GameplayAbilitySpec> pendingAdds,
                ref List<GameplayAbilitySpecHandle> pendingRemoves)
            {
                _abilitySystemComponent = abilitySystem;
                PendingAdds = pendingAdds;
                PendingRemoves = pendingRemoves;
                pendingAdds = null;
                pendingRemoves = null;
                
                _handle = System.Threading.Interlocked.Increment(ref _idGenerator);
                _abilitySystemComponent.AbilityListLockActiveChanges.Add(this);
            }
            
            
            public void Dispose()
            {
                _abilitySystemComponent.AbilityListLockActiveChanges.Remove(this);
                if(PendingAdds != null)
                    AbilitySystemComponent.Return(PendingAdds);
                if(PendingRemoves != null)
                    AbilitySystemComponent.Return(PendingRemoves);
            }

            public bool Equals(AbilityListLockActiveChange other)
            {
                return _handle == other._handle;
            }

            public override bool Equals(object obj)
            {
                return obj is AbilityListLockActiveChange other && Equals(other);
            }

            public override int GetHashCode()
            {
                return _handle;
            }
        }

        private const string LOG_TAG = "GameplayAbilitySystem";

        [Conditional("UNITY_EDITOR")]
        internal static void Ability_Log(string message)
        {
            Log.Debug($"[{LOG_TAG}] {message}");
        }
        
        internal static void Ability_Warning(string message)
        {
            Log.Warning($"[{LOG_TAG}] {message}");
        }
        
        internal static void Ability_Error(string message)
        {
            Log.Error($"[{LOG_TAG}] {message}");
        }
    }
    
    /// <summary>
    ///  How gameplay effects will be replicated to clients
    /// </summary>
    public enum EGameplayEffectReplicationMode
    {
        /** Only replicate minimal gameplay effect info. Note: this does not work for Owned AbilitySystemComponents (Use Mixed instead). */
        Minimal,
        /** Only replicate minimal gameplay effect info to simulated proxies but full info to owners and autonomous proxies */
        Mixed,
        /** Replicate full gameplay info to all */
        Full,
    }
    
    /// <summary>
    /// When performing actions (such as gathering activatable abilities), how do we deal with Pending items (e.g. abilities not yet added or removed)
    /// </summary>
    [Flags]
    public enum EConsiderPending
    {
        /** Don't consider any Pending actions (such as Pending Abilities Added or Removed) */
        None = 0,

        /** Consider Pending Adds when performing the action */
        PendingAdd = (1 << 0),

        /** Consider Pending Removes when performing the action */
        PendingRemove = (1 << 1),

        All = PendingAdd | PendingRemove
    }
}