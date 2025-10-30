/* This file is Generated, DON'T MODIFY THIS FILE. */
using MemoryPack;
using System.Collections.Generic;
using Bliss.Pool;

namespace Bliss
{
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.PingMessage)]
    public partial class PingMessage : MessageObject, ISessionMessage 
    {
        
        [MemoryPackOrder(1)]
        public int PacketId { get; set; }
        
        [MemoryPackOrder(2)]
        public long LocalTime { get; set; }
        
        public static PingMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(PingMessage), isFromPool) as PingMessage;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.PacketId = default;
            this.LocalTime = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private const int _sizeHint = sizeof(int) + sizeof(long);
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.PongMessage)]
    public partial class PongMessage : MessageObject, ISessionMessage 
    {
        
        [MemoryPackOrder(1)]
        public int PacketId { get; set; }
        
        [MemoryPackOrder(2)]
        public long LocalTime { get; set; }
        
        public static PongMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(PongMessage), isFromPool) as PongMessage;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.PacketId = default;
            this.LocalTime = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private const int _sizeHint = sizeof(int) + sizeof(long);
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.C2G_EnterLevel)]
    [MessageResponse("Bliss.G2C_EnterLevel")]
    public partial class C2G_EnterLevel : MessageObject, ISessionRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        public static C2G_EnterLevel Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_EnterLevel), isFromPool) as C2G_EnterLevel;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private const int _sizeHint = sizeof(int);
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.G2C_EnterLevel)]
    public partial class G2C_EnterLevel : MessageObject, ISessionResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        public static G2C_EnterLevel Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_EnterLevel), isFromPool) as G2C_EnterLevel;
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
       
        private const int _sizeHint = sizeof(int) + sizeof(int);
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.TransformSimpleSyncMessage)]
    public partial class TransformSimpleSyncMessage : MessageObject, IActorMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public Unity.Mathematics.float3 Position { get; set; }
        
        [MemoryPackOrder(3)]
        public Unity.Mathematics.float4 Rotation { get; set; }
        
        [MemoryPackOrder(4)]
        public Unity.Mathematics.float3 Velocity { get; set; }
        
        [MemoryPackOrder(5)]
        public Unity.Mathematics.float3 AngularVelocity { get; set; }
        
        public static TransformSimpleSyncMessage Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(TransformSimpleSyncMessage), isFromPool) as TransformSimpleSyncMessage;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.Position = default;
            this.Rotation = default;
            this.Velocity = default;
            this.AngularVelocity = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private const int _sizeHint = sizeof(long) + 3 * sizeof(float) + 4 * sizeof(float) + 3 * sizeof(float) + 3 * sizeof(float);
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.PropertySync_Long)]
    public partial class PropertySync_Long : MessageObject, IActorMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public int PropertyIndex { get; set; }
        
        [MemoryPackOrder(3)]
        public long Value { get; set; }
        
        public static PropertySync_Long Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(PropertySync_Long), isFromPool) as PropertySync_Long;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.PropertyIndex = default;
            this.Value = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private const int _sizeHint = sizeof(long) + sizeof(int) + sizeof(long);
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    public static partial  class OuterMessageOpcode
    {
        public const ushort PingMessage = 1000;
        public const ushort PongMessage = 1001;
        public const ushort C2G_EnterLevel = 1002;
        public const ushort G2C_EnterLevel = 1003;
        public const ushort TransformSimpleSyncMessage = 1004;
        public const ushort PropertySync_Long = 1005;
    }
}
