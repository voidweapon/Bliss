using System;
using System.Collections.Generic;
using Bliss.Gameplay.GameplayTags;
using Unity.Mathematics;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /// <summary>
    /// GameplayEffect Specification. Tells us:
    ///	-What UGameplayEffect (const data)
    ///	-What Level
    /// -Who instigated
    ///
    /// FGameplayEffectSpec is modifiable. We start with initial conditions and modifications be applied to it. In this sense, it is stateful/mutable but it
    /// is still distinct from an FActiveGameplayEffect which in an applied instance of an FGameplayEffectSpec.
    /// </summary>
    public struct GameplayEffectSpec
    {
        /// <summary>
        /// GameplayEffect definition. The static data that this spec points to.
        /// </summary>
        public GameplayEffect Def { get; private set; }
        
        /// <summary>
        /// The duration in seconds of this effect
        /// instantaneous effects should have a duration of FGameplayEffectConstants::INSTANT_APPLICATION
        /// effects that last forever should have a duration of FGameplayEffectConstants::INFINITE_DURATION
        /// </summary>
        public float Duration;
        
        public float Period;

        private int StackCount;

        /// <summary>
        /// Captured Source Tags on GameplayEffectSpec creation
        /// </summary>
        public TagContainerAggregator CapturedSourceTags;
        
        public GameplayTagContainer DynamicGrantedTags;

        public GameplayTagContainer DynamicAssetTags { get; private set; }

        /// <summary>
        /// Whether the spec has had its source attribute capture completed or not yet
        /// </summary>
        public bool bCompletedSourceAttributeCapture;
        
        /// <summary>
        ///  Whether the spec has had its target attribute capture completed or not yet
        /// </summary>
        public bool bCompletedTargetAttributeCapture;
        
        /// <summary>
        /// Whether the duration of the spec is locked or not; If it is, attempts to set it will fail 
        /// </summary>
        public bool bDurationLocked;

        /// <summary>
        /// Map of set by caller magnitudes
        /// </summary>
        public Dictionary<string, float> SetByCallerNameMagnitudes;
        
        /// <summary>
        /// Map of set by caller magnitudes
        /// </summary>
        public Dictionary<GameplayTag, float> SetByCallerTagMagnitudes;
        
        /// <summary>
        ///  This tells us how we got here (who / what applied us)
        /// </summary>
        private GameplayEffectContextHandle _effectContext;
        
        /// <summary>
        /// The level this effect was applied at
        /// </summary>
        private float _level;

        public GameplayEffectSpec(ref GameplayEffectSpec other)
        {
            Duration = other.Duration;
            Period = other.Period;
            StackCount = other.StackCount;
            CapturedSourceTags = other.CapturedSourceTags;
            DynamicGrantedTags = other.DynamicGrantedTags;
            bCompletedSourceAttributeCapture = other.bCompletedSourceAttributeCapture;
            bCompletedTargetAttributeCapture = other.bCompletedTargetAttributeCapture;
            bDurationLocked = other.bDurationLocked;
            SetByCallerNameMagnitudes = other.SetByCallerNameMagnitudes;
            SetByCallerTagMagnitudes = other.SetByCallerTagMagnitudes;
            _effectContext = other._effectContext;
            _level = other._level;
            Def = other.Def;
            DynamicAssetTags = other.DynamicAssetTags;
        }
        
        public GameplayEffectSpec(GameplayEffect gameplayEffect, 
            ref GameplayEffectContextHandle inEffectContext,
            float level)
        {
            Duration = 0;
            Period = 0;
            StackCount = 1;
            CapturedSourceTags = null;
            DynamicGrantedTags = null;
            bCompletedSourceAttributeCapture = false;
            bCompletedTargetAttributeCapture = false;
            bDurationLocked = false;
            SetByCallerNameMagnitudes = null;
            SetByCallerTagMagnitudes = null;
            _effectContext = default;
            _level = 0;
            Def = null;
            DynamicAssetTags = null;
            
            Initialize(gameplayEffect, ref inEffectContext, level);
        }


        /// <summary>
        /// Can be called manually, but it is preferred to use the 3 parameter constructor
        /// </summary>
        /// <param name="inDef"></param>
        /// <param name="inEffectContext"></param>
        /// <param name="level"></param>
        private void Initialize(GameplayEffect inDef,
            ref GameplayEffectContextHandle inEffectContext,
            float level)
        {
            Def =  inDef;
            _effectContext = inEffectContext;
            _level = level;
        }

        public bool IsValid()
        {
            return Def != null;
        }

        /// <summary>
        ///  Sets duration. This should only be called as the GameplayEffect is being created and applied; Ignores calls after attribute capture */
        /// </summary>
        /// <param name="newDuration"></param>
        /// <param name="bLockDuration"></param>
        public void SetDuration(float newDuration, bool bLockDuration)
        {
            if (!bDurationLocked)
            {
                Duration = newDuration;
                bDurationLocked =  bLockDuration;
                if (Duration > 0)
                {
                    // We may have potential problems one day if a game is applying duration based gameplay effects from instantaneous effects
                    // (E.g., every time fire damage is applied, a DOT is also applied). We may need to for Duration to always be captured.
                    throw new NotImplementedException();
                }
            }
        }

        public float GetDuration()
        {
            return Duration;
        }
        
        public void SetLevel(float inLevel)
        {
            
        }

        public float GetLevel()
        {
            return _level;
        }

        /// <summary>
        /// Set the context info: who and where this spec came from.
        /// </summary>
        /// <param name="newEffectContext"></param>
        /// <param name="bSkipRecaptureSourceActorTags"></param>
        public void SetContext(ref GameplayEffectContextHandle newEffectContext, bool bSkipRecaptureSourceActorTags = false)
        {
            bool bWasAlreadyInit = _effectContext.IsValid();
            _effectContext = newEffectContext;
            if (bWasAlreadyInit)
            {
                CaptureDataFromSource(bSkipRecaptureSourceActorTags);
            }
        }
        
        public 	GameplayEffectContextHandle GetContext() 
        {
            return _effectContext;
        }
        
        /// <summary>
        /// Sets the stack count for this GE to NewStackCount if stacking is supported
        /// </summary>
        /// <param name="newStackCount"></param>
        public void SetStackCount(int newStackCount)
        {
            StackCount = newStackCount;
        }

        /// <summary>
        /// Returns the stack count for this GE spec.
        /// </summary>
        /// <returns></returns>
        public int GetStackCount()
        {
            return StackCount;
        }
        
        /// <summary>
        /// Appends all tags granted by this gameplay effect spec 
        /// </summary>
        /// <param name="outContainer"></param>
        public void GetAllGrantedTags(GameplayTagContainer outContainer)
        {
            outContainer.AppendTags(DynamicGrantedTags);
            if (Def != null)
            {
                outContainer.AppendTags(Def.GetGrantedTags());
            }
        }
        
        /// <summary>
        /// Appends all blocked ability tags granted by this gameplay effect spec 
        /// </summary>
        /// <param name="outContainer"></param>
        public void GetAllBlockedAbilityTags(GameplayTagContainer outContainer)
        {
            if (Def != null)
            {
                outContainer.AppendTags(Def.GetBlockedAbilityTags());
            }
        }
        
        /// <summary>
        /// Appends all tags that apply to this gameplay effect spec
        /// </summary>
        /// <param name="outContainer"></param>
        public void GetAllAssetTags(GameplayTagContainer outContainer)
        {
            outContainer.AppendTags(GetDynamicAssetTags());
            if (Def != null)
            {
                outContainer.AppendTags(Def.GetAssetTags());
            }
        }

        /// <summary>
        ///  Sets the magnitude of a SetByCaller modifier
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="magnitude"></param>
        public void SetSetByCallerMagnitude(string dataName, float magnitude)
        {
            
        }
        
        /// <summary>
        ///  Sets the magnitude of a SetByCaller modifier
        /// </summary>
        /// <param name="dataTag"></param>
        /// <param name="magnitude"></param>
        public void SetSetByCallerMagnitude(GameplayTag dataTag, float magnitude)
        {
            
        }

        /// <summary>
        /// Returns the magnitude of a SetByCaller modifier. Will return 0.f and Warn if the magnitude has not been set.
        /// </summary>
        /// <param name="dataName"></param>
        /// <param name="warnIfNotFound"></param>
        /// <param name="defaultIfNotFound"></param>
        /// <returns></returns>
        public float GetSetByCallerMagnitude(string dataName, bool warnIfNotFound = true, float defaultIfNotFound = 0f)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Returns the magnitude of a SetByCaller modifier. Will return 0.f and Warn if the magnitude has not been set.
        /// </summary>
        /// <param name="dataTag"></param>
        /// <param name="warnIfNotFound"></param>
        /// <param name="defaultIfNotFound"></param>
        /// <returns></returns>
        public float GetSetByCallerMagnitude(GameplayTag dataTag, bool warnIfNotFound = true, float defaultIfNotFound = 0f)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies SetbuCallerMagnitudes, but only if magnitudes don't exist in our map (slower but preserves data)
        /// </summary>
        /// <param name="magnitudes"></param>
        public void MergeSetByCallerMagnitudes(Dictionary<GameplayTag, float> magnitudes)
        {
            foreach (var pair in magnitudes)
            {
                if (!SetByCallerTagMagnitudes.ContainsKey(pair.Key))
                {
                    SetByCallerTagMagnitudes.Add(pair.Key, pair.Value);
                }
            }
        }

        public GameplayTagContainer GetDynamicAssetTags()
        {
            return DynamicAssetTags;
        }

        public void CaptureAttributeDataFromTarget(AbilitySystemComponent abilitySystem)
        {
            throw new NotImplementedException();
        }
        
        private void CaptureDataFromSource(bool bSkipRecaptureSourceActorTags = false)
        {
            throw new NotImplementedException();
        }
    }
    
    public struct GameplayEffectSpecHandle
    {
        public GameplayEffectSpec Data;

        public void Clear()
        {
            Data = default;
        }
        
        public bool IsValid()
        {
            return Data.IsValid();
        }
    }
    
    public class GameplayEffectContext
    {
        public float3 WorldOrigin  { get; protected set; }
        
        /// <summary>
        /// Instigator actor, the actor that owns the ability system component
        /// </summary>
        protected EntityRef<Entity> Instigator;
        
        /// <summary>
        /// The physical actor that actually did the damage, can be a weapon or projectile
        /// </summary>
        protected EntityRef<Entity> EffectCauser;
        
        /// <summary>
        ///  The ability CDO that is responsible for this effect context (replicated)
        /// </summary>
        protected WeakReference<GameplayAbility> AbilityCDO;
        
        /// <summary>
        /// The ability instance that is responsible for this effect context (NOT replicated)
        /// </summary>
        protected WeakReference<GameplayAbility> AbilityInstanceNotReplicated;
        
        /// <summary>
        /// The level this was executed at
        /// </summary>
        protected int AbilityLevel;

        /// <summary>
        /// Object this effect was created from, can be an actor or static object. Useful to bind an effect to a gameplay object
        /// </summary>
        protected WeakReference SourceObject;
        
        /// <summary>
        /// The ability system component that's bound to instigator
        /// </summary>
        protected WeakReference<AbilitySystemComponent> InstigatorAbilitySystemComponent;

        /// <summary>
        /// Actors referenced by this context
        /// </summary>
        protected List<EntityRef<Entity>> Actors;

        protected bool bHasWorldOrigin;

        /// <summary>
        /// Sets the instigator and effect causer. Instigator is who owns the ability that spawned this, EffectCauser is the actor that is the physical source of the effect, such as a weapon. They can be the same.
        /// </summary>
        /// <param name="inInstigator"></param>
        /// <param name="inEffectCauser"></param>
        public virtual void AddInstigator(Entity inInstigator, Entity inEffectCauser)
        {
            Instigator =  inInstigator;
            
            SetEffectCauser(inEffectCauser);
        }

        /// <summary>
        /// Returns the immediate instigator that applied this effect
        /// </summary>
        /// <returns></returns>
        public virtual Entity GetInstigator()
        {
            return (Entity)Instigator;
        }

        /// <summary>
        /// Modify the effect causer actor, useful when that information is added after creation
        /// </summary>
        /// <param name="inEffectCauser"></param>
        public void SetEffectCauser(Entity inEffectCauser)
        {
            EffectCauser = inEffectCauser;
        }
        
        /// <summary>
        /// Sets the ability that was used to spawn this
        /// </summary>
        /// <param name="ability"></param>
        public virtual void SetAbility(GameplayAbility ability)
        {
            if(ability == null) return;
            
            AbilityInstanceNotReplicated =  new WeakReference<GameplayAbility>(ability);
            AbilityLevel = ability.GetAbilityLevel();
        }


        /// <summary>
        /// Sets the object this effect was created from.
        /// </summary>
        /// <param name="newSourceObject"></param>
        public virtual void AddSourceObject(object newSourceObject)
        {
            SourceObject =  new WeakReference(newSourceObject);
        }

        /// <summary>
        /// Returns the object this effect was created from.
        /// </summary>
        /// <returns></returns>
        public virtual object GetSourceObject()
        {
            return SourceObject.Target;
        }

        /// <summary>
        ///  Returns the ability system component of the instigator of this effect
        /// </summary>
        /// <returns></returns>
        public virtual AbilitySystemComponent GetInstigatorAbilitySystemComponent()
        {
            InstigatorAbilitySystemComponent.TryGetTarget(out var abilitySystemComponent);
            return abilitySystemComponent;
        }

        /// <summary>
        /// Returns the ability system component of the instigator that started the whole chain 
        /// </summary>
        /// <returns></returns>
        public virtual AbilitySystemComponent GetOriginalInstigatorAbilitySystemComponent()
        {
            InstigatorAbilitySystemComponent.TryGetTarget(out var abilitySystemComponent);
            return abilitySystemComponent;
        }

        /// <summary>
        /// Add actors to the stored actor list
        /// </summary>
        /// <param name="inActors"></param>
        /// <param name="bReset"></param>
        public virtual void AddActors(List<EntityRef<Entity>> inActors, bool bReset = false)
        {
            if (bReset && Actors.Count > 0)
            {
                Actors.Clear();
            }
            
            Actors.AddRange(inActors);
        }

        public virtual void AddOrigin(float3 origin)
        {
            bHasWorldOrigin = true;
            WorldOrigin = origin;
        }
    }

    public struct GameplayEffectContextHandle
    {
        private GameplayEffectContext _data;

        public GameplayEffectContextHandle(GameplayEffectContext data)
        {
            _data = data;
        }

        public void Clear()
        {
            _data = null;
        }

        public bool IsValid()
        {
            return _data != null;
        }

        public void AddInstigator(Entity inInstigator, Entity inEffectCauser)
        {
            if (IsValid())
            {
                _data.AddInstigator(inInstigator, inEffectCauser);
            }
        }

        public void SetAbility(GameplayAbility ability)
        {
            if(IsValid())
            {
                _data.SetAbility(ability);
            }
        }

        public void AddSourceObject(object newSourceObject)
        {
            if (IsValid())
            {
                _data.AddSourceObject(newSourceObject);
            }
        }
        
        public object GetSourceObject()
        {
            return _data?.GetSourceObject();
        }

        /// <summary>
        /// Returns the ability system component of the instigator of this effect
        /// </summary>
        /// <returns></returns>
        public AbilitySystemComponent GetInstigatorAbilitySystemComponent()
        {
            if (IsValid())
            {
                return _data.GetInstigatorAbilitySystemComponent();
            }
            return null;
        }

        /// <summary>
        /// Returns the ability system component of the instigator that started the whole chain
        /// </summary>
        /// <returns></returns>
        public AbilitySystemComponent GetOriginalInstigatorAbilitySystemComponent()
        {
            if (IsValid())
            {
                return _data.GetOriginalInstigatorAbilitySystemComponent();
            }
            return null;
        }

        public void AddActors(List<EntityRef<Entity>> inActors, bool bReset = false)
        {
            if (IsValid())
            {
                _data.AddActors(inActors, bReset);
            }
        }

        public void AddOrigin(float3 origin)
        {
            if (IsValid())
            {
                _data.AddOrigin(origin);
            }
        }
    }

    public struct GameplayEffectRemovalInfo
    {
        
    }

    /// <summary>
    /// Metadata about a gameplay cue execution
    /// </summary>
    public struct GameplayCueParameters
    {
        /// <summary>
        /// If originating from an ability, this will be the level of that ability
        /// </summary>
        public int AbilityLevel;
    }
}