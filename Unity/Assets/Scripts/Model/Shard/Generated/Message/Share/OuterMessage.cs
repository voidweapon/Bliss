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
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + 3 * sizeof(float) + 4 * sizeof(float) + 3 * sizeof(float) + 3 * sizeof(float);
        
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
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.C2R_Login)]
    [MessageResponse("Bliss.G2C_Login")]
    public partial class C2R_Login : MessageObject, ISessionRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public string Account { get; set; }
        
        [MemoryPackOrder(3)]
        public string Password { get; set; }
        
        public static C2R_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2R_Login), isFromPool) as C2R_Login;
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
    [MessageOpcode(OuterMessageOpcode.R2C_Login)]
    public partial class R2C_Login : MessageObject, ISessionResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        
        [MemoryPackOrder(4)]
        public long GateToken { get; set; }
        
        [MemoryPackOrder(5)]
        public string GateAddress { get; set; }
        
        public static R2C_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(R2C_Login), isFromPool) as R2C_Login;
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
            this.GateToken = default;
            this.GateAddress = default;
                
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
    [MessageOpcode(OuterMessageOpcode.C2G_Login)]
    [MessageResponse("Bliss.G2C_Login")]
    public partial class C2G_Login : MessageObject, ISessionRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public long GateToken { get; set; }
        
        public static C2G_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_Login), isFromPool) as C2G_Login;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.GateToken = default;
                
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
    [MessageOpcode(OuterMessageOpcode.G2C_Login)]
    public partial class G2C_Login : MessageObject, ISessionResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        // 登录成功后返回玩家Id
        [MemoryPackOrder(4)]
        public long PlayerId { get; set; }
        
        public static G2C_Login Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_Login), isFromPool) as G2C_Login;
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
            this.PlayerId = default;
                
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
    [MessageOpcode(OuterMessageOpcode.C2G_EnterMap)]
    [MessageResponse("Bliss.G2C_EnterMap")]
    public partial class C2G_EnterMap : MessageObject, ISessionRequest 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int MapId { get; set; }
        
        public static C2G_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2G_EnterMap), isFromPool) as C2G_EnterMap;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.MapId = default;
                
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
    [MessageOpcode(OuterMessageOpcode.G2C_EnterMap)]
    public partial class G2C_EnterMap : MessageObject, ISessionResponse 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public int Error { get; set; }
        
        [MemoryPackOrder(3)]
        public string Message { get; set; }
        // 自己的UnitId
        [MemoryPackOrder(4)]
        public long UnitId { get; set; }
        
        public static G2C_EnterMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(G2C_EnterMap), isFromPool) as G2C_EnterMap;
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
            this.UnitId = default;
                
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
    [MessageOpcode(OuterMessageOpcode.DTOUnitInfo)]
    public partial class DTOUnitInfo : MessageObject 
    {
        
        [MemoryPackOrder(1)]
        public long UnitId { get; set; }
        
        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }
        
        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 Position { get; set; }
        
        [MemoryPackOrder(4)]
        public Unity.Mathematics.quaternion Rotation { get; set; }
        
        [MemoryPackOrder(5)]
        public Bliss.ENetRole LocalRole { get; set; }
        
        [MemoryPackOrder(6)]
        public Bliss.ENetRole RemoteRole { get; set; }
        
        public static DTOUnitInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(DTOUnitInfo), isFromPool) as DTOUnitInfo;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.UnitId = default;
            this.ConfigId = default;
            this.Position = default;
            this.Rotation = default;
            this.LocalRole = default;
            this.RemoteRole = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + 3 * sizeof(float) + 4 * sizeof(float) + System.Runtime.CompilerServices.Unsafe.SizeOf<Bliss.ENetRole>() + System.Runtime.CompilerServices.Unsafe.SizeOf<Bliss.ENetRole>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_CreateMyUnit)]
    public partial class M2C_CreateMyUnit : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public DTOUnitInfo Unit { get; set; }
        
        public static M2C_CreateMyUnit Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateMyUnit), isFromPool) as M2C_CreateMyUnit;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.Unit = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<DTOUnitInfo>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_CreateUnits)]
    public partial class M2C_CreateUnits : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public List<DTOUnitInfo> Units { get; set; } = new List<DTOUnitInfo>();
        
        public static M2C_CreateUnits Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateUnits), isFromPool) as M2C_CreateUnits;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.Units?.Clear();
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_ChangeMap)]
    public partial class M2C_ChangeMap : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public bool IsLogin { get; set; }
        
        [MemoryPackOrder(3)]
        public int LevelConfigId { get; set; }
        
        [MemoryPackOrder(4)]
        public long LevelId { get; set; }
        
        public static M2C_ChangeMap Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_ChangeMap), isFromPool) as M2C_ChangeMap;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.IsLogin = default;
            this.LevelConfigId = default;
            this.LevelId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<bool>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.DTOGameplayTargetInfo)]
    public partial class DTOGameplayTargetInfo : MessageObject 
    {
        
        [MemoryPackOrder(1)]
        public Unity.Mathematics.float3 Position { get; set; }
        
        [MemoryPackOrder(2)]
        public Unity.Mathematics.float3 Direction { get; set; }
        
        [MemoryPackOrder(3)]
        public long EntityId { get; set; }
        
        public static DTOGameplayTargetInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(DTOGameplayTargetInfo), isFromPool) as DTOGameplayTargetInfo;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.Position = default;
            this.Direction = default;
            this.EntityId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = 3 * sizeof(float) + 3 * sizeof(float) + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.DTOBulletInfo)]
    public partial class DTOBulletInfo : MessageObject 
    {
        
        [MemoryPackOrder(1)]
        public long BulletId { get; set; }
        
        [MemoryPackOrder(2)]
        public int ConfigId { get; set; }
        
        [MemoryPackOrder(3)]
        public Unity.Mathematics.float3 Position { get; set; }
        
        [MemoryPackOrder(4)]
        public Unity.Mathematics.quaternion Rotation { get; set; }
        
        [MemoryPackOrder(5)]
        public Bliss.ENetRole LocalRole { get; set; }
        
        [MemoryPackOrder(6)]
        public Bliss.ENetRole RemoteRole { get; set; }
        
        [MemoryPackOrder(7)]
        public DTOGameplayTargetInfo TargetInfo { get; set; }
        
        [MemoryPackOrder(8)]
        public long RemainingLifetimeMS { get; set; }

        [MemoryPackOrder(9)]
        public long ShooterEntityId { get; set; }
        
        public static DTOBulletInfo Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(DTOBulletInfo), isFromPool) as DTOBulletInfo;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.BulletId = default;
            this.ConfigId = default;
            this.Position = default;
            this.Rotation = default;
            this.LocalRole = default;
            this.RemoteRole = default;
            this.TargetInfo = default;
            this.RemainingLifetimeMS = default;
            this.ShooterEntityId = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + 3 * sizeof(float) + 4 * sizeof(float) + System.Runtime.CompilerServices.Unsafe.SizeOf<Bliss.ENetRole>() + System.Runtime.CompilerServices.Unsafe.SizeOf<Bliss.ENetRole>() + System.Runtime.CompilerServices.Unsafe.SizeOf<DTOGameplayTargetInfo>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_CreateBullets)]
    public partial class M2C_CreateBullets : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public List<DTOBulletInfo> Bullets { get; set; } = new List<DTOBulletInfo>();
        
        public static M2C_CreateBullets Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateBullets), isFromPool) as M2C_CreateBullets;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.Bullets?.Clear();
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_CreateSceneActors)]
    public partial class M2C_CreateSceneActors : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public List<Bliss.SceneActorData> SceneActors { get; set; } = new List<Bliss.SceneActorData>();
        
        public static M2C_CreateSceneActors Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CreateSceneActors), isFromPool) as M2C_CreateSceneActors;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.SceneActors?.Clear();
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_DestroyEntities)]
    public partial class M2C_DestroyEntities : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public List<long> EntityIds { get; set; } = new List<long>();
        
        public static M2C_DestroyEntities Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_DestroyEntities), isFromPool) as M2C_DestroyEntities;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.EntityIds?.Clear();
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.C2M_CharacterMoveData)]
    [MessageResponse("Bliss.ResponseMessage")]
    public partial class C2M_CharacterMoveData : MessageObject, ILocationMessage 
    {
        
        [MemoryPackOrder(1)]
        public int RpcId { get; set; }
        
        [MemoryPackOrder(2)]
        public List<CharacterNetworkMoveData> MoveData { get; set; } = new List<CharacterNetworkMoveData>();
        
        [MemoryPackOrder(3)]
        public bool bDisableCombinedScopedMove { get; set; }
        
        [MemoryPackOrder(4)]
        public bool bIsDualHybridRootMotionMove { get; set; }
        
        public static C2M_CharacterMoveData Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(C2M_CharacterMoveData), isFromPool) as C2M_CharacterMoveData;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.RpcId = default;
            this.MoveData?.Clear();
            this.bDisableCombinedScopedMove = default;
            this.bIsDualHybridRootMotionMove = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<int>() + System.Runtime.CompilerServices.Unsafe.SizeOf<bool>() + System.Runtime.CompilerServices.Unsafe.SizeOf<bool>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_AckGoodCharacterMoveData)]
    public partial class M2C_AckGoodCharacterMoveData : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public float TimeStamp { get; set; }
        
        public static M2C_AckGoodCharacterMoveData Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_AckGoodCharacterMoveData), isFromPool) as M2C_AckGoodCharacterMoveData;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.TimeStamp = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<float>();
        
        partial void CustomSizeHint(ref int sizeHint);
        
        public override int SizeHint()
        {
            int sizeHint = _sizeHint;
            CustomSizeHint(ref sizeHint);
            return sizeHint;
        }
    }
    
    
    [MemoryPackable]
    [MessageOpcode(OuterMessageOpcode.M2C_CharacterMovementRepData)]
    public partial class M2C_CharacterMovementRepData : MessageObject, IMessage 
    {
        
        [MemoryPackOrder(1)]
        public long ActorId { get; set; }
        
        [MemoryPackOrder(2)]
        public CharacterRepMovement ReplicatedMovement { get; set; }
        
        public static M2C_CharacterMovementRepData Create(bool isFromPool = false)
        {
            return ObjectPool.Instance.Fetch(typeof(M2C_CharacterMovementRepData), isFromPool) as M2C_CharacterMovementRepData;
        }
        
        partial void CustomDispose();
        
        public override void Dispose()
        {
            if (!this.IsFromPool)
            {
                return;
            }
        
            this.ActorId = default;
            this.ReplicatedMovement = default;
                
            ObjectPool.Instance.Recycle(this);
        }
       
        private static int _sizeHint = System.Runtime.CompilerServices.Unsafe.SizeOf<long>() + System.Runtime.CompilerServices.Unsafe.SizeOf<CharacterRepMovement>();
        
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
        public const ushort C2R_Login = 1006;
        public const ushort R2C_Login = 1007;
        public const ushort C2G_Login = 1008;
        public const ushort G2C_Login = 1009;
        public const ushort C2G_EnterMap = 1010;
        public const ushort G2C_EnterMap = 1011;
        public const ushort DTOUnitInfo = 1012;
        public const ushort M2C_CreateMyUnit = 1013;
        public const ushort M2C_CreateUnits = 1014;
        public const ushort M2C_ChangeMap = 1015;
        public const ushort DTOGameplayTargetInfo = 1016;
        public const ushort DTOBulletInfo = 1017;
        public const ushort M2C_CreateBullets = 1018;
        public const ushort M2C_CreateSceneActors = 1019;
        public const ushort M2C_DestroyEntities = 1020;
        public const ushort C2M_CharacterMoveData = 1021;
        public const ushort M2C_AckGoodCharacterMoveData = 1022;
        public const ushort M2C_CharacterMovementRepData = 1023;
    }
}
