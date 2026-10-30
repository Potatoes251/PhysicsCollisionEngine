using System.Collections.Specialized;
using System.Diagnostics;

public class Benchmark
{
    private readonly string name;
    private double total;
    private double min = double.MaxValue;
    private double max;
    public int count;
    public int countToPrint = 500;

    private double average => total / count;

    public Benchmark(string name)
    {
        this.name = name;
    }

    public void Add(double value)
    {
        total += value; 
        if (value < min)
            min = value;
        else if (value > max)
            max = value;

        if (++count == countToPrint)
        {
            UnityEngine.Debug.Log($"{name} average : {average}, min : {min}, max : {max}");
            Reset();
        }
    }

    public void Reset()
    {
        count = 0;
        total = 0;
        min = double.MaxValue;
    }
}
