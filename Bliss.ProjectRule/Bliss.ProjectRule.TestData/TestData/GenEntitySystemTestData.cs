using Bliss.Core;
using TestProject1.TestData.MemberVisibleToAnalyzer.Mode;

namespace TestProject1.TestData.MemberVisibleToAnalyzer;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class EntitySystemAttribute : Attribute
{
    
}

public static partial class GenEntitySystemTestData
{
    [EntitySystem]
    public static void Run()
    {

    }
}


public interface IEntitySystemConstraint
{

}

public interface IAwake : IEntitySystemConstraint
{
    
}

public interface IAwake<T> : IEntitySystemConstraint
{
    
}

public interface IAwakeSystem 
{
    
}

public interface IAwakeSystem<T> 
{
    void Run(Mode.Entity entity, T param);
}

public abstract class AwakeSystem<TEntity, T> : IAwakeSystem<T> where TEntity : Mode.Entity, IAwake<T>
{
    public virtual int GetPriority()
    {
        return 0;
    }
    
    public void Run(Mode.Entity entity, T param)
    {
        Awake((TEntity)entity, param);
    }
    
    protected abstract void Awake(TEntity self, T param);
}



[EntitySystemOf(typeof(Mode.Entity))]
public static partial class GenEntitySystemTestData2
{
    private static void Awake(this Entity self, int param1)
    {

    }
}

