using System.Reflection;
using Bliss.ProjectRule.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
namespace TestProject1;

public class MemoryPackPolymorphismGeneratorTests
{
    [SetUp]
    public void Setup()
    {
    }
    
    [Test]
    public void Test()
    {
        Compilation inputCompilation = CreateCompilation(@"


namespace MemoryPack
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MemoryPackableAttribute : System.Attribute
    {

    }
}

namespace TestES
{
    using MemoryPack;

    [AttributeUsage(AttributeTargets.Class)]
    public class SerializePolymorphismAttribute : System.Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SerializePolymorphismRegisterAttribute : System.Attribute
    {

    }

    [MemoryPackable]
    [SerializePolymorphism]
    public partial class Entity
    {
        public int HP;
        public int MP;
    }

    [MemoryPackable]
    [SerializePolymorphism]
    public partial class Unit : Entity
    {
        public float X;
        public float Y;
    }

    [MemoryPackable]
    [SerializePolymorphism]
    public partial class Hero : Unit, IMover
    {
        public float Tag;
    }

    public interface IMover
    {
    }

    [SerializePolymorphismRegister]
    public static partial class MemoryPackSerializerRegister : Unit, IMover
    {
        static partial void Register();

        public static void Init()
        {
            Register();
        }
    }
}

");
        
        // directly create an instance of the generator
        // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
        MemoryPackPolymorphismGenerator testGenerator = new MemoryPackPolymorphismGenerator();
        
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