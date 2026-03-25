using System;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    /// <summary>
    ///  Add this around code that sends multiple gameplay cues to allow grouping them into a smalkler number of cues for more efficient networking
    /// </summary>
    public struct ScopedGameplayCueSendContext : IDisposable
    {
        public static ScopedGameplayCueSendContext StartScope()
        {
            var scope = new ScopedGameplayCueSendContext();
            AbilitySystemGlobals.Instance.GetGameplayCueManager().StartGameplayCueSendContext();
            return scope;
        }
        
        public void Dispose()
        {
            AbilitySystemGlobals.Instance.GetGameplayCueManager().EndGameplayCueSendContext();
        }
    }
}