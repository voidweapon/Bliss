using System;
using System.Collections.Generic;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public class GameplayEffect
    {
	    /// <summary>
	    /// Policy for the duration of this effect
	    /// </summary>
	    public EGameplayEffectDurationType DurationPolicy;

	    /// <summary>
	    /// If true, GameplayCues will only be triggered for the first instance in a stacking GameplayEffect.
	    /// </summary>
	    public bool bSuppressStackingCues;
	    
        // ----------------------------------------------------------------------
        //	Tag Containers
        // ----------------------------------------------------------------------
        
        
        // ----------------------------------------------------------------------
        //	Stacking
        // ----------------------------------------------------------------------
        public int StackLimitCount;
        
        // ----------------------------------------------------------------------
        //	Cached Component Data - Do not modify these at runtime!
        //	If you want to manipulate these, write your own GameplayEffectComponent
        //	set the data during PostLoad.
        // ----------------------------------------------------------------------
        
        /// <summary>
        /// Cached copy of all the tags this GE has. Data populated during PostLoad.
        /// </summary>
        public GameplayTagContainer CachedAssetTags;
        
        /// <summary>
        /// Cached copy of all the tags this GE grants to its target. Data populated during PostLoad.
        /// </summary>
        public GameplayTagContainer CachedGrantedTags;

       /// <summary>
       /// Cached copy of all the tags this GE applies to its target to block Gameplay Abilities. Data populated during PostLoad.
       /// </summary>
       public GameplayTagContainer CachedBlockedAbilityTags;
        
       /// <summary>
       /// Returns all tags that this GE *has* and *does not* grant to any Actor.
       /// </summary>
       /// <returns></returns>
       public GameplayTagContainer GetAssetTags()
       {
	       return CachedAssetTags;
       }
       
        /// <summary>
        /// Returns all tags granted to the Target Actor of this gameplay effect. That is to say: If this GE is applied to an Actor, they get these tags. */
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetGrantedTags()
        {
            return CachedGrantedTags;
        }
        
        /// <summary>
        /// Returns all blocking ability tags granted by this gameplay effect definition. Blocking tags means the target will not be able to execute an ability with this asset tag.
        /// </summary>
        /// <returns></returns>
        public GameplayTagContainer GetBlockedAbilityTags()
        {
	        return CachedBlockedAbilityTags;
        }

        /// <summary>
        /// Returns the maximum stack size for this gameplay effect
        /// </summary>
        /// <returns></returns>
        public int GetStackLimitCount()
        {
	        return StackLimitCount;
        }

        public bool CanApply(ActiveGameplayEffectsContainer activeGEContainer, ref GameplayEffectSpec geSpec)
        {
	        throw new NotImplementedException();
        }

        /// <summary>
        /// Receive a notify that this GameplayEffect has been applied (this GE is either previously added to the container or executed in such cases).
        /// However, this also encompasses cases where a GE was added to a container previously and then applied again to 'stack'.  It does not happen for periodic executions of a duration GE.
        /// </summary>
        public void OnApplied(ActiveGameplayEffectsContainer activeGEContainer, ref GameplayEffectSpec geSpec, ref PredictionKey predictionKey)
        {
	        throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Every set condition within this query must match in order for the query to match. i.e. individual query elements are ANDed together.
    /// </summary>
    public struct GameplayEffectQuery
    {
        /// <summary>
        /// Native delegate for providing custom matching conditions.
        /// </summary>
        public Func<ActiveGameplayEffect, bool> CustomMatchDelegate;
        
        /// <summary>
        ///  Query that is matched against tags this GE gives
        /// </summary>
        public GameplayTagQuery OwningTagQuery;
        
        /// <summary>
        /// Query that is matched against tags this GE has 
        /// </summary>
        public GameplayTagQuery EffectTagQuery;
        
        /// <summary>
        ///  Query that is matched against spec tags the source of this GE has
        /// </summary>
        public GameplayTagQuery SourceTagQuery;
        
        /// <summary>
        /// Query that is matched against all tags the source of this GE has
        /// </summary>
        public GameplayTagQuery SourceAggregateTagQuery;
        
        //public GameplayAttribute ModifyingAttribute;

        /// <summary>
        /// Matches on GameplayEffects which come from this source
        /// </summary>
        public object EffectSource;

        /// <summary>
        /// Matches on GameplayEffects with this definition
        /// </summary>
        public Type EffectDefinition;

        /// <summary>
        /// Handles to ignore as matches, even if other criteria is met 
        /// </summary>
        public List<ActiveGameplayEffectHandle> IgnoreHandles;

        /// <summary>
        /// Returns true if Effect matches all specified criteria of this query, including CustomMatch delegates if bound. Returns false otherwise.
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public bool Matches(ref ActiveGameplayEffect effect)
        {
	        // since all of these query conditions must be met to be considered a match, failing
	        // any one of them means we can return false

	        // Anything in the ignore handle list is an immediate non-match

	        if (IgnoreHandles != null && IgnoreHandles.Contains(effect.Handle))
	        {
		        return false;
	        }

	        if (CustomMatchDelegate != null)
	        {
		        if (CustomMatchDelegate.Invoke(effect) == false)
		        {
			        return false;
		        }
	        }

	        return Matches(ref effect.Spec);
        }

        private static GameplayTagContainer _sTags = new GameplayTagContainer();
        /// <summary>
        /// Returns true if Effect matches all specified criteria of this query. This does NOT check FActiveGameplayEffectQueryCustomMatch since this is performed on the spec (possibly prior to applying).
        /// Note: it would be reasonable to support a custom delegate that operated on the FGameplayEffectSpec itself.
        /// </summary>
        /// <param name="effectSpec"></param>
        /// <returns></returns>
        public bool Matches(ref GameplayEffectSpec effectSpec)
        {
	        if (effectSpec.Def == null)
	        {
		        return false;
	        }

	        if (OwningTagQuery != null && !OwningTagQuery.IsEmpty())
	        {
		        // Combine tags from the definition and the spec into one container to match queries that may span both
		        // static to avoid memory allocations every time we do a query
		        var targetTags = _sTags;
		        targetTags.Reset();
		        
		        // This functionality is kept the same as prior to UE5.3:
		        // I believe this is a side-effect of not understanding the previous Asset vs. Granted tags in UGameplayEffect.
		        // We're keeping the functionality and actually comparing TargetTags against both Asset & Granted.
		        // The one change in 5.3: We're now also comparing against DynamicAssetTags to keep it consistent.
		        effectSpec.GetAllAssetTags(targetTags);
		        effectSpec.GetAllGrantedTags(targetTags);

		        if (OwningTagQuery.Matches(targetTags) == false)
		        {
			        return false;
		        }
	        }

	        if (EffectTagQuery != null && !EffectTagQuery.IsEmpty())
	        {
		        // Combine tags from the definition and the spec into one container to match queries that may span both
		        // static to avoid memory allocations every time we do a query
		        var GETags = _sTags;
		        GETags.Reset();
		        
		        effectSpec.GetAllAssetTags(GETags);
		        
		        if (EffectTagQuery.Matches(GETags) == false)
		        {
			        return false;
		        }
	        }

	        if (SourceAggregateTagQuery != null && !SourceAggregateTagQuery.IsEmpty())
	        {
		        GameplayTagContainer sourceAggregateTags = effectSpec.CapturedSourceTags.GetAggregatedTags();
		        if (SourceAggregateTagQuery.Matches(sourceAggregateTags) == false)
		        {
			        return false;
		        }
	        }

	        if (SourceTagQuery != null && !SourceTagQuery.IsEmpty())
	        {
		        GameplayTagContainer sourceSpecTags = effectSpec.CapturedSourceTags.GetSpecTags();
		        if (SourceTagQuery.Matches(sourceSpecTags) == false)
		        {
			        return false;
		        }
	        }
	        
	        // if we are looking for ModifyingAttribute go over each of the Spec Modifiers and check the Attributes

	        
	        //-----------------

	        //check source object
	        if (EffectSource != null)
	        {
		        if (effectSpec.GetContext().GetSourceObject() != EffectSource)
		        {
			        return false;
		        }
	        }
	        
	        // check definition
	        if (EffectDefinition != null)
	        {
		        if (effectSpec.Def.GetType() != EffectDefinition)
		        {
			        return false;
		        }
	        }

	        return true;
        }

        /// <summary>
        /// Returns true if the query is empty/default. E.g., it has no data set.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return OwningTagQuery.IsEmpty() 
                   && EffectTagQuery.IsEmpty()
                   && SourceTagQuery.IsEmpty()
                   && SourceAggregateTagQuery.IsEmpty()
                   && EffectSource == null
                   && EffectDefinition == null
                   && CustomMatchDelegate == null
                   && IgnoreHandles == null;
        }

        /** Creates an effect query that will match if there are any common tags between the given tags and an ActiveGameplayEffect's owning tags */
        public static GameplayEffectQuery MakeQuery_MatchAnyOwningTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.OwningTagQuery = GameplayTagQuery.MakeQuery_MatchAnyTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if all of the given tags are in the ActiveGameplayEffect's owning tags */
        public static GameplayEffectQuery MakeQuery_MatchAllOwningTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.OwningTagQuery = GameplayTagQuery.MakeQuery_MatchAllTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if there are no common tags between the given tags and an ActiveGameplayEffect's owning tags */
        public static GameplayEffectQuery MakeQuery_MatchNoOwningTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.OwningTagQuery = GameplayTagQuery.MakeQuery_MatchNoTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if there are any common tags between the given tags and an ActiveGameplayEffect's tags */
        public static GameplayEffectQuery MakeQuery_MatchAnyEffectTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.EffectTagQuery = GameplayTagQuery.MakeQuery_MatchAnyTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if all of the given tags are in the ActiveGameplayEffect's tags */
        public static GameplayEffectQuery MakeQuery_MatchAllEffectTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.EffectTagQuery = GameplayTagQuery.MakeQuery_MatchAllTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if there are no common tags between the given tags and an ActiveGameplayEffect's tags */
        public static GameplayEffectQuery MakeQuery_MatchNoEffectTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.EffectTagQuery = GameplayTagQuery.MakeQuery_MatchNoTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if there are any common tags between the given tags and an ActiveGameplayEffect's source spec tags */
        public static GameplayEffectQuery MakeQuery_MatchAnySourceSpecTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.SourceTagQuery = GameplayTagQuery.MakeQuery_MatchAnyTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if all of the given tags are in the ActiveGameplayEffect's source spec tags */
        public static GameplayEffectQuery MakeQuery_MatchAllSourceSpecTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.SourceTagQuery = GameplayTagQuery.MakeQuery_MatchAllTags(inTags);
	        return outQuery;
        }

        /** Creates an effect query that will match if there are no common tags between the given tags and an ActiveGameplayEffect's source spec tags */
        public static GameplayEffectQuery MakeQuery_MatchNoSourceSpecTags(GameplayTagContainer inTags)
        {
	        GameplayEffectQuery outQuery =  new GameplayEffectQuery();
	        outQuery.SourceTagQuery = GameplayTagQuery.MakeQuery_MatchNoTags(inTags);
	        return outQuery;
        }
    }

    /// <summary>
    /// This is a cut down version of the gameplay effect spec used for RPCs.
    /// </summary>
    public struct GameplayEffectSpecForRPC
    {
        public GameplayEffect Def;
        public GameplayEffectContextHandle EffectContext;
        public GameplayTagContainer AggregatedSourceTags;
        public GameplayTagContainer AggregatedTargetTags;
        public 	float Level;
        public float AbilityLevel;
    }

    public struct ActiveGameplayEffect
    {
	    /// <summary>
	    /// Globally unique ID for identify this active gameplay effect. Can be used to look up owner. Not networked.
	    /// </summary>
        public ActiveGameplayEffectHandle Handle;
        
        /// <summary>
        /// 
        /// </summary>
        public GameplayEffectSpec Spec;
        
        public PredictionKey PredictionKey;

        /// <summary>
        /// Handles of Gameplay Abilities that were granted to the target by this Active Gameplay Effect
        /// </summary>
        public List<GameplayAbilitySpecHandle> GrantedAbilityHandles;
        
        /// <summary>
        /// Server time this started 
        /// </summary>
        public float StartServerWorldTime;

        /// <summary>
        ///  Used for handling duration modifications being replicated
        /// </summary>
        public float CachedStartServerWorldTime;

        public float StartWorldTime;

        public bool bIsInhibited;

        public bool IsPendingRemove;

        /// <summary>
        /// Last StackCount that the client had. Used to tell if the stackcount has changed in PostReplicatedChange
        /// </summary>
        public int ClientCachedStackCount;
        
        /// <summary>
        ///  All the bindable events for this active effect (bundled to allow easier non-const access to these events via the ASC) */
        /// </summary>
        public ActiveGameplayEffectEvents EventSet;

        public bool IsValid()
        {
	        return Handle.IsValid();
        }
    }
    
    public struct ActiveGameplayEffectQuery
    {
        
    }

    /// <summary>
    /// Active GameplayEffects Container
    ///		-Bucket of ActiveGameplayEffects
    ///		-Needed for FFastArraySerialization
    /// </summary>
    public class ActiveGameplayEffectsContainer
    {
	    private int ScopedLockCount;
	    private bool PendingRemoves;

	    public bool CanApplyAttributeModifiers(GameplayEffect gameplayEffect, float level, ref GameplayEffectContextHandle effectContext)
	    {
		    throw new NotImplementedException();
	    }
	    
	    public List<(float, float)> GetActiveEffectsTimeRemainingAndDuration(ref GameplayEffectQuery query)
	    {
		    throw new NotImplementedException();
	    }

	    public ActiveGameplayEffect ApplyGameplayEffectSpec(ref GameplayEffectSpec effectSpec, PredictionKey predictionKey, out bool bFoundExistingStackableGE)
	    {
		    throw new NotImplementedException();
	    }

	    public ActiveGameplayEffect GetActiveGameplayEffect(ActiveGameplayEffectHandle handle)
	    {
		    throw new NotImplementedException();
	    }

	    public bool RemoveActiveGameplayEffect(ActiveGameplayEffectHandle handle, int stacksToRemove = -1)
	    {
		    throw new NotImplementedException();
	    }
	    
	    public int RemoveActiveEffects(ref GameplayEffectQuery query, int stacksToRemove = -1)
	    {
		    // Force a lock because the removals could cause other removals earlier in the array, so iterating backwards is not safe all by itself
		    using var _ = new ScopedActiveGameplayEffectLock(this);

		    //int NumRemoved = 0;
		    
		    // Manually iterating through in reverse because this is a removal operation
		    throw new NotImplementedException();
	    }

	    public void IncrementLock()
	    {
		    ScopedLockCount++;
	    }

	    public void DecrementLock()
	    {
		    ScopedLockCount--;
		    
		    if (ScopedLockCount == 0)
		    {
			    
		    }
	    }
    }

    public struct GameplayEffectAttributeCaptureSpec
    {
        
    }

	/// <summary>
	/// Provides a mechanism for locking the active gameplay effect list while possibly invoking callbacks into gamecode.
	///	For example, if some internal code in FActiveGameplayEffectsContainer is iterating through the active GE list
	///	or holding onto a pointer to something in that list, any changes to that list could cause memory the move out from underneath.
	///
	///	This scope lock will queue deletions and additions until after the scope is over. The additions and deletions will actually 
	/// go through, but we will defer the memory operations to the active gameplay effect list.
	/// </summary>
    public struct ScopedActiveGameplayEffectLock : System.IDisposable
    {
	    private ActiveGameplayEffectsContainer _container;
	    public ScopedActiveGameplayEffectLock(ActiveGameplayEffectsContainer  inContainer)
	    {
		    _container = inContainer;
	    }

	    public void Dispose()
	    {
		    _container = null;
	    }
    }

	public struct ScopeCurrentGameplayEffectBeingApplied : System.IDisposable
	{
		public ScopeCurrentGameplayEffectBeingApplied(ref GameplayEffectSpec effectSpec, AbilitySystemComponent abilitySystem)
		{
			AbilitySystemGlobals.Instance.PushCurrentAppliedGE(ref effectSpec, abilitySystem);
		}

		public void Dispose()
		{
			AbilitySystemGlobals.Instance.PopCurrentAppliedGE();
		}
	}

	/// <summary>
	/// Gameplay effect duration policies
	/// </summary>
	public enum EGameplayEffectDurationType
	{
		/// <summary>
		///  This effect applies instantly
		/// </summary>
		Instant,
		/// <summary>
		///  This effect lasts forever
		/// </summary>
		Infinite,
		/// <summary>
		/// The duration of this effect will be specified by a magnitude
		/// </summary>
		HasDuration
	}
}