using System;

namespace Bliss
{
    public static class RandomGenerator
    {
        [ThreadStatic]
        private static Random random;

        private static Random GetRandom()
        {
            return random ??= new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount);
        }
        
        public static uint RandUInt32()
        {
            return GetRandom().RandUInt32();
        }
        
        public static int RandInt32()
        {
            return GetRandom().RandInt32();
        }
    }
}