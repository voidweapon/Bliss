namespace Bliss
{
    public interface ISingletonInit
    {
        void Init();
    }
    
    public interface ISingletonInit<T>
    {
        void Init(T param);
    }
    
    public abstract class ASingleton : DisposableObject
    {
        internal abstract void Register();
    }

    public abstract class Singleton<T> : ASingleton where T : Singleton<T>
    {
        private bool _isDisposed;
        private static T _instance;
        
        public static T Instance
        {
            get
            {
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }
        
        internal sealed override void Register()
        {
            Instance = (T)this;
        }
        
        public bool IsDisposed()
        {
            return this._isDisposed;
        }

        
        public sealed override void Dispose()
        {
            if (this._isDisposed)
            {
                return;
            }
            
            this._isDisposed = true;

            this.Destroy();
            
            Instance = null;
        }
        
        protected virtual void Destroy()
        {
            
        }
    }
}