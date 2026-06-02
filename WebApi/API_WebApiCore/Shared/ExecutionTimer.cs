using System;
using System.Diagnostics;

public static class ExecutionTimer
{
    public static (TimeSpan elapsed, T result) Measure<T>(Func<T> func)
    {
        var sw = Stopwatch.StartNew();
        T result = func();
        sw.Stop();
        return (sw.Elapsed, result);
    }
}