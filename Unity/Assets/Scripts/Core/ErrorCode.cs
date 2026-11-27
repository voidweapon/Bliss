namespace Bliss
{
    public static partial class ErrorCode
    {
        public const int ERR_Success = 0;


        // 110000 以上，避免跟SocketError冲突

        public const int ERR_RPC_FAIL = 110001;
        public const int ERR_ACTOR_LOCATION_SEND_TIMEOUT = 110003;
        public const int ERR_ACTOR_LOCATION_SEND_TIMEOUT_2 = 110004;
        public const int ERR_ACTOR_LOCATION_SEND_TIMEOUT_3 = 110005;
        public const int ERR_ACTOR_LOCATION_SEND_TIMEOUT_4 = 110006;

        public const int ERR_ACTOR_NOT_FOUND = 110005;

        public const int ERR_CONNECT_GATE_KEY_ERROR = 110006;
        
        //-----------------------------------
        // 小于这个Rpc会抛异常，大于这个异常的error需要自己判断处理，也就是说需要处理的错误应该要大于该值
        public const int ERR_Exception = 200000;
        
        public const int ERR_TASK_ABORTED = 200001;
        public const int ERR_TASK_TIMEOUT = 200002;

        public const int ERR_NOT_FOUND_MAP = 200003;

        /// <summary>The target Server Root has not completed initialization.</summary>
        public const int ERR_ROOT_NOT_READY = 200004;
        
        public static bool IsRpcNeedThrowException(int error)
        {
            if (error == 0)
            {
                return false;
            }
            // ws平台返回错误专用的值
            if (error == -1)
            {
                return false;
            }

            if (error > ERR_Exception)
            {
                return false;
            }

            return true;
        }
    }
}