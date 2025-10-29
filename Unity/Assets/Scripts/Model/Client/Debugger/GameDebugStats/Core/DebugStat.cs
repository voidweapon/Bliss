using System;

namespace Bliss.Debugger.GameDebugStats
{
    public interface IDebugStat : IDisposable
    {
        public Type GetPropertyType();
        public string GetPropertyValueStr(out bool haveChange);
    }
    
    public class DebugStat<TValue> : IDebugStat
    {
        private TValue _prevValue = default;
        private string _prevValueStr;
        private bool _haveChange = false;

        public DebugStat(TValue value)
        {
            _prevValue = value;
            _prevValueStr = _prevValue.ToString();
            _haveChange = false;
        }
        
        public Type GetPropertyType()
        {
           return typeof(TValue);
        }

        public void SetPropertyValue(TValue value)
        {
            if(_prevValue.Equals(value)) return;
            _prevValue = value;
            _prevValueStr = value.ToString();
            _haveChange = true;
        }
        
        public string GetPropertyValueStr(out bool haveChange)
        {
            haveChange = _haveChange;
            _haveChange = false;
            return _prevValueStr;
        }

        public void Dispose()
        {
            _prevValue = default;
        }
    }
}