using System.Reflection;
using Bliss.ProjectRule.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;


namespace TestProject1;

public class EntitySystemGeneratorTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test2()
    {
        Compilation inputCompilation = CreateCompilation(@"
using System;

namespace Bliss.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntitySystemOfAttribute : System.Attribute
    {
        public System.Type EntityType { get; private set; }

        public EntitySystemOfAttribute(System.Type entityType)
        {
        EntityType = entityType;
        }
    }
}


namespace TestES
{
    [EntitySystemOf(typeof(Entity))]
    public static partial class GenTest
    {

    }


    public interface IEntitySystemConstraint
    {
    }

    public interface IAwake<T> : IEntitySystemConstraint
    {

    }

    public interface IAwake<T, TParam0> : IEntitySystemConstraint
    {

    }

    public interface IUpdate : IEntitySystemConstraint
    {

    }



    public class Entity : IAwake<int, EntityInitData>,  IUpdate
    {

    }

    public struct EntityInitData 
    {

    }
}
");        
        
        // directly create an instance of the generator
        // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
        EntitySystemGenerator testGenerator = new EntitySystemGenerator();
        
        // Create the driver that will control the generation, passing in our generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(testGenerator);
        
        // Run the generation pass
        // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        // Or you can access the individual results on a by-generator basis
        GeneratorRunResult generatorResult = runResult.Results[0];
        Assert.Pass();
    }
    
    
    private static Compilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}