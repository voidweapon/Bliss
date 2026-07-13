using System.Collections;
using Bliss;
using MemoryPack;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public class MemoryPack_Tests
{
    [SetUp]
    public void Init()
    {
        MemoryPackFormatterProvider.Register<Vector_NetQuantize10>(new Vector_NetQuantize10Formtter());
        MemoryPackFormatterProvider.Register<Vector_NetQuantize100>(new Vector_NetQuantize100Formtter());
    }
    // A Test behaves as an ordinary method
    [Test]
    public void MemoryPack_Vector_NetQuantize_Test()
    {
        Vector_NetQuantize10 vector = new Vector_NetQuantize10();
        vector.Value = new float3(1, 2, 3);
        
        var bin = MemoryPackSerializer.Serialize(vector);
        var val = MemoryPackSerializer.Deserialize<Vector_NetQuantize10>(bin);
        
        Assert.AreEqual(vector.Value, val.Value);
    }
    
    [Test]
    public void MemoryPack_Vector_NetQuantize_Float_Test()
    {
        Vector_NetQuantize10 vector = new Vector_NetQuantize10();
        vector.Value = new float3(1.24f, 2.35f, 3.76f);
        
        var bin = MemoryPackSerializer.Serialize(vector);
        var val = MemoryPackSerializer.Deserialize<Vector_NetQuantize10>(bin);
        
        float3 expected = new float3(1.2f, 2.4f, 3.8f);
        Assert.AreEqual(expected, val.Value);
    }

    // // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // // `yield return null;` to skip a frame.
    // [UnityTest]
    // public IEnumerator Memorypack_TestsWithEnumeratorPasses()
    // {
    //     // Use the Assert class to test conditions.
    //     // Use yield to skip a frame.
    //     yield return null;
    // }
}
