using System;

namespace Bliss
{
    public class RpcException : Exception
    {
        public int ErrorCode { get; }
        
        public RpcException(int error, string message): base(message)
        {
            ErrorCode = error;
        }

        public override string ToString()
        {
            return $"ErrorCode: {ErrorCode}\n{base.ToString()}";
        }
    }
}