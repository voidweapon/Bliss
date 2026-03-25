using System;
using System.Collections.Generic;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public partial class AbilitySystemComponent
    {
        #region GameplayAbilities
        
        /*
           *	The role of the AbilitySystemComponent with respect to Abilities is to provide:
           *		-Management of ability instances (whether per actor or per execution instance).
           *			-Someone *has* to keep track of these instances.
           *			-Non instanced abilities *could* be executed without any ability stuff in AbilitySystemComponent.
           *				They should be able to operate on an GameplayAbilityActorInfo + GameplayAbility.
           *		
           *	As convenience, it may provide some other features:
           *		-Some basic input binding (whether instanced or non instanced abilities).
           *		-Concepts like "this component has these abilities"
         */

        /// <summary>
        /// Grants an Ability.
        /// This will be ignored if the actor is not authoritative.
        /// </summary>
        /// <param name="abilitySpec"></param>
        /// <returns>Returns handle that can be used in TryActivateAbility, etc.</returns>
        public GameplayAbilitySpecHandle GiveAbility(ref GameplayAbilitySpec abilitySpec)
        {
            if (abilitySpec.Ability == null)
            {
                Ability_Error("GiveAbility called with an invalid Ability Class.");
                return new GameplayAbilitySpecHandle();
            }

            if (!IsOwnerActorAuthoritative())
            {
                Ability_Error($"GiveAbility called on ability {abilitySpec.Ability} on the client, not allowed!");
                return new GameplayAbilitySpecHandle();
            }

            // If locked, add to pending list. The Spec.Handle is not regenerated when we receive, so returning this is ok.
            if (AbilityScopeLockCount > 0)
            {
                AbilityPendingAdds.Add(abilitySpec);
                return abilitySpec.Handle;
            }
            
            using var _ = new ScopedAbilityListLock(this);
            
            ActivatableAbilities.Items.Add(abilitySpec);
            GameplayAbilitySpec ownedSpec = ActivatableAbilities.Items[^1];
            if (ownedSpec.Ability.GetInstancingPolicy() == EGameplayAbilityInstancingPolicy.InstancedPerActor)
            {
                // Create the instance at creation time
                CreateNewInstanceOfAbility(ownedSpec.Ability, ref ownedSpec);
            }
            
            OnGiveAbility(ref ownedSpec);
            MarkAbilitySpecDirty(ref ownedSpec, true);
            
            return ownedSpec.Handle;
        }

        /// <summary>
        /// Grants an ability and attempts to activate it exactly one time, which will cause it to be removed.
        /// Only valid on the server, and the ability's Net Execution Policy cannot be set to Local or Local Predicted
        /// </summary>
        /// <param name="abilitySpec"></param>
        /// <param name="gameplayEventData"></param>
        /// <returns></returns>
        public GameplayAbilitySpecHandle GiveAbilityAndActivateOnce(ref GameplayAbilitySpec abilitySpec, GameplayEventData? gameplayEventData)
        {
            if (abilitySpec.Ability == null)
            {
                Ability_Error("GiveAbility called with an invalid Ability Class.");
                return new GameplayAbilitySpecHandle();
            }

            if (abilitySpec.Ability.GetInstancingPolicy() == EGameplayAbilityInstancingPolicy.NonInstanced
                || abilitySpec.Ability.GetNetExecutionPolicy() == EGameplayAbilityNetExecutionPolicy.LocalOnly)
            {
                Ability_Error($"GiveAbilityAndActivateOnce called on ability {abilitySpec.Ability} that is non instanced or won't execute on server, not allowed!");
                return new GameplayAbilitySpecHandle();
            }

            if (!IsOwnerActorAuthoritative())
            {
                Ability_Error($"GiveAbilityAndActivateOnce called on ability {abilitySpec.Ability} on the client, not allowed!");
                return new GameplayAbilitySpecHandle();
            }
            
            abilitySpec.IsActivateOnce = true;
            
            GameplayAbilitySpecHandle AddedAbilityHandle = GiveAbility(ref abilitySpec);
            if (FindAbilitySpecFromHandle(out GameplayAbilitySpec FoundSpec, AddedAbilityHandle))
            {
                FoundSpec.RemoveAfterActivation = true;
                if (!InternalTryActivateAbility(AddedAbilityHandle, new PredictionKey(), null, null, gameplayEventData.Value))
                {
                    // We failed to activate it, so remove it now
                    ClearAbility(ref AddedAbilityHandle);

                    return new GameplayAbilitySpecHandle();
                }
            }
            else if(gameplayEventData != null)
            {   
                // Cache the GameplayEventData in the pending spec (if it was correctly queued)
                if (AbilityPendingAdds.Count > 0)
                {
                    var pendingSpec = AbilityPendingAdds[^1];
                    if (pendingSpec.Handle == AddedAbilityHandle)
                    {
                        pendingSpec.GameplayEventData = gameplayEventData.Value;
                    }
                }
            }
            
            return AddedAbilityHandle;
        }

        /// <summary>
        /// Wipes all 'given' abilities.
        /// This will be ignored if the actor is not authoritative.
        /// </summary>
        public void ClearAllAbilities()
        {
            // If this is called inside an ability scope lock, postpone the workload until end of scope.
            // This was introduced for abilities that trigger their owning actor's destruction on ability
            // activation.
            if (AbilityScopeLockCount > 0)
            {
                bAbilityPendingClearAll = true;
                return;
            }

            if (!IsOwnerActorAuthoritative())
            {
                Ability_Error($"Attempted to call {nameof(ClearAllAbilities)} without authority.");
                return;
            }

            // Note we aren't marking any old abilities pending kill. This shouldn't matter since they will be garbage collected.
            using var _ = new ScopedAbilityListLock(this);
            for (int i = 0; i < ActivatableAbilities.Items.Count; i++)
            {
                var abilitySpec = ActivatableAbilities.Items[i];
                OnRemoveAbility(ref abilitySpec);
            }
            
            ActivatableAbilities.Items.Clear();
            //ActivatableAbilities.MarkArrayDirty();
            
            CheckForClearedAbilities();
            bAbilityPendingClearAll = false;
        }

        /// <summary>
        /// Removes the specified ability.
        /// </summary>
        /// <param name="handle"></param>
        public void ClearAbility(ref GameplayAbilitySpecHandle handle)
        {
            if (!IsOwnerActorAuthoritative())
            {
                Ability_Error($"AAttempted to call {nameof(ClearAbility)} on the client. This is not allowed!");
                return;
            }

            for (int index = 0; index < AbilityPendingAdds.Count; index++)
            {
                if (AbilityPendingAdds[index].Handle == handle)
                {
                    AbilityPendingAdds.RemoveAt(index);
                    return;
                }
            }


            for (int index = ActivatableAbilities.Items.Count - 1; index >= 0; index--)
            {
                var abilitySpec = ActivatableAbilities.Items[index];
                if (abilitySpec.Handle == handle)
                {
                    if (AbilityScopeLockCount > 0)
                    {
                        if (abilitySpec.PendingRemove == false)
                        {
                            abilitySpec.PendingRemove = true;
                            ActivatableAbilities.Items[index] = abilitySpec;
                            AbilityPendingRemoves.Add(handle);
                        }
                    }
                    else
                    {
                        // OnRemoveAbility will possibly call EndAbility. EndAbility can "do anything" including remove this abilityspec again. So a scoped list lock is necessary here.
                        {
                            using var _ = new ScopedAbilityListLock(this);
                            OnRemoveAbility(ref abilitySpec);
                            ActivatableAbilities.Items.RemoveAt(index);
                            //ActivatableAbilities.MarkArrayDirty();
                        }
                        CheckForClearedAbilities();
                    }
                }
            }
        }

        /// <summary>
        /// Sets an ability spec to remove when its finished. If the spec is not currently active, it terminates it immediately. Also clears InputID of the Spec.
        /// </summary>
        /// <param name="abilitySpecHandle"></param>
        public void SetRemoveAbilityOnEnd(ref GameplayAbilitySpecHandle abilitySpecHandle)
        {
            if (FindAbilitySpecFromHandle(out GameplayAbilitySpec abilitySpec, abilitySpecHandle))
            {
                if (abilitySpec.IsActive())
                {
                    abilitySpec.RemoveAfterActivation = true;
                }
                else
                {
                    ClearAbility(ref abilitySpecHandle);
                }
            }
        }

        /// <summary>
        /// Gets all Activatable Gameplay Abilities that match all tags in GameplayTagContainer AND for whichDoesAbilitySatisfyTagRequirements() is true. 
        /// </summary>
        /// <param name="gameplayTagContainer"></param>
        /// <param name="matchingGameplayAbilities"></param>
        /// <param name="bOnlyAbilitiesThatSatisfyTagRequirements"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void GetActivatableGameplayAbilitySpecsByAllMatchingTags(GameplayTagContainer gameplayTagContainer,
            List<GameplayAbilitySpec> matchingGameplayAbilities, 
            bool bOnlyAbilitiesThatSatisfyTagRequirements = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to activate the ability that is passed in. This will check costs and requirements before doing so.
        /// If bAllowRemoteActivation is true, it will remotely activate local/server abilities, if false it will only try to locally activate the ability
        /// </summary>
        /// <param name="gameplayTagContainer"></param>
        /// <param name="allowRemoteActivation"></param>
        /// <returns>Returns true if it thinks it activated, but it may return false positives due to failure later in activation.</returns>
        public bool TryActivateAbilitiesByTag(GameplayTagContainer gameplayTagContainer, bool allowRemoteActivation = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to activate the ability that is passed in. This will check costs and requirements before doing so.
        /// If bAllowRemoteActivation is true, it will remotely activate local/server abilities, if false it will only try to locally activate the ability
        /// </summary>
        /// <param name="inAbilityToActive"></param>
        /// <param name="allowRemoteActivation"></param>
        /// <returns>Returns true if it thinks it activated, but it may return false positives due to failure later in activation.</returns>
        public bool TryActivateAbilityByClass(GameplayAbility inAbilityToActive, bool allowRemoteActivation = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to activate the given ability, will check costs and requirements before doing so.
        /// If bAllowRemoteActivation is true, it will remotely activate local/server abilities, if false it will only try to locally activate the ability
        /// </summary>
        /// <param name="inAbilityToActive"></param>
        /// <param name="allowRemoteActivation"></param>
        /// <returns>Returns true if it thinks it activated, but it may return false positives due to failure later in activation.</returns>
        public bool TryActivateAbility(GameplayAbilitySpecHandle inAbilityToActive, bool allowRemoteActivation = true)
        {
            throw new NotImplementedException();
        }

        public bool HasActivatableTriggeredAbility(GameplayTag tag)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Triggers an ability from a gameplay event, will only trigger on local/server depending on execution flags
        /// </summary>
        /// <param name="inAbilityToActive"></param>
        /// <param name="actorInfo"></param>
        /// <param name="eventTag"></param>
        /// <param name="eventPayload"></param>
        /// <param name="abilitySystemComponent"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool TriggerAbilityFromGameplayEvent(GameplayAbilitySpecHandle inAbilityToActive,
            GameplayAbilityActorInfo actorInfo,
            GameplayTag eventTag,
            GameplayEventData eventPayload,
            AbilitySystemComponent abilitySystemComponent)
        {
            throw new NotImplementedException();
        }
        
        #region Ability Cancelling/Interrupts

        /// <summary>
        /// Cancels the specified ability CDO.
        /// </summary>
        /// <param name="ability"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void CancelAbility(GameplayAbility ability)
        {
            using var _ = new ScopedAbilityListLock(this);
            for (int i = 0; i < ActivatableAbilities.Items.Count; i++)
            {
                var abilitySpec = ActivatableAbilities.Items[i];
                if (abilitySpec.Ability == ability)
                {
                    CancelAbilitySpec(ref abilitySpec, null);
                }
            }
        }

        /// <summary>
        /// Cancels the ability indicated by passed in spec handle. If handle is not found among reactivated abilities nothing happens.
        /// </summary>
        /// <param name="abilityHandle"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void CancelAbilityHandle(ref GameplayAbilitySpecHandle abilityHandle)
        {
            using var _ = new ScopedAbilityListLock(this);
            for (int i = 0; i < ActivatableAbilities.Items.Count; i++)
            {
                var abilitySpec = ActivatableAbilities.Items[i];
                if (abilitySpec.Handle == abilityHandle)
                {
                    CancelAbilitySpec(ref abilitySpec, null);
                }
            }
        }

        /// <summary>
        /// Cancel all abilities with the specified tags. Will not cancel the Ignore instance
        /// </summary>
        /// <param name="withTag"></param>
        /// <param name="withoutTag"></param>
        /// <param name="ignoredAbility"></param>
        public void CancelAbilities(GameplayTagContainer withTag, GameplayTagContainer withoutTag,
            GameplayAbility ignoredAbility = null)
        {
            using var _ = new ScopedAbilityListLock(this);
            for (int i = 0; i < ActivatableAbilities.Items.Count; i++)
            {
                var abilitySpec = ActivatableAbilities.Items[i];
                if (!abilitySpec.IsActive() || abilitySpec.Ability == null)
                {
                    continue;
                }
                
                bool withTagPass = withTag == null || abilitySpec.Ability.AbilityTags.HasAny(withTag);
                bool withoutTagPass = withoutTag == null || !abilitySpec.Ability.AbilityTags.HasAny(withoutTag);
                if (withTagPass && withoutTagPass)
                {
                    CancelAbilitySpec(ref abilitySpec, ignoredAbility);
                }
            }
        }
        
        /// <summary>
        /// Cancel a specific ability spec
        /// </summary>
        /// <param name="abilitySpec"></param>
        /// <param name="ignoredAbility"></param>
        protected virtual void CancelAbilitySpec(ref GameplayAbilitySpec abilitySpec,
            GameplayAbility ignoredAbility = null)
        {
            GameplayAbilityActorInfo ActorInfo = AbilityActorInfo;
            if (abilitySpec.Ability.GetInstancingPolicy() != EGameplayAbilityInstancingPolicy.NonInstanced)
            {
                
            }
            else
            {
                //abilitySpec.Ability.CancelAbility();
            }
        }

        /// <summary>
        /// Cancels all abilities regardless of tags. Will not cancel the ignore instance 
        /// </summary>
        /// <param name="ignoredAbility"></param>
        public void CancelAllAbilities(GameplayAbility ignoredAbility = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Cancels all abilities and kills any remaining instanced abilities 
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void DestroyActiveState()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called from ability activation or native code, will apply the correct ability blocking tags and cancel existing abilities. Subclasses can override the behavior 
        /// </summary>
        /// <param name="abilityTags">The tags of the ability that has block and cancel flags</param>
        /// <param name="requestingAbility">The gameplay ability requesting the change, can be NULL for native events</param>
        /// <param name="bEnableBlockTags">If true will enable the block tags, if false will disable the block tags</param>
        /// <param name="blockTags">What tags to block</param>
        /// <param name="bExecuteCancelTags">If true will cancel abilities matching tags</param>
        /// <param name="cancelTags">what tags to cancel</param>
        public virtual void ApplyAbilityBlockAndCancelTags(GameplayTagContainer abilityTags,
            GameplayAbility requestingAbility,
            bool bEnableBlockTags, GameplayTagContainer blockTags,
            bool bExecuteCancelTags, GameplayTagContainer cancelTags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called when an ability is cancellable or not. Doesn't do anything by default, can be overridden to tie into gameplay events
        /// </summary>
        /// <param name="abilityTags"></param>
        /// <param name="requestingAbility"></param>
        /// <param name="bCanBeCanceled"></param>
        public virtual void HandleChangeAbilityCanBeCanceled(GameplayTagContainer abilityTags,
            GameplayAbility requestingAbility,
            bool bCanBeCanceled)
        {
            
        }

        /// <summary>
        /// Returns true if any passed in tags are blocked
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public virtual bool AreAbilityTagsBlocked(GameplayTagContainer tags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Block  for specific ability tags
        /// </summary>
        /// <param name="tags"></param>
        public void BlockAbilitiesWithTags(GameplayTagContainer tags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// cancel blocking for specific ability tags
        /// </summary>
        /// <param name="tags"></param>
        public void UnBlockAbilitiesWithTags(GameplayTagContainer tags)
        {
            throw new NotImplementedException();
        }
        
        #endregion

        #region Functions meant to be called from GameplayAbility and subclasses, but not meant for general use

        /// <summary>
        /// Returns the list of all activatable abilities.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<GameplayAbilitySpec> GetActivatableAbilities()
        {
            throw new NotImplementedException();
        }

        public float GetAbilityLastActivatedTime()
        {
            throw new NotImplementedException();
        }

        public bool FindAbilitySpecFromHandle(out GameplayAbilitySpec abilitySpec,
            GameplayAbilitySpecHandle handle, 
            EConsiderPending considerPending = EConsiderPending.PendingRemove)
        {
            throw new NotImplementedException();
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public GameplayAbilitySpec FindAbilitySpecFromGEHandle(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Returns all ability spec handles granted from a GE handle. Only the server may call this function.
        /// </summary>
        /// <param name="scopeLock"></param>
        /// <param name="handle">The handle of the Active Gameplay Effect which granted the abilities we are looking for</param>
        /// <param name="considerPending">Are we returning AbilitySpecs that are pending for addition/removal?</param>
        /// <returns></returns>
        public List<GameplayAbilitySpec> FindAbilitySpecsFromGEHandle(ref ScopedAbilityListLock scopeLock, 
            ActiveGameplayEffectHandle handle, 
            EConsiderPending considerPending = EConsiderPending.PendingRemove)
        {
            throw new NotImplementedException();
        }

        public GameplayAbilitySpec FindAbilitySpecFromClass<T>(T inAbilityClass) where T : GameplayAbility
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Build a simple FGameplayAbilitySpec from class, level and optional Input ID
        /// </summary>
        /// <param name="inAbilityClass"></param>
        /// <param name="level"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual GameplayAbilitySpec BuildAbilitySpecFromClass<T>(T inAbilityClass, int level = 0) where T : GameplayAbility
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an array with all granted ability handles
        /// </summary>
        /// <param name="outAbilities"></param>
        public void GetAllAbilities(List<GameplayAbilitySpecHandle> outAbilities)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an array with all abilities that match the provided tags
        /// </summary>
        /// <param name="outAbilities"></param>
        /// <param name="tags"></param>
        /// <param name="bExactMatch"></param>
        public void FindAllAbilitiesWithTags(List<GameplayAbilitySpec> outAbilities, GameplayTagContainer tags, bool bExactMatch = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an array with all abilities that match the provided Gameplay Tag Query
        /// </summary>
        /// <param name="outAbilities"></param>
        /// <param name="tagQuery"></param>
        public void FindAllAbilitiesMatchingQuery(List<GameplayAbilitySpec> outAbilities, GameplayTagQuery tagQuery)
        {
            throw new NotImplementedException();
        }

        public GameplayEffectContextHandle GetEffectContextFromActiveGEHandle(ActiveGameplayEffectHandle handle)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Call to mark that an ability spec has been modified
        /// </summary>
        /// <param name="abilitySpec"></param>
        /// <param name="wasAddOrRemove"></param>
        public void MarkAbilitySpecDirty(ref GameplayAbilitySpec abilitySpec, bool wasAddOrRemove = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Failure tags used by InternalTryActivateAbility (E.g., this stores the  FailureTags of the last call to InternalTryActivateAbility
        /// </summary>
        protected GameplayTagContainer _internalTryActivateAbilityFailureTags;
        
        internal bool InternalTryActivateAbility(GameplayAbilitySpecHandle abilityToActivate, PredictionKey predictionKey,
            GameplayAbility outInstancedAbility,
            Action<GameplayAbility> onGameplayAbilityEndedDelegate,
            GameplayEventData eventData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called from the ability to let the component know it is ended 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="ability"></param>
        /// <param name="bWasCancelled"></param>
        public virtual void NotifyAbilityEnded(GameplayAbilitySpecHandle handle, GameplayAbility ability,
            bool bWasCancelled)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="activationInfo"></param>
        public  void ClearAbilityReplicatedDataCache(GameplayAbilitySpecHandle handle,  ref GameplayAbilityActivationInfo activationInfo)
        {
            throw new NotImplementedException();
        }
        
        internal void IncrementAbilityListLock()
        {
            AbilityScopeLockCount++;
        }
        
        internal void DecrementAbilityListLock()
        {
            AbilityScopeLockCount--;
            if (AbilityScopeLockCount == 0)
            {
                if (bAbilityPendingClearAll)
                {
                    ClearAllAbilities();

                    if (AbilityPendingAdds.Count > 0)
                    {
                        Ability_Warning("GiveAbility and ClearAllAbilities were both called within an ability scope lock. Prioritizing clear all abilities by ignoring pending adds.");
                        AbilityPendingAdds.Clear();
                    }
                }
                else if(AbilityPendingAdds.Count > 0 || AbilityPendingRemoves.Count > 0)
                {
                    using var _ = new AbilityListLockActiveChange(this, ref this._abilityPendingAdds, ref this._abilityPendingRemoves);
                    for (int i = 0; i < AbilityPendingAdds.Count; i++)
                    {
                        var spec = AbilityPendingAdds[i];
                        if (spec.IsActivateOnce)
                        {
                            GiveAbilityAndActivateOnce(ref spec, spec.GameplayEventData);
                        }
                        else
                        {
                            GiveAbility(ref spec);
                        }
                    }

                    for (int i = 0; i < AbilityPendingRemoves.Count; i++)
                    {
                        var handle = AbilityPendingRemoves[i];
                        ClearAbility(ref handle);
                    }
                }
            }
        }
        
        #endregion

        /// <summary>
        /// Full list of all instance-per-execution gameplay abilities associated with this component
        /// </summary>
        /// <returns></returns>
        protected List<GameplayAbility> GetReplicatedInstancedAbilities()
        {
            return AllReplicatedInstancedAbilities;
        }

        /// <summary>
        ///  Add a gameplay ability associated to this component
        /// </summary>
        /// <param name="gameplayAbility"></param>
        protected void AddReplicatedInstancedAbility(GameplayAbility gameplayAbility)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove a gameplay ability associated to this component
        /// </summary>
        /// <param name="gameplayAbility"></param>
        protected void RemoveReplicatedInstancedAbility(GameplayAbility gameplayAbility)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Unregister all the gameplay abilities of this component
        /// </summary>
        protected void RemoveAllReplicatedInstancedAbilities()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Will be called from GiveAbility or from OnRep. Initializes events (triggers and inputs) with the given ability
        /// </summary>
        protected virtual void OnGiveAbility(ref GameplayAbilitySpec abilitySpec)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Will be called from RemoveAbility or from OnRep. Unbinds inputs with the given ability 
        /// </summary>
        /// <param name="abilitySpec"></param>
        protected virtual void OnRemoveAbility(ref GameplayAbilitySpec abilitySpec)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called from ClearAbility, ClearAllAbilities or OnRep. Clears any triggers that should no longer exist.
        /// </summary>
        protected void CheckForClearedAbilities()
        {
            foreach (var triggered in GameplayEventTriggeredAbilities)
            {
                for (int i = triggered.Value.Count - 1; i >= 0; i--)
                {
                    if (!FindAbilitySpecFromHandle(out _, triggered.Value[i]))
                    {
                        triggered.Value.RemoveAt(i);
                    }
                }
            }

            foreach (var triggered in OwnedTagTriggeredAbilities)
            {
                bool bRemovedTrigger = false;
                for (int i = triggered.Value.Count - 1; i >= 0; i--)
                {
                    if (!FindAbilitySpecFromHandle(out _, triggered.Value[i]))
                    {
                        triggered.Value.RemoveAt(i);
                        bRemovedTrigger = true;
                    }
                }

                if (bRemovedTrigger && triggered.Value.Count == 0)
                {
                    // If we removed all triggers, remove the callback
                }
            }
            
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new instance of an ability, storing it in the spec
        /// </summary>
        /// <param name="ability"></param>
        /// <param name="abilitySpec"></param>
        /// <returns></returns>
        protected GameplayAbility CreateNewInstanceOfAbility(GameplayAbility ability, ref GameplayAbilitySpec abilitySpec)
        {
            if (ability == null)
            {
                throw new ArgumentNullException(nameof(ability));
            }
            
            GameplayAbility abilityInstance = NewObject<GameplayAbility>(ability.GetType());
            
            if (abilityInstance.GetReplicationPolicy() != EGameplayAbilityReplicationPolicy.ReplicateNo)
            {
                abilitySpec.ReplicatedInstances.Add(abilityInstance);
            }
            else
            {
                abilitySpec.NonReplicatedInstances.Add(abilityInstance);
            }
            
            return abilityInstance;
        }


        protected virtual void MonitoredTagChanged(ref GameplayTag tag, int newCount)
        {
            using var _ = new ScopedAbilityListLock(this);
            if (!OwnedTagTriggeredAbilities.TryGetValue(tag, out var triggeredAbilityHandles))
            {
                return;
            }
            
            foreach (var abilityHandle in triggeredAbilityHandles)
            {
                if (!FindAbilitySpecFromHandle(out var abilitySpec, abilityHandle)
                    || !HasNetworkAuthorityToActivateTriggeredAbility(ref abilitySpec))
                {
                    continue;
                }

                if (abilitySpec.Ability == null)
                {
                    continue;
                }
                
                foreach (var triggerData in abilitySpec.Ability.AbilityTriggers)
                {
                    GameplayTag eventTag = triggerData.TriggerTag;
                    if (eventTag == tag)
                    {
                        if (newCount > 0)
                        {
                            // Populate event data so this will use the same blueprint node to activate as gameplay triggers
                            GameplayEventData eventData = new GameplayEventData()
                            {
                                EventTag = eventTag,
                                EventMagnitude = newCount,
                                Instigator = GetOwnerActor(),
                                Target = GetOwnerActor(),
                            };
                            
                            // Try to activate it
                            InternalTryActivateAbility(abilitySpec.Handle, new PredictionKey(), null, null, eventData);
                        }
                        else if (newCount == 0 && triggerData.TriggerSource == EGameplayAbilityTriggerSource.OwnedTagPresent)
                        {
                            // Try to cancel, but only if the type is right
                            CancelAbilitySpec(ref abilitySpec, null);
                        }
                    }
                }
            }
        }
        
        protected bool HasNetworkAuthorityToActivateTriggeredAbility(ref GameplayAbilitySpec  spec)
        {
            bool bIsAuthority = IsOwnerActorAuthoritative();
            bool bIsLocal = AbilityActorInfo.IsLocallyControlled();
            switch (spec.Ability.GetNetExecutionPolicy())
            {
                case EGameplayAbilityNetExecutionPolicy.LocalPredicted:
                case EGameplayAbilityNetExecutionPolicy.LocalOnly:
                    return bIsLocal;
                case EGameplayAbilityNetExecutionPolicy.ServerInitiated:
                case EGameplayAbilityNetExecutionPolicy.ServerOnly:
                    return bIsAuthority;
            }
            return false;
        }
        #endregion

    }
}