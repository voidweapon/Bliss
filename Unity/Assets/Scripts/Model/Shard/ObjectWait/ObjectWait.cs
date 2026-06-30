using System;
using System.Collections.Generic;
using Bliss.Core;
using Bliss.Timer;
using ET;

namespace Bliss
{
    public class ObjectWait: Entity, IAwake, IDestroy
    {
        public Dictionary<Type, List<object>> Tcss = new();
    }
    
    public static class WaitTypeError
    {
        public const int Success = 0;
        public const int Destroy = 1;
        public const int Cancel = 2;
        public const int Timeout = 3;
    }

    public interface IWaitType
    {
        int Error
        {
            get;
            set;
        }
    }
    
    [EntitySystemOf(typeof(ObjectWait))]
    public static partial class ObjectWaitSystems
    {
        private static void Awake(this ObjectWait self)
        {
            self.Tcss.Clear();
        }
        private static void Destroy(this ObjectWait self)
        {
            foreach(var p in self.Tcss)
            {
                foreach(object v in p.Value)
                {
                    ((IDestroyRun)v).SetResult();
                }
            }
            self.Tcss.Clear();
        }
        
        public static async ETTask<T> Wait<T>(this ObjectWait self, ETCancellationToken cancellationToken = null) where T : struct, IWaitType
        {
            ResultCallback<T> tcs = new ResultCallback<T>();
            Type type = typeof (T);
            self.Add(type, tcs);

            void CancelAction()
            {
                self.Notify(new T() { Error = WaitTypeError.Cancel });
            }

            T ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs.Task;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }

        public static async ETTask<T> Wait<T>(this ObjectWait self, int timeout, ETCancellationToken cancellationToken = null) where T : struct, IWaitType
        {
            ResultCallback<T> tcs = new ResultCallback<T>();
            Type type = typeof(T);
            async ETTask WaitTimeout()
            {
                await self.RootScene().GetComponent<TimerComponent>().WaitDurationAsync(timeout, cancellationToken);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                if (tcs.IsDisposed)
                {
                    return;
                }
                
                if (!self.Tcss.TryGetValue(type, out var tcsList))
                {
                    return;
                }
                tcsList.Remove(tcs);
                tcs.SetResult(new T() { Error = WaitTypeError.Timeout });
            }
            
            WaitTimeout().Coroutine();
            
            self.Add(type, tcs);
            
            void CancelAction()
            {
                self.Notify(new T() { Error = WaitTypeError.Cancel });
            }
            
            T ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs.Task;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }

        public static void Notify<T>(this ObjectWait self, T obj, bool ensureExist = false) where T : struct, IWaitType
        {
            Type type = typeof (T);
            if (!self.Tcss.TryGetValue(type, out var tcsList) || tcsList.Count == 0)
            {
                if (ensureExist)
                {
                    Log.Error($"[{nameof(ObjectWait)}]: Notify<{type.FullName}> Before Wait<{type.FullName}>>");
                }
                return;
            }

            foreach(var tcs in tcsList)
            {
                ((ResultCallback<T>) tcs).SetResult(obj);
            }
            tcsList.Clear();
        }


        private static void Add(this ObjectWait self, Type type, object obj)
        {
            if (self.Tcss.TryGetValue(type, out var list))
            {
                list.Add(obj);
            }
            else
            {
                self.Tcss.Add(type, new List<object> { obj });
            }
        }
        

        
        private interface IDestroyRun
        {
            void SetResult();
        }

        private class ResultCallback<TType>: Object, IDestroyRun where TType : struct, IWaitType
        {
            private ETTask<TType> tcs;

            public ResultCallback()
            {
                this.tcs = ETTask<TType>.Create(true);
            }

            public bool IsDisposed
            {
                get
                {
                    return this.tcs == null;
                }
            }

            public ETTask<TType> Task => this.tcs;

            public void SetResult(TType k)
            {
                var t = tcs;
                this.tcs = null;
                t.SetResult(k);
            }

            public void SetResult()
            {
                var t = tcs;
                this.tcs = null;
                t.SetResult(new TType() { Error = WaitTypeError.Destroy });
            }
        }
    }
}