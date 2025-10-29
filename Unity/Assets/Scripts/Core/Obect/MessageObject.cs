using System;
using Bliss.Pool;
using MemoryPack;

namespace Bliss
{
    [DisableNew]
    public abstract class MessageObject : BObject, IDisposable, IPoolableObject
    {
        public virtual void Dispose()
        {
        
        }

        public abstract int SizeHint();

#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [MemoryPackIgnore]
        public bool IsFromPool { get; set; }
    
#if DOTNET
        [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
#endif
        [MemoryPackIgnore]
        public bool IsInPool { get; set; }
    }
}

