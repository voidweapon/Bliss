using System.Reflection;
using Bliss.Core;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Bliss.Tests;

public class EntityBsonSerialize_Test
{
    private TypeManager _typeManager;
    private EntitySystemManager _systemManager;
    
    [SetUp]
    public void Setup()
    {
        _typeManager = new TypeManager();
        _typeManager.Init(new List<Assembly>()
        {
            typeof(Entity).Assembly,
            typeof(Unit).Assembly,
        });
        _typeManager.Register();
        
        _systemManager =  new EntitySystemManager();
        _systemManager.Init();
        _systemManager.Register();
    }

    [TearDown]
    public void Clear()
    {
        _systemManager.Dispose();
        _typeManager.Dispose();
    }
    
    [Test]
    public void SerializeSubClass()
    {
        var root = new Root(0, 0, ELogicSceneType.Main, "Tester");
        var unit = root.Scene.AddChild<Unit>();
        unit.HP = 1900;
        
        BsonClassMap.LookupClassMap(typeof(Entity));
        BsonClassMap.LookupClassMap(typeof(Unit));
        
        JsonWriterSettings defaultSettings = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };
        var jsonStr = unit.ToJson(defaultSettings);

        var tester = new TestData();
        var jsStr2 = tester.ToJson(defaultSettings);
        
        Assert.Pass();
    }
    
    
    public class Unit : Entity
    {
        public int HP;

        protected int AGI = 11;
        
        private int MP = 2;
    }
    
    public class TestData
    {
        public int HP;

        protected int AGI = 11;
        [BsonRequired]
        private int MP = 2;
    }
}