using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bliss.Base.Event;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    using OnGameplayEffectTagCountChanged = Action<GameplayTag, int>;
    

    /// <summary>
    /// Indicates what type of action happened to a specific gameplay cue tag. Sometimes you will get multiple events at once
    /// </summary>
    public enum EGameplayCueEvent
    {
        /** Called when a GameplayCue with duration is first activated, this will only be called if the client witnessed the activation */
        OnActive,

        /** Called when a GameplayCue with duration is first seen as active, even if it wasn't actually just applied (Join in progress, etc) */
        WhileActive,

        /** Called when a GameplayCue is executed, this is used for instant effects or periodic ticks */
        Executed,

        /** Called when a GameplayCue with duration is removed */
        Removed
    }


    //public delegate void OnGameplayEffectTagCountChanged(GameplayTag tag, int count);
    public delegate void OnGivenActiveGameplayEffectRemoved(ref ActiveGameplayEffect activeGameplayEffect);
    
    public delegate void OnActiveGameplayEffectRemoved_Info(ref GameplayEffectRemovalInfo removalInfo);
    public delegate void OnActiveGameplayEffectStackChange(ActiveGameplayEffectHandle handle, int newStackCount, int previousStackCount);
    public delegate void OnActiveGameplayEffectTimeChange(ActiveGameplayEffectHandle handle, float newStartTime, float newDuration);
    public delegate void OnActiveGameplayEffectInhibitionChanged(ActiveGameplayEffectHandle handle, bool isInhibited);

    /// <summary>
    ///  Callback struct for different types of gameplay effect changes
    /// </summary>
    public class ActiveGameplayEffectEvents
    {
        public event OnActiveGameplayEffectRemoved_Info OnEffectRemoved;
        public event OnActiveGameplayEffectStackChange OnStackChanged;
        public event OnActiveGameplayEffectTimeChange OnTimeChanged;
        public event OnActiveGameplayEffectInhibitionChanged OnInhibitionChanged;
    }
    
    public enum EGameplayTagEventType
    {
        /** Event only happens when tag is new or completely removed */
        NewOrRemoved,

        /** Event happens any time tag "count" changes */
        AnyCountChange		
    }
    
    public class GameplayTagCountContainer
    {
        /// <summary>
        /// Map of tag to delegate that will be fired when the count for the key tag changes to or away from zero
        /// </summary>
        private Dictionary<GameplayTag, DelegateInfo> GameplayTagEventMap;
        
        /// <summary>
        /// Map of tag to active count of that tag 
        /// </summary>
        private Dictionary<GameplayTag, int> GameplayTagCountMap;
        
        /// <summary>
        ///  Map of tag to explicit count of that tag. Cannot share with above map because it's not safe to merge explicit and generic counts
        /// </summary>
        private Dictionary<GameplayTag, int> ExplicitTagCountMap;
        
        /// <summary>
        /// Container of tags that were explicitly added
        /// </summary>
        private GameplayTagContainer ExplicitTags;
        
        /// <summary>
        /// Delegate fired whenever any tag's count changes to or away from zero
        /// </summary>
        private event OnGameplayEffectTagCountChanged OnAnyTagChangeDelegate;

        /// <summary>
        /// Check if the count container has a gameplay tag that matches against the specified tag (expands to include parents of asset tags)
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasMatchingGameplayTag(GameplayTag tag)
        {
            return GameplayTagCountMap.TryGetValue(tag, out int count) && count > 0;
        }

        /// <summary>
        /// Check if the count container has gameplay tags that matches against all of the specified tags (expands to include parents of asset tags)
        /// </summary>
        /// <param name="tagContainer"></param>
        /// <returns></returns>
        public bool HasAllMatchingGameplayTags(GameplayTagContainer tagContainer)
        {
            if (tagContainer.Num() == 0)
            {
                return true;
            }

            bool AllMatch = true;
            foreach (GameplayTag tag in tagContainer.GameplayTags)
            {
                if (!GameplayTagCountMap.TryGetValue(tag, out int count) || count <= 0)
                {
                    AllMatch = false;
                    break;
                }
            }		
            return AllMatch;
        }

        /// <summary>
        /// Check if the count container has gameplay tags that matches against any of the specified tags (expands to include parents of asset tags)
        /// </summary>
        /// <param name="tagContainer"></param>
        /// <returns></returns>
        public bool HasAnyMatchingGameplayTags(GameplayTagContainer tagContainer)
        {
            if (tagContainer.Num() == 0)
            {
                return false;
            }

            bool AnyMatch = false;
            foreach (GameplayTag tag in tagContainer)
            {
                if (GameplayTagCountMap.TryGetValue(tag, out int count) && count > 0)
                {
                    AnyMatch = true;
                    break;
                }
            }		
            return AnyMatch;
        }

        public void UpdateTagCount(GameplayTagContainer tagContainer, int countDelta)
        {
            if (countDelta != 0)
            {
                bool bUpdatedAny = false;
                List<Action> deferredTagChangeDelegates =  new List<Action>();
                foreach (GameplayTag tag in tagContainer)
                {
                    GameplayTag thisTag = tag;
                    bUpdatedAny |= UpdateTagMapDeferredParentRemoval_Internal(ref thisTag, countDelta, deferredTagChangeDelegates);
                }

                if (bUpdatedAny && countDelta < 0)
                {
                    ExplicitTags.FillParentTags();
                }

                foreach (var deferredTagChangeDelegate in deferredTagChangeDelegates)
                {
                    deferredTagChangeDelegate();
                }
            }
        }
        
        public bool UpdateTagCount(ref GameplayTag tag, int countDelta)
        {
            if (countDelta != 0)
            {
                return UpdateTagMap_Internal(ref tag, countDelta);
            }
            
            return false;
        }

        
        /// <summary>
        /// Update the specified tag by the specified delta, potentially causing an additional or removal from the explicit tag list.
        /// Calling code MUST call FillParentTags followed by executing the returned delegates.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="countDelta"></param>
        /// <param name="deferredTagChangeDelegates"></param>
        /// <returns></returns>
        public bool UpdateTagCount_DeferredParentRemoval(ref GameplayTag tag,
            int countDelta,
            List<Action> deferredTagChangeDelegates)
        {
            if (countDelta != 0)
            {
                return UpdateTagMapDeferredParentRemoval_Internal(ref tag, countDelta, deferredTagChangeDelegates);
            }
            return false;
        }

        
        /// <summary>
        ///  Set the specified tag count to a specific value
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="newCount"></param>
        /// <returns></returns>
        public bool SetTagCount(ref GameplayTag tag, int newCount)
        {
            ExplicitTagCountMap.TryGetValue(tag, out int existingCount);

            int CountDelta = newCount - existingCount;
            if (CountDelta != 0)
            {
                return UpdateTagMap_Internal(ref tag, CountDelta);
            }

            return false;
        }

        /// <summary>
        /// return the hierarchical count for a specified tag
        /// e.g. if A.B & A.C were added, GetTagCount("A") would return 2.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public int GetTagCount(ref GameplayTag tag)
        {
            GameplayTagCountMap.TryGetValue(tag, out int existingCount);
            return existingCount;
        }
        
        /// <summary>
        /// return how many times the exact specified tag has been added to the container (ignores the tag hierarchy)
        /// e.g. if A.B & A.C were added, GetExplicitTagCount("A") would return 0, and GetExplicitTagCount("A.B") would return 1.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public int GetExplicitTagCount(ref GameplayTag tag)
        {
            ExplicitTagCountMap.TryGetValue(tag, out int existingCount);
            return existingCount;
        }
        
        /// <summary>
        /// Broadcasts the AnyChange event for this tag. This is called when the stack count of the backing gameplay effect change.
        ///	It is up to the receiver of the broadcasted delegate to decide what to do with this.
        /// </summary>
        /// <param name="tag"></param>
        public void Notify_StackCountChange(ref GameplayTag tag)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Return delegate that can be bound to for when the specific tag's count changes to or off of zero
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="eventType"></param>
        public void RegisterGameplayTagEvent(OnGameplayEffectTagCountChanged  callback, ref GameplayTag tag, EGameplayTagEventType eventType = EGameplayTagEventType.NewOrRemoved)
        {
            if (GameplayTagEventMap.TryGetValue(tag, out var eventInfo))
            {
                if (eventType == EGameplayTagEventType.NewOrRemoved)
                {
                    eventInfo.OnNewOrRemove += callback;
                }
                else
                {
                    eventInfo.OnAnyChange  += callback;
                }
            }
        }

        /// <summary>
        /// Return delegate that can be bound to for when the any tag's count changes to or off of zero
        /// </summary>
        /// <returns></returns>
        public  void  RegisterGenericGameplayEvent(OnGameplayEffectTagCountChanged tagCountDelegate)
        {
            OnAnyTagChangeDelegate = tagCountDelegate;
        }

        /// <summary>
        /// Simple accessor to the explicit gameplay tag list
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetExplicitGameplayTags()
        {
            return ExplicitTags;
        }

        /// <summary>
        /// Removes all of the tags. Does not notify any delegates.
        /// </summary>
        /// <param name="bResetCallbacks"></param>
        public void Reset(bool bResetCallbacks = true)
        {
            GameplayTagCountMap.Clear();
            ExplicitTagCountMap.Clear();
            ExplicitTags.Reset();
            
            if (bResetCallbacks)
            {
                GameplayTagEventMap.Clear();
                OnAnyTagChangeDelegate = null;
            }
        }
        
        /// <summary>
        /// Fills in ParentTags from GameplayTags
        /// </summary>
        public 	void FillParentTags()
        {
            ExplicitTags.FillParentTags();
        }

        /// <summary>
        /// Internal helper function to adjust the explicit tag list & corresponding maps/delegates/etc. as necessary
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="countDelta"></param>
        /// <returns></returns>
        private bool UpdateTagMap_Internal(ref GameplayTag tag, int countDelta)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Internal helper function to adjust the explicit tag list & corresponding maps/delegates/etc. as necessary. This does not call FillParentTags or any of the tag change delegates. These delegates are returned and must be executed by the caller.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="countDelta"></param>
        /// <param name="deferredTagChangeDelegates"></param>
        /// <returns></returns>
        private bool UpdateTagMapDeferredParentRemoval_Internal(ref GameplayTag tag,
            int countDelta,
            List<Action> deferredTagChangeDelegates)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Internal helper function to adjust the explicit tag list & corresponding map.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="countDelta"></param>
        /// <param name="bDeferParentTagsOnRemove"></param>
        /// <returns></returns>
        private bool UpdateExplicitTags(ref GameplayTag tag, int countDelta, bool bDeferParentTagsOnRemove)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Internal helper function to collect the delegates that need to be called when Tag has its count changed by CountDelta.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="countDelta"></param>
        /// <param name="deferredTagChangeDelegates"></param>
        /// <returns></returns>
        private bool GatherTagChangeDelegates(ref GameplayTag tag, int countDelta, List<Action> deferredTagChangeDelegates)
        {
            throw new NotImplementedException();
        }
        
        private class DelegateInfo
        {
            //public OnGameplayEffectTagCountChanged OnNewOrRemove;
            //public OnGameplayEffectTagCountChanged OnAnyChange;
            public AutoEventAction<GameplayTag, int> OnNewOrRemove;
            public AutoEventAction<GameplayTag, int> OnAnyChange;
        }
    }

    /// <summary>
    /// Structure used to combine tags from different sources during effect execution
    /// </summary>
    public class TagContainerAggregator
    {
        private GameplayTagContainer CapturedActorTags;
        
        private GameplayTagContainer CapturedSpecTags;
        
        private GameplayTagContainer CachedAggregator;
        
        private bool CacheIsValid;

        /// <summary>
        /// Returns tags from the source or target actor 
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetActorTags()
        {
            CacheIsValid = false;
            return CapturedActorTags;
        }
        
        /// <summary>
        /// Get tags that came from the effect spec 
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetSpecTags()
        {
            CacheIsValid = false;
            return CapturedSpecTags;
        }
        
        /// <summary>
        /// Returns tags from the source or target actor 
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetAggregatedTags()
        {
            if (CacheIsValid == false)
            {
                CacheIsValid  = true;
                CachedAggregator.Reset();
                CachedAggregator.AppendTags(CapturedActorTags);
                CachedAggregator.AppendTags(CapturedSpecTags);
            }
            return CapturedActorTags;
        }
    }
}