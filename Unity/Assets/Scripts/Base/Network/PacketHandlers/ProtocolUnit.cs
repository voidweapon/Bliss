namespace Bliss.Core.Network
{
    public  abstract class AProtocolUnit
    {
        internal ProtocolStack _protoStack;
        
        public abstract int HeaderLength { get; }

        /// <summary>
        /// 可用数据数量
        /// </summary>
        public int Available { get; private set; }

        /// <summary>
        /// 上层数据输入给本层
        /// </summary>
        /// <returns></returns>
        public abstract int Send();
        
        /// <summary>
        /// 输出给数据下层
        /// </summary>
        /// <returns></returns>
        public abstract int Output();


        /// <summary>
        /// 向上层提交数据
        /// </summary>
        /// <returns></returns>
        public abstract int Receive();
        
        /// <summary>
        /// 下层数据，提交给本层
        /// </summary>
        /// <returns></returns>
        public abstract int Input();

        protected void OnError(int code)
        {
            
        }
    }
}