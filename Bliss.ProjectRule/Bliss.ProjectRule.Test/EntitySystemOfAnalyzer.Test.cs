using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Bliss.ProjectRule.Analyzer.EntitySystemOfAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace TestProject1;

public class EntitySystemOfAnalyzerTests
{
    [Test]
    public async Task Test()
    {
        var sourceCode = @"
using System;

namespace TestProject1.TestData.MemberVisibleToAnalyzer
{



[AttributeUsage(AttributeTargets.Class)]
public class EntitySystemOfAttribute : Attribute
{
    public System.Type EntityType { get; private set; }
    
    public EntitySystemOfAttribute(System.Type entityType)
    {
        EntityType = entityType;
    }
}

public interface IEntitySystemConstraint
{

}

public interface IAwake<T> : IEntitySystemConstraint
{
    
}

public interface IAwake<T,T1, T2> : IEntitySystemConstraint
{
    
}

public interface IAwakeSystem<T> 
{
    void Run(Entity entity, T param);
}

public abstract class AwakeSystem<TEntity, T> : IAwakeSystem<T> where TEntity : Entity, IAwake<T>
{
    public void Run(Entity entity, T param)
    {
        Awake((TEntity)entity, param);
    }
    
    protected abstract void Awake(TEntity self, T param);
}

[EntitySystemOf(typeof(Entity))]
public static partial class GenEntitySystemTestData2
{
    public static void __Awake(this Entity e, int p)
    {
        
    }
}

public class Entity : IAwake<int, DD, bool>
{
    
}

public struct DD 
{
    
}

}

";

        await new Verify()
        {
            TestCode = sourceCode,
        }.RunAsync();
    }
}