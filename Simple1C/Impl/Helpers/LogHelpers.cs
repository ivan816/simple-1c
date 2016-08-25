using System;
using System.Diagnostics;

namespace Simple1C.Impl.Helpers
{
    internal static class LogHelpers
    {
        public static void LogWithTiming(string description, Action action)
        {
            Console.Out.WriteLine(description);
            var s = Stopwatch.StartNew();
            action();
            s.Stop();
            Console.Out.WriteLine("done, took [{0}] millis", s.ElapsedMilliseconds);
        }
    }
}