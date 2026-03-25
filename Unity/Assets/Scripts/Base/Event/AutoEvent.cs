using System;
using System.Collections.Generic;

namespace Bliss.Base.Event
{
    public interface IAutoEvent : IDisposable
    {
        void Clear();
    }
    
    public class AutoEventAction : IAutoEvent
    {
        private readonly List<Action> _actions = new List<Action>();

        public static AutoEventAction operator +(AutoEventAction a, Action action)
        {
            a._actions.Add(action);
            return a;
        }
        public static AutoEventAction operator -(AutoEventAction a, Action action)
        {
            a._actions.Remove(action);
            return a;
        }

        public void Invoke()
        {
            foreach (var fun in _actions)
            {
                fun();
            }
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
    
    public class AutoEventAction<T> : IAutoEvent
    {
        private readonly List<Action<T>> _actions = new List<Action<T>>();

        public static AutoEventAction<T> operator +(AutoEventAction<T> a, Action<T> action)
        {
            a._actions.Add(action);
            return a;
        }
        public static AutoEventAction<T> operator -(AutoEventAction<T> a, Action<T> action)
        {
            a._actions.Remove(action);
            return a;
        }

        public void Invoke(T param1)
        {
            foreach (var fun in _actions)
            {
                fun(param1);
            }
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
    
    public class AutoEventAction<TParamA, TParamB> : IAutoEvent
    {
        private readonly List<Action<TParamA, TParamB>> _actions = new List<Action<TParamA, TParamB>>();

        public static AutoEventAction<TParamA, TParamB> operator +(AutoEventAction<TParamA, TParamB> a, Action<TParamA, TParamB> action)
        {
            a._actions.Add(action);
            return a;
        }
        public static AutoEventAction<TParamA, TParamB> operator -(AutoEventAction<TParamA, TParamB> a, Action<TParamA, TParamB> action)
        {
            a._actions.Remove(action);
            return a;
        }

        public void Invoke(TParamA paramA, TParamB  paramB)
        {
            foreach (var fun in _actions)
            {
                fun(paramA,  paramB);
            }
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }

    
    public class AutoEventFunc<TResult> : IAutoEvent
    {
        public event Func<TResult> Event
        {
            add => _funcs.Add(value);
            remove => _funcs.Remove(value);
        }
        private readonly List<Func<TResult>> _funcs = new List<Func<TResult>>();
        
        public TResult Invoke()
        {
            TResult result = default(TResult);
            
            foreach (var fun in _funcs)
            {
                result = fun();
            }
            
            return result;
        }

        public void Clear()
        {
            _funcs.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
    
    public class AutoEventFunc<TParam, TResult> : IAutoEvent
    {
        public event Func<TParam, TResult> Event
        {
            add => _funcs.Add(value);
            remove => _funcs.Remove(value);
        }
        private readonly List<Func<TParam, TResult>> _funcs = new List<Func<TParam, TResult>>();
        
        public TResult Invoke(TParam param1)
        {
            TResult result = default(TResult);
            
            foreach (var fun in _funcs)
            {
                result = fun(param1);
            }
            
            return result;
        }

        public void Clear()
        {
            _funcs.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}