using BenchmarkDotNet.Attributes;
using Bliss.Base.Collections;

namespace BlissBenchMark;

/// <summary>Measures keyed IndexList removal from identically populated production lists.</summary>
[MemoryDiagnoser]
public class IndexListRemovalBenchmark
{
    private const int SMALL_COUNT = 100;
    private const int LARGE_COUNT = 1000;
    private const int OPERATIONS_PER_INVOKE = 64;
    private const int FIRST_KEY = 0;
    private const int SINGLE_ITEM = 1;
    private const int MIDPOINT_DIVISOR = 2;
    private const long REINSERTED_VALUE = -1;
    private readonly RemovalList[] lists = new RemovalList[OPERATIONS_PER_INVOKE];
    private int removedKey;

    /// <summary>Number of elements in each list before removal.</summary>
    [Params(SMALL_COUNT, LARGE_COUNT)]
    public int ElementCount { get; set; }

    /// <summary>Position of the removed key.</summary>
    [Params(ERemovalPosition.First, ERemovalPosition.Middle, ERemovalPosition.Last)]
    public ERemovalPosition Position { get; set; }

    /// <summary>Populates independent lists outside the measured operation.</summary>
    [IterationSetup]
    public void Prepare()
    {
        this.removedKey = this.Position switch
        {
            ERemovalPosition.First => FIRST_KEY,
            ERemovalPosition.Middle => this.ElementCount / MIDPOINT_DIVISOR,
            ERemovalPosition.Last => this.ElementCount - SINGLE_ITEM,
            _ => throw new ArgumentOutOfRangeException(nameof(this.Position)),
        };
        for (int index = 0; index < this.lists.Length; index++)
        {
            RemovalList list = new RemovalList();
            for (int key = 0; key < this.ElementCount; key++)
            {
                list.Values.Add(key, key);
            }
            this.lists[index] = list;
        }
    }

    /// <summary>Removes one key from each prepared list.</summary>
    [Benchmark(OperationsPerInvoke = OPERATIONS_PER_INVOKE)]
    public void RemoveKey()
    {
        for (int index = 0; index < this.lists.Length; index++)
        {
            this.lists[index].Values.Remove(this.removedKey);
        }
    }

    /// <summary>Checks that every measured removal removed the key mapping.</summary>
    [IterationCleanup]
    public void Verify()
    {
        foreach (RemovalList list in this.lists)
        {
            int count = list.Values.Count;
            list.Values.Add(this.removedKey, REINSERTED_VALUE);
            if (list.Values.Count != count + SINGLE_ITEM)
            {
                throw new InvalidOperationException("The measured removal did not remove the key mapping.");
            }
        }
    }

    /// <summary>Position of the key removed by the benchmark.</summary>
    public enum ERemovalPosition
    {
        /// <summary>Remove the first key.</summary>
        First,
        /// <summary>Remove a middle key.</summary>
        Middle,
        /// <summary>Remove the final key.</summary>
        Last,
    }

    private sealed class RemovalList
    {
        public readonly IndexList<int, long> Values = new IndexList<int, long>();
    }
}
