// See https://aka.ms/new-console-template for more information

using TestProject1.TestData.MemberVisibleToAnalyzer;

Console.WriteLine("Hello, World!");

int[][]  test = new int[3][];

test[0] = new int[] { 1, 2 };
test[1] = new int[] { 3 };
test[2] = new int[] { 4 };
Console.WriteLine($"{typeof(List<int>).Name}");


