using System;

namespace Bliss
{
    public interface ISystem
    {
        Type GetEntityType();

        Type GetSystemCategoryType();
        
        ESystemUpdateGroup GetUpdateGroup();
    }

    public interface IUpdateGroupSystem : ISystem
    {
        void Run(Entity entity);
    }
}