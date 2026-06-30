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

        public static long RandInt64()
        {
            uint r1 = RandUInt32();
            uint r2 = RandUInt32();
            return (long)(((ulong)r1 << 32) | r2);
        }
        
        public static int RandomNumber(int lower, int upper)
        {
            return GetRandom().RandomNumber(lower, upper);
        }
        
        public static float RandFloat()
        {
            return (float)GetRandom().NextDouble();
        }
    }
}