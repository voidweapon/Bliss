using System;

namespace Bliss
{
    public static class HashCodeHelper
    {
        #region BKDR HASH

        // bkdr hash
        public static long GetLongHashCode(this string str)
        {
            const uint seed = 1313; // 31 131 1313 13131 131313 etc..
            
            ulong hash = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                byte high = (byte)(c >> 8);
                byte low = (byte)(c & byte.MaxValue);
                hash = hash * seed + high;
                hash = hash * seed + low;
            }
            return (long)hash;
        }

        #endregion

        public static int Mode(this string strText, int mode)
        {
            if (mode <= 0)
            {
                throw new Exception($"string mode < 0: {strText} {mode}");
            }
            return (int)((ulong)strText.GetLongHashCode() % (uint)mode);
        }


        #region FNV HASH

        // string.GetHashCode is not guaranteed to be the same on all
        // machines, but we need one that is the same on all machines.
        // Uses fnv1a as hash function for more uniform distribution http://www.isthe.com/chongo/tech/comp/fnv/
        // Tests: https://softwareengineering.stackexchange.com/questions/49550/which-hashing-algorithm-is-best-for-uniqueness-and-speed
        // NOTE: Do not call this from hot path because it's slow O(N) for long method names.
        // - As of 2012-02-16 There are 2 design-time callers (weaver) and 1 runtime caller that caches.
        public static int GetStableHashCode(this string text)
        {
            unchecked
            {
                uint hash = 0x811c9dc5;
                uint prime = 0x1000193;

                for (int i = 0; i < text.Length; ++i)
                {
                    byte value = (byte)text[i];
                    hash = hash ^ value;
                    hash *= prime;
                }

                // Uncomment this code to log message hashes and methods for debugging.
                // There will be one log message per message hash without flooding duplicates.
                // User should look in the log of the SENDING side for the offending hash.
                // try-catch to avoid nonsense weaver errors at design time.
                //try
                //{
                //    // cast the hash to ushort to match what user sees in unknown message id warnings
                //    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Created stable hash {0} for {1}", (ushort)hash, text);
                //}
                //catch { }

                return (int)hash;
            }
        }

        // smaller version of our GetStableHashCode.
        // careful, this significantly increases chance of collisions.
        public static ushort GetStableHashCode16(this string text)
        {
            // deterministic hash
            int hash = GetStableHashCode(text);

            // Gets the 32bit fnv1a hash
            // To get it down to 16bit but still reduce hash collisions we cant just cast it to ushort
            // Instead we take the highest 16bits of the 32bit hash and fold them with xor into the lower 16bits
            // This will create a more uniform 16bit hash, the method is described in:
            // http://www.isthe.com/chongo/tech/comp/fnv/ in section "Changing the FNV hash size - xor-folding"
            return (ushort)((hash >> 16) ^ hash);
        }

        #endregion
    }
}


