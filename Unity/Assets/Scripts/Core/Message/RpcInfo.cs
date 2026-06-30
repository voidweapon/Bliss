using System;
using ET;

namespace Bliss
{
    public readonly struct RpcInfo
    {
        public Type RequestType { get; }
        
        private readonly ETTask<IResponse> tcs;

        public RpcInfo(Type requestType)
        {
            RequestType = requestType;
            tcs = ETTask<IResponse>.Create(true);
        }
        
        public void SetResult(IResponse response)
        {
            tcs.SetResult(response);
        }

        public void SetException(Exception exception)
        {
            tcs.SetException(exception);
        }

        public async ETTask<IResponse> Wait()
        {
            return await tcs;
        }
    }
}