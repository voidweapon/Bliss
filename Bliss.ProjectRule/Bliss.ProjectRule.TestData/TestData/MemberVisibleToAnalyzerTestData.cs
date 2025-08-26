using Bliss;

namespace TestProject1.TestData.MemberVisibleToAnalyzer;


public interface ITestMember
{
    int HP { get; set; }
}

public class TestMember : ITestMember
{
    [Bliss.MemberVisibleTo(typeof(ConsumerA), MemberVisibleToAttribute.EAccess.Get)]
    public int HP { get; set; }

    [Bliss.MemberVisibleTo(typeof(ConsumerA), MemberVisibleToAttribute.EAccess.Set)]
    public int MP;

    public void Run()
    {
        int h = HP;
        int test = (1 + 2 + HP + h);
        HP = 1;
        
        int mp = MP;
        MP = 2 + 3;
    }

    public void TestMethod()
    {
        Run();
    }
}

public class ConsumerA
{
    public void Run()
    {
        var a = new TestMember();
        int hp = a.HP;
        a.HP = 1 + 2;
        
        int mp = a.MP;
        a.MP = 2 + 3;
    }
}

public class ConsumerB
{
    public void Run()
    {
        // var a = new TestMember();
        // int hp = a.HP;
        // a.HP = 1 + 2;
        //
        // int mp = a.MP;
        // a.MP = 2 + 3;
    }
}