using System;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public struct ActiveGameplayEffectHandle : IEquatable<ActiveGameplayEffectHandle>
    {
        private int _handle;
        private bool _passedFiltersAndWasExecuted;
        private static int _idGenerator = 1;
        
        public ActiveGameplayEffectHandle(int inHandle)
        {
            _handle = inHandle;
            _passedFiltersAndWasExecuted = false;
        }

        public void Invalidate()
        {
            _handle = -1;
        }
        
        public bool IsValid()
        {
            return _handle > 0;
        }
        
        public 	bool WasSuccessfullyApplied() 
        {
            return _passedFiltersAndWasExecuted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="abilitySystem"></param>
        /// <returns></returns>
        public static ActiveGameplayEffectHandle GenerateNewHandle(AbilitySystemComponent abilitySystem)
        {
            ActiveGameplayEffectHandle  handle = new ActiveGameplayEffectHandle(_idGenerator++);
            AddTomGlobalMap(handle, abilitySystem);
            
            if (_idGenerator <= 0)
            {
                _idGenerator = 1;
            }
            
            return handle;
        }

        private static void AddTomGlobalMap(ActiveGameplayEffectHandle handle, AbilitySystemComponent abilitySystem)
        {
            throw new NotImplementedException();
        }
        public static void ResetGlobalHandleMap()
        {
            throw new NotImplementedException();
        }

        public void RemoveFromGlobalMap()
        {
            throw new NotImplementedException();
        }

        public AbilitySystemComponent GetOwningAbilitySystemComponent()
        {
            throw new NotImplementedException();
        }
        
        public bool Equals(ActiveGameplayEffectHandle other)
        {
            return _handle == other._handle;
        }

        public override bool Equals(object obj)
        {
            return obj is ActiveGameplayEffectHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _handle;
        }

        public static bool operator ==(ActiveGameplayEffectHandle left, ActiveGameplayEffectHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActiveGameplayEffectHandle left, ActiveGameplayEffectHandle right)
        {
            return !(left == right);
        }
    }
}