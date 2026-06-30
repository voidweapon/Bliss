using System;
using System.Runtime.InteropServices;
using MemoryPack;

#if DOTNET
using MongoDB.Bson.Serialization.Attributes;
#endif


namespace Bliss
{ 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [MemoryPackable]
    public partial struct RootAddress : IEquatable<RootAddress>
    {
        [MemoryPackOrder(0)]
        public int Process;
        [MemoryPackOrder(1)]
        public int Root;

        public RootAddress(int process, int root)
        {
            Process = process;
            Root = root;
        }
        
        public static bool operator ==(RootAddress left, RootAddress right)
        {
            return left.Process == right.Process && left.Root == right.Root;
        }

        public static bool operator !=(RootAddress left, RootAddress right)
        {
            return !(left == right);
        }
        
        public bool Equals(RootAddress other)
        {
            return this.Process == other.Process && this.Root == other.Root;
        }

        public override bool Equals(object obj)
        {
            return obj is RootAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Process, this.Root);
        }

        public override string ToString()
        {
            return $"{this.Process}:{this.Root}";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [MemoryPackable]
    public partial struct ActorAddress : IEquatable<ActorAddress>
    {
        [MemoryPackOrder(0)]
        public RootAddress RootAddress;
        [MemoryPackOrder(1)]
        public long InstanceId;
        
    #if DOTNET
        [BsonIgnore]
    #endif
        [MemoryPackIgnore]
        public int Process
        {
            get
            {
                return this.RootAddress.Process;
            }
            set
            {
                this.RootAddress.Process = value;
            }
        }
            
    #if DOTNET
        [BsonIgnore]
    #endif
        [MemoryPackIgnore]
        public int Root
        {
            get
            {
                return this.RootAddress.Root;
            }
            set
            {
                this.RootAddress.Root = value;
            }
        }

        public const long CFirstInstanceId = 1;
        
        public ActorAddress(int process, int root)
        {
            this.RootAddress = new RootAddress(process, root);
            this.InstanceId = CFirstInstanceId;
        }
            
        public ActorAddress(int process, int root, long instanceId)
        {
            this.RootAddress = new RootAddress(process, root);
            this.InstanceId = instanceId;
        }
            
        public ActorAddress(RootAddress rootAddress): this(rootAddress, CFirstInstanceId)
        {
        }
            
        public ActorAddress(RootAddress rootAddress, long instanceId)
        {
            this.RootAddress = rootAddress;
            this.InstanceId = instanceId;
        }
        
        public static bool operator ==(ActorAddress left, ActorAddress right)
        {
            return left.InstanceId == right.InstanceId && left.RootAddress == right.RootAddress;
        }

        public static bool operator !=(ActorAddress left, ActorAddress right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{this.Process}:{this.Root}:{this.InstanceId}";
        }
        
        public bool Equals(ActorAddress other)
        {
            return this.RootAddress == other.RootAddress && this.InstanceId == other.InstanceId;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.RootAddress, this.InstanceId);
        }
    }
}

