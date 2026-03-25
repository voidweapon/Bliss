using System;
using Bliss.Base.Event;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /// <summary>
    /// Singleton manager object that handles dispatching gameplay cues and spawning GameplayCueNotify actors as needed
    /// </summary>
    public class GameplayCueManager
    {
        /// <summary>
        /// Number of active gameplay cue send contexts, when it goes to 0 cues are flushed
        /// </summary>
        public int GameplayCueSendContextCount;

        /// <summary>
        ///  Broadcasted when ::FlushPendingCues runs: useful for custom batching/gameplay cue handling 
        /// </summary>
        public AutoEventAction OnFlushPendingCues;

        // -------------------------------------------------------------
        // Wrappers to handle replicating executed cues
        // -------------------------------------------------------------
        public virtual void InvokeGameplayCueExecuted_FromSpec(AbilitySystemComponent owningComponent, ref GameplayEffectSpec  spec, PredictionKey predictionKey)
        {
            throw new NotImplementedException();
        }
        
        public virtual void InvokeGameplayCueAddedAndWhileActive_FromSpec(AbilitySystemComponent owningComponent, ref GameplayEffectSpec  spec, PredictionKey predictionKey)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Start a gameplay cue send context. Used by FScopedGameplayCueSendContext above, when all contexts are removed the cues are flushed
        /// </summary>
        public void StartGameplayCueSendContext()
        {
            GameplayCueSendContextCount++;
        }

        /// <summary>
        /// stop a gameplay cue send context. Used by FScopedGameplayCueSendContext above, when all contexts are removed the cues are flushed
        /// </summary>
        public void EndGameplayCueSendContext()
        {
            GameplayCueSendContextCount--;
            
            if (GameplayCueSendContextCount == 0)
            {
                FlushPendingCues();
            }
            else if (GameplayCueSendContextCount < 0)
            {
                AbilitySystemComponent.Ability_Warning("GameplayCueManager::EndGameplayCueSendContext called too many times! Negative context count");
            }
        }

        /// <summary>
        /// Send out any pending cues
        /// </summary>
        public virtual void FlushPendingCues()
        {
            
        }
    }
}