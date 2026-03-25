using System;

namespace Bliss.Gameplay.GameplayAbilitySystem
{
    public struct GameplayAbilitySpecHandle : IEquatable<GameplayAbilitySpecHandle>
    {
        private int _handle;

        private static int _idGenerator = 1;
        
        public bool IsValid()
        {
            return _handle > 0;
        }

        public void GenerateNewHandle()
        {
            _handle = _idGenerator++;
            if (_idGenerator < 0)
            {
                _idGenerator = 1;
            }
        }

        public bool Equals(GameplayAbilitySpecHandle other)
        {
            return _handle == other._handle;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAbilitySpecHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _handle;
        }

        public static bool operator ==(GameplayAbilitySpecHandle lhs, GameplayAbilitySpecHandle rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(GameplayAbilitySpecHandle lhs, GameplayAbilitySpecHandle rhs)
        {
            return !(lhs == rhs);
        }
    }
}