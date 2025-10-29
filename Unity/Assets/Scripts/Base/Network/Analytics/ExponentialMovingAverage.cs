using System;

namespace Bliss
{
    public struct ExponentialMovingAverage
    {
        public double Value;
        public double Variance;
        public double StandardDeviation; // absolute value, see test
        
        private readonly double _alpha;
        private readonly EMode  _mode;
        private bool _initialized;

        public ExponentialMovingAverage(int n, EMode mode = EMode.Value)
        {
            // standard N-day EMA alpha calculation
            _alpha = 2.0 / (n + 1);
            _mode = mode;
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }
        
        public void Add(double newValue)
        {
            // simple algorithm for EMA described here:
            // https://en.wikipedia.org/wiki/Moving_average#Exponentially_weighted_moving_variance_and_standard_deviation
            if (_initialized)
            {
                //Value = (1-alpha) * Value + newValue * alpha
                //      = Value - alpha * Value + newValue * alpha
                //      = Value + (newValue - Value) * alpha
                //      =  Value + delta * alpha
                double delta = newValue - Value;
                Value += _alpha * delta;

                if (_mode > EMode.Value)
                {
                    Variance = (1 - _alpha) * (Variance + _alpha * delta * delta);
                }

                if (_mode > EMode.Variance)
                {
                    StandardDeviation = Math.Sqrt(Variance);
                }
            }
            else
            {
                Value = newValue;
                _initialized = true;
            }
        }

        public void Reset()
        {
            _initialized = false;
            Value = 0;
            Variance = 0;
            StandardDeviation = 0;
        }
        
        public enum EMode
        {
            Value,
            
            /// <summary>
            /// Value + Variance
            /// </summary>
            Variance,
            
            /// <summary>
            /// Value + Variance + StandardDeviation
            /// </summary>
            StandardDeviation
        }
    }
}