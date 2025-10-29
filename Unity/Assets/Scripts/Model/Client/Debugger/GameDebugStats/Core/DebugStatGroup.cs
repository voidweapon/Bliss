using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Bliss.Debugger.GameDebugStats
{
    public class DebugStatGroup : IDisposable
    {
        public string GroupName { get; private set; }

        private Dictionary<(int, int, string), WeakDebugStatInfo> _stats = new Dictionary<(int, int, string), WeakDebugStatInfo>();
        
        public DebugStatGroup(string groupName)
        {
            GroupName = groupName;
        }
        
        public void Dispose()
        {
            foreach (var kv in _stats)
            {
                kv.Value?.DebugStat?.Dispose();
            }
            _stats.Clear();
            GroupName = null;
        }

        public void SetDebugStat<TValue>( string propertyName, 
            TValue  value,             
            float4 color,
            bool highlightWhenChange = false)
        {
            var key = (2, 0, propertyName);
            if (!_stats.TryGetValue(key, out var statInfo))
            {
                statInfo = new WeakDebugStatInfo()
                {
                    DebugStat = new DebugStat<TValue>(value),
                    PropertyName = propertyName,
                    Color = color,
                    HighlightWhenChange = highlightWhenChange,
                };
                _stats.Add(key, statInfo);
            }

            statInfo.Color = color;
            statInfo.HighlightWhenChange = highlightWhenChange;
            (statInfo.DebugStat as DebugStat<TValue>)?.SetPropertyValue(value);
        }
        
        public void AddWeakDebugStat<TRef, TValue>(TRef refObj,
            string propertyName,
            float4 color,
            bool highlightWhenChange = false)
        {
            var key = (0, refObj.GetHashCode(), propertyName);
            if (_stats.ContainsKey(key))
            {
                return;
            }
            
            var weakDebugStat = WeakableFactory.CreateWeakDebugStat<TRef, TValue>(refObj, propertyName);
            _stats.Add(key, new WeakDebugStatInfo()
            {
                DebugStat = weakDebugStat,
                PropertyName = propertyName,
                Color = color,
                HighlightWhenChange = highlightWhenChange,
            });
        }
        
        public void AddStaticDebugStat<TValue>(Type staticType,
            string propertyName,
            float4 color,
            bool highlightWhenChange = false)
        {
            var key = (1, staticType.GetHashCode(), propertyName);
            if (_stats.ContainsKey(key))
            {
                return;
            }
            
            var weakDebugStat = WeakableFactory.CreateStaticDebugStat<TValue>(staticType, propertyName);
            _stats.Add(key, new WeakDebugStatInfo()
            {
                DebugStat = weakDebugStat,
                PropertyName = propertyName,
                Color = color,
                HighlightWhenChange = highlightWhenChange,
            });
        }

        public void RemoveWeakDebugStat<TRef>(TRef refObj, string propertyName)
        {
            var key = (0, refObj.GetHashCode(), propertyName);
            if (_stats.Remove(key, out var weakStat))
            {
                weakStat.DebugStat?.Dispose();
            }
        }
        
        public void RemoveStaticDebugStat(Type staticType, string propertyName)
        {
            var key = (1, staticType.GetHashCode(), propertyName);
            if (_stats.Remove(key, out var weakStat))
            {
                weakStat.DebugStat?.Dispose();
            }
        }

        public void Draw(Action<DrawInfo> drawFunc)
        {
            int i = 0;
            foreach (var kv in _stats)
            {
                var statInfo = kv.Value;
                string valueStr = statInfo.DebugStat.GetPropertyValueStr(out bool hasChange);
                
                drawFunc(new DrawInfo()
                {
                    Index = i++,
                    PropertyName = statInfo.PropertyName,
                    ValueStr = valueStr,
                    Color = statInfo.Color,
                    HighlightWhenChange = statInfo.HighlightWhenChange,
                    HasChange = hasChange,
                });
            }
        }
        
        private class WeakDebugStatInfo
        {
            public string PropertyName;
            public IDebugStat DebugStat;
            public float4 Color;
            public bool HighlightWhenChange;
        }

        public struct DrawInfo
        {
            public int Index;
            public string PropertyName;
            public string ValueStr;
            public float4 Color;
            public bool HighlightWhenChange;
            public bool HasChange;
        }
    }
}