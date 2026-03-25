using System;
using System.Collections.Generic;
using Bliss.Mathematics;
using Unity.Mathematics;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /**
     *	A generic structure for targeting data. We want generic functions to produce this data and other generic
     *	functions to consume this data.
     *
     *	We expect this to be able to hold specific actors/object reference and also generic location/direction/origin
     *	information.
     *
     *	Some example producers:
     *		-Overlap/Hit collision event generates TargetData about who was hit in a melee attack
     *		-A mouse input causes a hit trace and the actor infront of the crosshair is turned into TargetData
     *		-A mouse input causes TargetData to be generated from the owner's crosshair view origin/direction
     *		-An AOE/aura pulses and all actors in a radius around the instigator are added to TargetData
     *		-Panzer Dragoon style 'painting' targeting mode
     *		-MMORPG style ground AOE targeting style (potentially both a location on the ground and actors that were targeted)
     *
     *	Some example consumers:
     *		-Apply a GameplayEffect to all actors in TargetData
     *		-Find closest actor from all in TargetData
     *		-Call some function on all actors in TargetData
     * 		-Filter or merge TargetDatas
     *		-Spawn a new actor at a TargetData location
     *
     *	Maybe it is better to distinguish between actor list targeting vs positional targeting data?
     *		-AOE/aura type of targeting data blurs the line
     */
    public abstract class GameplayAbilityTargetData
    {
        /// <summary>
        ///  Applies a gameplay effect to each target represented 
        /// </summary>
        /// <param name="gameplayEffect"></param>
        /// <param name="inEffectContext"></param>
        /// <param name="level"></param>
        /// <param name="predictionKey"></param>
        /// <returns></returns>
        public List<ActiveGameplayEffectHandle> ApplyGameplayEffect(GameplayEffect gameplayEffect, 
            ref GameplayEffectContextHandle inEffectContext,
            float level,
            PredictionKey predictionKey = new PredictionKey())
        {
            // Make a temp spec and call the spec function. This ends up cloning the spec per target
            GameplayEffectSpec tempSpecToApply = new GameplayEffectSpec(gameplayEffect, ref inEffectContext, level);
            return ApplyGameplayEffectSpec(ref tempSpecToApply, predictionKey);
        }

        /// <summary>
        /// Applies a previously created gameplay effect spec to each target represented
        /// </summary>
        /// <param name="inEffectSpec"></param>
        /// <param name="predictionKey"></param>
        /// <returns></returns>
        public virtual List<ActiveGameplayEffectHandle> ApplyGameplayEffectSpec(ref GameplayEffectSpec  inEffectSpec, 
            PredictionKey predictionKey = new PredictionKey())
        {
            List<ActiveGameplayEffectHandle> AppliedHandles = new List<ActiveGameplayEffectHandle>();
            var inEffectContext = inEffectSpec.GetContext();
            if (!(inEffectContext.IsValid() && inEffectContext.GetInstigatorAbilitySystemComponent() != null))
            {
                return AppliedHandles;
            }

            List<EntityRef<Entity>> actors = GetActors();

            for (int i = 0; i < actors.Count; i++)
            {
                var targetActor = (Entity)actors[i];
                if (targetActor != null)
                {
                    AbilitySystemComponent targetComponent = targetActor.GetComponent<AbilitySystemComponent>();
                    if (targetComponent != null)
                    {
                        // We have to make a new effect spec and context here, because otherwise the targeting info gets accumulated and things take damage multiple times
                        GameplayEffectSpec specToApply = new GameplayEffectSpec(ref inEffectSpec);
                        GameplayEffectContextHandle effectContext = specToApply.GetContext();
                        specToApply.SetContext(ref effectContext);
                        
                        AddTargetDataToContext(ref effectContext, false);
                        AppliedHandles.Add(effectContext.GetInstigatorAbilitySystemComponent().ApplyGameplayEffectSpecToTarget(specToApply, targetComponent, predictionKey));
                    }
                }
            }
            
            return AppliedHandles;
        }

        /// <summary>
        /// Modifies the context and adds this target data to the target data handle stored within 
        /// </summary>
        /// <param name="inEffectContext"></param>
        /// <param name="bIncludeActorArray"></param>
        public virtual void AddTargetDataToContext(ref GameplayEffectContextHandle inEffectContext,
            bool bIncludeActorArray)
        {
            if (bIncludeActorArray)
            {
                var weakActors = GetActors();
                if (weakActors.Count > 0)
                {
                    inEffectContext.AddActors(weakActors);
                }
            }

            if (HasOrigin())
            {
                inEffectContext.AddOrigin(GetOrigin().Location);
            }
        }

        /// <summary>
        /// Returns all actors targeted, almost always overridden 
        /// </summary>
        /// <returns></returns>
        public virtual List<EntityRef<Entity>> GetActors()
        {
            return null;
        }
        
        /// <summary>
        /// Modify the actor list
        /// </summary>
        /// <returns></returns>
        public virtual bool SetActors(List<EntityRef<Entity>> newActors)
        {
            //By default, we don't keep this data, and therefore can't set it.
           return false;
        }

        /// <summary>
        /// Override to true if GetOrigin will work
        /// </summary>
        /// <returns></returns>
        public virtual bool HasOrigin()
        {
            return false;
        }

        /// <summary>
        /// Override to return an origin point, which may be derived from other data
        /// </summary>
        /// <returns></returns>
        public virtual transform GetOrigin()
        {
            return new transform(float3.zero);
        }
        
        /// <summary>
        /// Override to true if GetEndPoint/Transform will work
        /// </summary>
        /// <returns></returns>
        public virtual bool HasEndPoint()
        {
            return false;
        }

        /// <summary>
        /// Override to return a target/end point
        /// </summary>
        /// <returns></returns>
        public virtual float3 GetEndPoint()
        {
            return float3.zero;
        }
        
        /// <summary>
        /// Override to return a transform, default will create one from just the location
        /// </summary>
        /// <returns></returns>
        public virtual transform GetEndPointTransform()
        {
            return new transform(float3.zero);
        }
        
        /// <summary>
        /// See notes on delegate definition FOnTargetActorSwapped
        /// </summary>
        /// <returns></returns>
        public virtual bool ShouldCheckForTargetActorSwap() 
        {
            return false;
        }
    }

    /// <summary>
    /// Describes how the targeting information is confirmed
    /// </summary>
    public enum EGameplayTargetingConfirmation
    {
        /// <summary>
        /// The targeting happens instantly without special logic or user input deciding when to 'fire'
        /// </summary>
        Instant,
        
        /// <summary>
        /// The targeting happens when the user confirms the targeting
        /// </summary>
        UserConfirmed,
        
        /// <summary>
        /// The GameplayTargeting Ability is responsible for deciding when the targeting data is ready. Not supported by all TargetingActors
        /// </summary>
        Custom,
        
        /// <summary>
        /// The GameplayTargeting Ability is responsible for deciding when the targeting data is ready. Not supported by all TargetingActors. Should not destroy upon data production
        /// </summary>
        CustomMulti,
    }

    /// <summary>
    /// What type of location calculation to use when an ability asks for our transform
    /// </summary>
    public enum EGameplayAbilityTargetingLocationType
    {
        /// <summary>
        /// We report an actual raw transform. This is also the final fallback if other methods fail 
        /// </summary>
        LiteralTransform,
        
        /// <summary>
        /// We pull the transform from an associated actor directly
        /// </summary>
        ActorTransform,
        
        /// <summary>
        /// We aim from a named socket on the player's skeletal mesh component
        /// </summary>
        SocketTransform,
    }

    /**
    *	Handle for Targeting Data. This servers two main purposes:
    *		-Avoid us having to copy around the full targeting data structure in Blueprints
    *		-Allows us to leverage polymorphism in the target data structure
    *		-Allows us to implement NetSerialize and replicate by value between clients/server
    *
    *		-Avoid using UObjects could be used to give us polymorphism and by reference passing in blueprints.
    *		-However we would still be screwed when it came to replication
    *
    *		-Replication by value
    *		-Pass by reference in blueprints
    *		-Polymophism in TargetData structure
    */
    public struct GameplayAbilityTargetDataHandle
    {
        public GameplayAbilityTargetDataHandle(GameplayAbilityTargetData targetData)
        {
            Data = new List<GameplayAbilityTargetData>();
            Data.Add(targetData);

            UniqueId = 0;
        }
        
        /// <summary>
        ///  Raw storage of target data, do not modify this directly
        /// </summary>
        public List<GameplayAbilityTargetData> Data;
        
        public byte UniqueId;
        
        /// <summary>
        /// Resets handle to have no targets 
        /// </summary>
        public void Clear()
        {
            Data.Clear();
        }

        /// <summary>
        /// Returns number of target data, not number of actors/targets as target data may contain multiple actors
        /// </summary>
        /// <returns></returns>
        public int Num()
        {
            return Data.Count;
        }

        /// <summary>
        /// Returns true if there are any valid targets
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsValid(int index)
        {
            return index >= 0 && index < Data.Count && Data[index] != null;
        }

        /// <summary>
        /// Returns data at index, or nullptr if invalid 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GameplayAbilityTargetData Get(int index)
        {
            return IsValid(index) ? Data[index] : null;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetData"></param>
        public void Add(GameplayAbilityTargetData targetData)
        {
            Data.Add(targetData);
        }

        public void Append(ref GameplayAbilityTargetDataHandle otherHandle)
        {
            Data.AddRange(otherHandle.Data);
        }

        public static bool operator ==(GameplayAbilityTargetDataHandle a, GameplayAbilityTargetDataHandle b)
        {
            if (a.Data.Count != b.Data.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Data.Count; i++)
            {
                if (a.Data[i] != b.Data[i])
                {
                    return false;
                }
            }
            
            return true;
        }

        public static bool operator !=(GameplayAbilityTargetDataHandle a, GameplayAbilityTargetDataHandle b)
        {
            return !(a == b);
        }
    }

    /// <summary>
    /// Structure that stores a location in one of several different formats
    /// </summary>
    public struct GameplayAbilityTargetingLocationInfo
    {
        /// <summary>
        ///  Type of location used - will determine what data is transmitted over the network and what fields are used when calculating position.
        /// </summary>
        public EGameplayAbilityTargetingLocationType LocationType;

        /// <summary>
        /// A literal world transform can be used, if one has been calculated outside of the actor using the ability.
        /// </summary>
        public transform LiteralTransform;

        /// <summary>
        /// A source actor is needed for Actor-based targeting, but not for Socket-based targeting.
        /// </summary>
        public EntityRef<Entity> SourceActor;
        
        /// <summary>
        /// Ability that will be using the targeting data
        /// </summary>
        public GameplayAbility SourceAbility;

        /// <summary>
        /// If SourceComponent is valid, this is the name of the socket transform that will be used. If no Socket is provided, SourceComponent's transform will be used.
        /// </summary>
        public string SourceSocketName;

        /// <summary>
        /// Converts internal format into a literal world space transform
        /// </summary>
        /// <returns></returns>
        public transform GetTargetingTransform()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes new target data and fills in with hit results
        /// </summary>
        /// <param name="ability"></param>
        /// <returns></returns>
        public GameplayAbilityTargetDataHandle MakeTargetDataHandleFromHitResult(WeakReference<GameplayAbility> ability)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Initializes new actor list target data, and sets this as the origin
        /// </summary>
        /// <param name="targetActors"></param>
        /// <param name="oneActorPerHandler"></param>
        /// <returns></returns>
        public GameplayAbilityTargetDataHandle MakeTargetDataHandleFromActors(List<EntityRef<Entity>> targetActors,
            bool oneActorPerHandler = false)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Target data with just a source and target location in space
    /// </summary>
    public class GameplayAbilityTargetData_LocationInfo : GameplayAbilityTargetData
    {
        /// <summary>
        /// Generic location data for source
        /// </summary>
        public GameplayAbilityTargetingLocationInfo SourceLocation;
        
        /// <summary>
        /// Generic location data for target
        /// </summary>
        public GameplayAbilityTargetingLocationInfo TargetLocation;

        public override bool HasOrigin()
        {
            return true;
        }

        public override transform GetOrigin()
        {
            return SourceLocation.GetTargetingTransform();
        }

        public override bool HasEndPoint()
        {
            return true;
        }

        public override transform GetEndPointTransform()
        {
            return TargetLocation.GetTargetingTransform();
        }

        public override float3 GetEndPoint()
        {
            return TargetLocation.GetTargetingTransform().Location;
        }
    }
    
    /// <summary>
    /// Target data with a source location and a list of targeted actors, makes sense for AOE attacks
    /// </summary>
    public class GameplayAbilityTargetData_ActorArray : GameplayAbilityTargetData
    {
        /// <summary>
        /// We could be selecting this group of actors from any type of location, so use a generic location type
        /// </summary>
        public GameplayAbilityTargetingLocationInfo SourceLocation;
        
        /// <summary>
        /// Rather than targeting a single point, this type of targeting selects multiple actors.
        /// </summary>
        public List<EntityRef<Entity>> TargetActors;

        public override bool SetActors(List<EntityRef<Entity>> newActors)
        {
            TargetActors =  newActors;
            return true;
        }
        
        public override List<EntityRef<Entity>> GetActors()
        {
            return TargetActors;
        }

        public override bool HasOrigin()
        {
            return true;
        }

        public override transform GetOrigin()
        {
            transform returnTransform = SourceLocation.GetTargetingTransform();
            
            for (int i = 0; i < TargetActors.Count; i++)
            {
                Entity targetActor = (Entity)TargetActors[i];
                if (targetActor != null)
                {
                    float3 direction = (targetActor.GetTransform().Location - SourceLocation.GetTargetingTransform().Location);
                    direction = math.normalizesafe(direction);
                    if (!direction.Equals(float3.zero))
                    {
                        returnTransform.SetRotation(quaternion.LookRotation(direction, math.right()));
                    }
                }
            }
            
            return returnTransform;
        }

        public override bool HasEndPoint()
        {
            //We have an endpoint if we have at least one valid actor in our target array
            for (int i = 0; i < TargetActors.Count; i++)
            {
                Entity targetActor = (Entity)TargetActors[i];
                if (targetActor != null)
                {
                    return true;
                }
            }
            return false;
        }

        public override float3 GetEndPoint()
        {
            for (int i = 0; i < TargetActors.Count; i++)
            {
                Entity targetActor = (Entity)TargetActors[i];
                if (targetActor != null)
                {
                    return targetActor.GetTransform().Location;
                }
            }
            return float3.zero;
        }
    }
    
    /// <summary>
    /// Target data with a single hit result, data is packed into the hit result
    /// </summary>
    public class GameplayAbilityTargetData_SingleTargetHit : GameplayAbilityTargetData
    {
        
    }
}