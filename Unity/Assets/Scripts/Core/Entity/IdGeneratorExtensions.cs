using Bliss.Id;

namespace Bliss
{
    public static class IdGeneratorExtensions
    {
        public static long GenerateId(this IdGenerator generator)
        {
            return generator.GenerateId(TimeInfo.Instance.FrameTime);
        }
        
        public static long GenerateInstanceId(this IdGenerator generator)
        {
            return generator.GenerateInstanceId(TimeInfo.Instance.FrameTime);
        }
    }
}