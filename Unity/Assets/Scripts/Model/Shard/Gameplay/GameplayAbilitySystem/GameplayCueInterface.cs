using System;
using System.Collections.Generic;
using Bliss.Gameplay.GameplayTags;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public struct ActiveGameplayCue
    {
        GameplayTag GameplayCueTag;

        PredictionKey PredictionKey;
        
        public bool bPredictivelyRemoved;

        void PreReplicatedRemove(ActiveGameplayCueContainer InArray)
        {
            throw new NotImplementedException();
        }

        void PostReplicatedAdd(ActiveGameplayCueContainer InArray)
        {
            throw new NotImplementedException();
        }

        void PostReplicatedChange(ActiveGameplayCueContainer InArray)
        {
            throw new NotImplementedException();
        }
    }
    public class ActiveGameplayCueContainer
    {
        private List<ActiveGameplayCue> GameplayCues = new();
        /// <summary>
        /// Should this container only replicate in minimal replication mode
        /// </summary>
        private bool bMinimalReplication;
        private AbilitySystemComponent Owner;


        void SetOwner(AbilitySystemComponent InOwner)
        {
            Owner = InOwner;
        }


        void AddCue(GameplayTag Tag, PredictionKey PredictionKey, GameplayCueParameters Parameters)
        {
            throw new NotImplementedException();
        }

        void RemoveCue(GameplayTag Tag)
        {
            throw new NotImplementedException();
        }

        /** Marks as predictively removed so that we dont invoke remove event twice due to onrep */
        void PredictiveRemove(GameplayTag Tag)
        {
            throw new NotImplementedException();
        }

        void PredictiveAdd(GameplayTag Tag, PredictionKey PredictionKey)
        {
            throw new NotImplementedException();
        }

        /** Does explicit check for gameplay cue tag */
        bool HasCue(GameplayTag Tag)
        {
            throw new NotImplementedException();
        }

        /** Returns true if the instance should be replicated. If false the property is allowed to be disabled for replication. */
        bool ShouldReplicate()
        {
            throw new NotImplementedException();
        }


        // Will broadcast the OnRemove event for all currently active cues
        void RemoveAllCues()
        {
            throw new NotImplementedException();
        }
    }
}