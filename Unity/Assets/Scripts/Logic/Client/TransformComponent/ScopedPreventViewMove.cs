using System;
using Bliss.Pool;

namespace Bliss
{
    public class ScopedPreventViewMove : IDisposable
    {
        private WeakReference<TransformComponent> _weakReference =  new WeakReference<TransformComponent>(null);

        public static ScopedPreventViewMove Create(TransformComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var obj = ObjectPool.Instance.Fetch<ScopedPreventViewMove>();
            obj._weakReference.SetTarget(target);
            target.AddViewSyncBarrier(obj);
            
            return obj;
        }
        
        
        public void Dispose()
        {
            if (_weakReference.TryGetTarget(out var target))
            {
                target.RemoveViewSyncBarrier(this);
            }
            _weakReference.SetTarget(null);
            
            ObjectPool.Instance.Recycle(this);
        }
    }
}