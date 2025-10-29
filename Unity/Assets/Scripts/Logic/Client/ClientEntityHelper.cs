
namespace Bliss
{
    public  static class ClientEntityHelper
    {
        public static TComponent GetSceneComponent<TComponent>(this Entity self) where TComponent : Entity
        {
            return self.Scene<LogicScene>()?.GetComponent<TComponent>();
        }
    }
}