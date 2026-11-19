using Bliss.Base.Collections;

namespace Bliss.Tests;

/// <summary>Protects keyed, indexed and value removal and subsequent reuse of IndexList.</summary>
public class IndexListTests
{
    private const int FIRST_KEY = 10;
    private const int MIDDLE_KEY = 20;
    private const int LAST_KEY = 30;
    private const int MISSING_KEY = 40;
    private const int FIRST_INDEX = 0;
    private const int MIDDLE_INDEX = 1;
    private const int LAST_INDEX = 2;
    private const int SINGLE_ITEM = 1;
    private const string FIRST_VALUE = "first";
    private const string MIDDLE_VALUE = "middle";
    private const string LAST_VALUE = "last";
    private const string MISSING_VALUE = "missing";
    private const string REPLACEMENT_VALUE = "replacement";

    /// <summary>Removal swaps the last element into the removed slot and preserves other keys.</summary>
    [TestCase(FIRST_KEY, new[] { LAST_VALUE, MIDDLE_VALUE })]
    [TestCase(MIDDLE_KEY, new[] { FIRST_VALUE, LAST_VALUE })]
    [TestCase(LAST_KEY, new[] { FIRST_VALUE, MIDDLE_VALUE })]
    public void RemoveKeyShrinksAndPreservesRemainingKeys(int key, string[] expected)
    {
        IndexList<int, string> list = createList();
        list.Remove(key);
        Assert.That(list, Is.EqualTo(expected));

        list.Remove(key);
        Assert.That(list, Is.EqualTo(expected));
        list.Add(key, REPLACEMENT_VALUE);
        Assert.That(list, Has.Count.EqualTo(expected.Length + SINGLE_ITEM));
        list.Remove(key);
        Assert.That(list, Is.EqualTo(expected));

        list.Remove(FIRST_KEY);
        list.Remove(MIDDLE_KEY);
        list.Remove(LAST_KEY);
        Assert.That(list, Is.Empty);
    }

    /// <summary>Indexed removal has the same keyed swap-and-pop behavior.</summary>
    [TestCase(FIRST_INDEX, new[] { LAST_VALUE, MIDDLE_VALUE })]
    [TestCase(MIDDLE_INDEX, new[] { FIRST_VALUE, LAST_VALUE })]
    [TestCase(LAST_INDEX, new[] { FIRST_VALUE, MIDDLE_VALUE })]
    public void RemoveAtShrinksAndPreservesKeys(int index, string[] expected)
    {
        IndexList<int, string> list = createList();
        list.RemoveAt(index);
        Assert.That(list, Is.EqualTo(expected));
        list.Remove(FIRST_KEY);
        list.Remove(MIDDLE_KEY);
        list.Remove(LAST_KEY);
        Assert.That(list, Is.Empty);
    }

    /// <summary>Removal by value updates the key mapping and ignores an absent value.</summary>
    [Test]
    public void RemoveValuePreservesKeyMapping()
    {
        IndexList<int, string> list = createList();
        list.Remove(MISSING_VALUE);
        Assert.That(list, Has.Count.EqualTo(LAST_INDEX + SINGLE_ITEM));
        list.Remove(MIDDLE_VALUE);
        Assert.That(list, Is.EqualTo(new[] { FIRST_VALUE, LAST_VALUE }));
        list.Remove(LAST_KEY);
        Assert.That(list, Is.EqualTo(new[] { FIRST_VALUE }));
        list.Remove(FIRST_VALUE);
        Assert.That(list, Is.Empty);
    }

    /// <summary>Absent keys and clearing the list do not prevent subsequent reuse.</summary>
    [Test]
    public void MissingKeyAndClearAllowReuse()
    {
        IndexList<int, string> list = createList();
        list.Remove(MISSING_KEY);
        Assert.That(list, Has.Count.EqualTo(LAST_INDEX + SINGLE_ITEM));
        list.Clear();
        list.Add(FIRST_KEY, FIRST_VALUE);
        list.Remove(FIRST_KEY);
        Assert.That(list, Is.Empty);
    }

    private static IndexList<int, string> createList()
    {
        IndexList<int, string> list = new IndexList<int, string>();
        list.Add(FIRST_KEY, FIRST_VALUE);
        list.Add(MIDDLE_KEY, MIDDLE_VALUE);
        list.Add(LAST_KEY, LAST_VALUE);
        return list;
    }
}
