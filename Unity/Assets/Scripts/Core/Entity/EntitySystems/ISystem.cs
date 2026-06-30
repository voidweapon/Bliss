using System;

namespace Bliss
{
    public interface ISystem : IComparable<ISystem>
    {
        Type GetEntityType();

        Type GetSystemCategoryType();
        
        ESystemUpdateGroup GetUpdateGroup();

        int GetPriority();
    }

    public interface IUpdateGroupSystem : ISystem
    {
        void Run(Entity entity);
    }
    
    public interface IEventSystem : ISystem
    {
        void Run(Entity entity);
    }
}