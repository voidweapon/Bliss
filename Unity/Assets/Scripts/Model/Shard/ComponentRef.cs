using Bliss.Core;

namespace Bliss
{
    public class ComponentRef<T> : Entity where T : Entity
    {
        public EntityRef<T> CmpRef { get; private set; }
        public T Component => (T)CmpRef;

        public void SetComponent(T component)
        {
            CmpRef = component;
        }


        public override void Dispose()
        {
            base.Dispose();
            CmpRef = null;
        }
    }
    
}