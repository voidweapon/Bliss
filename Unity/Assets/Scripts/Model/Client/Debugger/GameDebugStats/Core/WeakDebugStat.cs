using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Bliss.Debugger.GameDebugStats
{
    public abstract class AWeakDebugStat : IDebugStat
    {
        protected readonly bool _isStaticStat;
        protected WeakReference WeakReference { get; private set; }

        public AWeakDebugStat(WeakReference reference)
        {
            WeakReference = reference;
            _isStaticStat = false;
        }
        
        public AWeakDebugStat()
        {
            _isStaticStat = true;
        }
        
        public abstract Type GetPropertyType();
        public abstract string GetPropertyValueStr(out bool haveChange);

        public void Dispose()
        {
            WeakReference = null;
        }
        
        protected bool CheckRef()
        {
            return _isStaticStat || WeakReference.IsAlive;
        }
    }
    
    public class WeakDebugStat<TRef, TValue> : AWeakDebugStat
    {
        private readonly Func<TRef, TValue> _getter;
        private TValue _prevValue = default;
        private string _prevValueStr;
        
        public WeakDebugStat(WeakReference refObj, Func<TRef, TValue> getter) : base(refObj)
        {
            _getter = getter;

            if (CheckRef())
            {
                _prevValue = _getter((TRef)WeakReference.Target);
                _prevValueStr = _prevValue.ToString();
            }
        }
        
        public override Type GetPropertyType()
        {
            return typeof(TValue);
        }
        
        public override string GetPropertyValueStr(out bool haveChange)
        {
            haveChange = false;
            if (!CheckRef()) return string.Empty;
            
            TValue current = _getter((TRef)WeakReference.Target);
            haveChange = !_prevValue.Equals(current);
            
            if (!haveChange)
            {
                return _prevValueStr;
            }
            
            _prevValue = current;
            _prevValueStr = current.ToString();
            return _prevValueStr;
        }
    }
    
    public class WeakStaticDebugStat<TValue> : AWeakDebugStat
    {
        private readonly Func<TValue> _getter;
        private TValue _prevValue = default;
        private string _prevValueStr;
        
        public WeakStaticDebugStat(Func<TValue> getter) : base()
        {
            _getter = getter;
            
            if (CheckRef())
            {
                _prevValue = _getter();
                _prevValueStr = _prevValue.ToString();
            }
        }
        
        public override Type GetPropertyType()
        {
            return typeof(TValue);
        }
        
        public override string GetPropertyValueStr(out bool haveChange)
        {
            haveChange = false;
            if (!CheckRef()) return string.Empty;
            
            TValue current = _getter();
            haveChange = !_prevValue.Equals(current);
            
            if (!haveChange)
            {
                return _prevValueStr;
            }
            
            _prevValue = current;
            _prevValueStr = current.ToString();
            return _prevValueStr;
        }
    }
    
    
    public static class WeakableFactory
    {
        public static AWeakDebugStat CreateWeakDebugStat<TRef, TValue>(TRef refObj, string propertyName)
        {
            var param = Expression.Parameter(refObj.GetType(),"refObj");
            var pOrF = Expression.PropertyOrField(param, propertyName);
            var func = Expression.Lambda<Func<TRef, TValue>>(pOrF, param).Compile();
            return new WeakDebugStat<TRef, TValue>(new WeakReference(refObj), func);
        }
        
        public static AWeakDebugStat CreateStaticDebugStat<TValue>(Type staticType, string propertyName)
        {
            var staticPropertyInfo = staticType.GetMember(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
            var pOrF = Expression.MakeMemberAccess(null, staticPropertyInfo[0]);
            var func = Expression.Lambda<Func<TValue>>(pOrF).Compile();
            return new WeakStaticDebugStat<TValue>(func);
        }
    }
}