using System;
using System.ComponentModel;

namespace Bliss
{
    public abstract class DisposableObject : BObject, IDisposable, ISupportInitialize
    {
        public virtual void Dispose()
        {
            
        }

        public virtual void BeginInit()
        {
            
        }

        public virtual void EndInit()
        {
            
        }
    }
}