/* This file is Generated, DON'T MODIFY THIS FILE. */
using MemoryPack;
using System.Collections.Generic;
using Bliss.Pool;

namespace Bliss
{
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectAddRequest)]
    [MessageResponse("Bliss.ObjectAddResponse")]
    public partial class ObjectAddRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Type { get; set; }
        
        [MemoryPackOrder(3)]
        public long Key { get; set; }
        
        [MemoryPackOrder(4)]
        public ActorAddress ActorId { get; set; }
        
        public static ObjectAddRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectAddRequest), isFromPool) as ObjectAddRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.ActorId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<ActorAddress>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectAddResponse)]
    public partial class ObjectAddResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        public static ObjectAddResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectAddResponse), isFromPool) as ObjectAddResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectRemoveRequest)]
    [MessageResponse("Bliss.ObjectRemoveResponse")]
    public partial class ObjectRemoveRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Type { get; set; }
        
        [MemoryPackOrder(3)]
        public long Key { get; set; }
        
        public static ObjectRemoveRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectRemoveRequest), isFromPool) as ObjectRemoveRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Type = default;
            this.Key = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectRemoveResponse)]
    public partial class ObjectRemoveResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        public static ObjectRemoveResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectRemoveResponse), isFromPool) as ObjectRemoveResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectGetRequest)]
    [MessageResponse("Bliss.ObjectGetResponse")]
    public partial class ObjectGetRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Type { get; set; }
        
        [MemoryPackOrder(3)]
        public long Key { get; set; }
        
        public static ObjectGetRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectGetRequest), isFromPool) as ObjectGetRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Type = default;
            this.Key = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectGetResponse)]
    public partial class ObjectGetResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        [MemoryPackOrder(4)]
        public int Type { get; set; }
        
        [MemoryPackOrder(5)]
        public ActorAddress ActorId { get; set; }
        
        public static ObjectGetResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectGetResponse), isFromPool) as ObjectGetResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Type = default;
            this.ActorId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<ActorAddress>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectLockRequest)]
    [MessageResponse("Bliss.ObjectLockResponse")]
    public partial class ObjectLockRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Type { get; set; }
        
        [MemoryPackOrder(3)]
        public long Key { get; set; }
        
        [MemoryPackOrder(4)]
        public ActorAddress ActorId { get; set; }
        
        [MemoryPackOrder(5)]
        public int Time { get; set; }
        
        public static ObjectLockRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectLockRequest), isFromPool) as ObjectLockRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.ActorId = default;
            this.Time = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<ActorAddress>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectLockResponse)]
    public partial class ObjectLockResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        public static ObjectLockResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectLockResponse), isFromPool) as ObjectLockResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectUnLockRequest)]
    [MessageResponse("Bliss.ObjectUnLockResponse")]
    public partial class ObjectUnLockRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Type { get; set; }
        
        [MemoryPackOrder(3)]
        public long Key { get; set; }
        
        [MemoryPackOrder(4)]
        public ActorAddress OldActorId { get; set; }
        
        [MemoryPackOrder(5)]
        public ActorAddress NewActorId { get; set; }
        
        public static ObjectUnLockRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectUnLockRequest), isFromPool) as ObjectUnLockRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Type = default;
            this.Key = default;
            this.OldActorId = default;
            this.NewActorId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<ActorAddress>() + System.Runtime.CompilerServices.Unsafe.SizeOf<ActorAddress>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectUnLockResponse)]
    public partial class ObjectUnLockResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        public static ObjectUnLockResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectUnLockResponse), isFromPool) as ObjectUnLockResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectQueryRequest)]
    [MessageResponse("Bliss.ObjectQueryResponse")]
    public partial class ObjectQueryRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public long Key { get; set; }
        
        [MemoryPackOrder(3)]
        public long InstanceId { get; set; }
        
        public static ObjectQueryRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectQueryRequest), isFromPool) as ObjectQueryRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Key = default;
            this.InstanceId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.ObjectQueryResponse)]
    public partial class ObjectQueryResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        [MemoryPackOrder(4)]
        public byte[] Entity { get; set; }
        
        public static ObjectQueryResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(ObjectQueryResponse), isFromPool) as ObjectQueryResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Entity = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.R2G_LoginTokenRequest)]
    [MessageResponse("Bliss.G2R_LoginTokenResponse")]
    public partial class R2G_LoginTokenRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public string Account { get; set; }
        
        [MemoryPackOrder(3)]
        public string Password { get; set; }
        
        public static R2G_LoginTokenRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2G_LoginTokenRequest), isFromPool) as R2G_LoginTokenRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Account = default;
            this.Password = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.G2R_LoginTokenResponse)]
    public partial class G2R_LoginTokenResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        [MemoryPackOrder(4)]
        public long Token { get; set; }
        
        public static G2R_LoginTokenResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2R_LoginTokenResponse), isFromPool) as G2R_LoginTokenResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.Token = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.G2M_PlayerOfflineMessage)]
    [MessageResponse("Bliss.ResponseMessage")]
    public partial class G2M_PlayerOfflineMessage : MessageObject, ILocationMessage 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public long PlayerId { get; set; }
        
        public static G2M_PlayerOfflineMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2M_PlayerOfflineMessage), isFromPool) as G2M_PlayerOfflineMessage;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.PlayerId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.M2M_UnitTransferRequest)]
    [MessageResponse("Bliss.M2M_UnitTransferResponse")]
    public partial class M2M_UnitTransferRequest : MessageObject, IRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public bool IsLogin { get; set; }
        
        [MemoryPackOrder(3)]
        public ActorAddress FromAddress { get; set; }
        
        [MemoryPackOrder(4)]
        public byte[] RawEntity { get; set; }
        
        public static M2M_UnitTransferRequest Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2M_UnitTransferRequest), isFromPool) as M2M_UnitTransferRequest;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.IsLogin = default;
            this.FromAddress = default;
            this.RawEntity = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<bool>() + System.Runtime.CompilerServices.Unsafe.SizeOf<ActorAddress>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(InnerMessageOpcode.M2M_UnitTransferResponse)]
    public partial class M2M_UnitTransferResponse : MessageObject, IResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        public static M2M_UnitTransferResponse Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2M_UnitTransferResponse), isFromPool) as M2M_UnitTransferResponse;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    public static partial  class InnerMessageOpcode
    {
        public const ushort ObjectAddRequest = 20000;
        public const ushort ObjectAddResponse = 20001;
        public const ushort ObjectRemoveRequest = 20002;
        public const ushort ObjectRemoveResponse = 20003;
        public const ushort ObjectGetRequest = 20004;
        public const ushort ObjectGetResponse = 20005;
        public const ushort ObjectLockRequest = 20006;
        public const ushort ObjectLockResponse = 20007;
        public const ushort ObjectUnLockRequest = 20008;
        public const ushort ObjectUnLockResponse = 20009;
        public const ushort ObjectQueryRequest = 20010;
        public const ushort ObjectQueryResponse = 20011;
        public const ushort R2G_LoginTokenRequest = 20012;
        public const ushort G2R_LoginTokenResponse = 20013;
        public const ushort G2M_PlayerOfflineMessage = 20014;
        public const ushort M2M_UnitTransferRequest = 20015;
        public const ushort M2M_UnitTransferResponse = 20016;
    }
}
