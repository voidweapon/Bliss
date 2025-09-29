using System;

namespace Bliss
{
    public static class RandomExtensions
    {
        public static int RandInt32(this Random random)
        {
            return random.Next();
        }
        
        public static uint RandUInt32(this Random random)
        {
            return (uint) random.Next();
        }
        
        public static ulong RandUInt64(this Random random)
        {
            int r1 = random.RandInt32();
            int r2 = random.RandInt32();
            return ((ulong)r1 << 32) | (uint)r2;
        }
        
        public static long RandInt64(this Random random)
        {
            uint r1 = random.RandUInt32();
            uint r2 = random.RandUInt32();
            return (long)(((ulong)r1 << 32) | r2);
        }
        
        public static bool RandomBool(this Random random)
        {
            return random.Next(2) == 0;
        }
        
        /// <summary>
        /// 获取lower与Upper之间的随机数, [lower,Upper)
        /// </summary>
        /// <param name="random"></param>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <returns></returns>
        public static int RandomNumber(this Random random, int lower, int upper)
        {
            int value = random.Next(lower, upper);
            return value;
        }
        
        public static float RandomNumber(this Random random, float lower, float upper)
        {
            float value = lower + (float)random.NextDouble() * (upper - lower);
            return value;
        }
    }
}