namespace Bliss.Timer
{
    /// <summary>
    /// 用于从其他Invoke中区分出来一个类型专门做TimerCallback
    /// </summary>
    public struct TimerCallback
    {
        public object Userdata;
    }
}