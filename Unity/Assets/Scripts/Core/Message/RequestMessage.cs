using MemoryPack;

namespace Bliss
{
    public interface IMessage
    {
        
    }

    public interface IRequest: IMessage
    {
        int RpcId
        {
            get;
            set;
        }
    }

    public interface IResponse: IMessage
    {
        int Error
        {
            get;
            set;
        }

        string Message
        {
            get;
            set;
        }

        int RpcId
        {
            get;
            set;
        }
    }

    [MemoryPackable]
    [MessageOpcode(ushort.MaxValue)]
    public partial class ResponseMessage : MessageObject, IResponse
    {
        public int Error { get; set; }
        public string Message { get; set; }
        public int RpcId { get; set; }
        
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            return sizeHint;
        }
    }
}