using Bliss.Core;

namespace Bliss.CoroutineLocks
{

    [EntitySystemOf(typeof(CoroutineLock))]
    public static partial class CoroutineLockSystems
    {
        public static void Awake(this CoroutineLock self, CoroutineLockInitParam param1)
        {
            self.LockType = param1.LockType;
            self.Identifier = param1.Identifier;
            self.LockDepth = param1.LockDepth;
        }
        
        public static void Destroy(this CoroutineLock self)
        {
            self.RootCoroutineLockComponent().RunNextCoroutine(self.LockType, self.Identifier, self.LockDepth + 1);
            self.LockDepth = ECoroutineLockType.None;
            self.Identifier = 0;
            self.LockDepth = 0;
        }
    }

    public class CoroutineLock : Entity, IAwake<CoroutineLockInitParam>, IDestroy
    {
        /// <summary>
        /// 锁类型
        /// </summary>
        public int LockType;

        /// <summary>
        /// 锁标识
        /// </summary>
        public long Identifier;
        
        /// <summary>
        /// 锁栈深度
        /// </summary>
        public int LockDepth;
    }

    public struct CoroutineLockInitParam
    {
        /// <summary>
        /// 锁类型
        /// </summary>
        public int LockType;

        /// <summary>
        /// 锁标识
        /// </summary>
        public long Identifier;
        
        /// <summary>
        /// 锁栈深度
        /// </summary>
        public int LockDepth;
    }
}