// See https://aka.ms/new-console-template for more information

using TestProject1.TestData.MemberVisibleToAnalyzer;



Console.WriteLine("Hello, World!");

// int value = 44;
// int index = GetIndex(value);
// int size = GetMaxSize(index);
// Console.WriteLine($"Value={value}, index={index}, Size={size}");


int aa = int.MinValue;
uint id = (uint)Interlocked.Add(ref aa, 1);
Console.WriteLine($"gen:{aa} id:{id}");


int[][]  test = new int[3][];

test[0] = new int[] { 1, 2 };
test[1] = new int[] { 3 };
test[2] = new int[] { 4 };
Console.WriteLine($"{typeof(List<int>).Name}");

Console.WriteLine($"{Diff(1, 16383)}");

uint vvv = unchecked((uint)-1);
int testInt = (int)vvv;
int testCastInt = unchecked((int) vvv);

Console.WriteLine($"{testInt} --- {testCastInt}");


static int Diff(uint a, uint b)
{
    int shiftValue = sizeof(Int32) * 8 - 14;
    return (int)((a - b) << shiftValue) >> shiftValue;
}

static int GetIndex(int value)
{
    return (int)Math.Log((uint)value - 1 | 15, 2) - 3;
}

static int GetMaxSize(int index)
{
    return 16 << index;
}