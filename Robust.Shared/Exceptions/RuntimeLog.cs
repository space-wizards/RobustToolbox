using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Robust.Shared.Log;
using Robust.Shared.Utility;


namespace Robust.Shared.Exceptions
{
    internal sealed class RuntimeLog : IRuntimeLog
    {
        private readonly Dictionary<Type, List<LoggedException>> exceptions = new();

        public int ExceptionCount => exceptions.Values.Sum(l => l.Count);

        public void LogException(Exception exception, string? catcher=null)
        {
            if (!exceptions.TryGetValue(exception.GetType(), out var list))
            {
                list = new List<LoggedException>();
                exceptions[exception.GetType()] = list;
            }

            list.Add(new LoggedException(exception, DateTime.Now, catcher));

            if (catcher != null)
            {
                Logger.ErrorS("runtime", exception, "Caught exception in {Catcher}", catcher);
            }
            else
            {
                Logger.ErrorS("runtime", exception, "Caught exception");
            }
        }

        public string Display()
        {
            var ret = new StringBuilder();
            foreach (var (type, list) in exceptions)
            {
                ret.AppendLine($"{list.Count} exception {(exceptions[type].Count > 1 ? "s" : "")} {type}");
                foreach (var logged in list)
                {
                    var e = logged.Exception;
                    var t = logged.Time;
                    ret.AppendLine($"Exception in {e.TargetSite}, at {t.ToString(CultureInfo.InvariantCulture)}:");
                    ret.AppendLine($"Message: {e.Message}");
                    ret.AppendLine($"Catcher: {logged.Catcher}");
                    ret.AppendLine($"Stack trace: {e.StackTrace}");
                    if (e.Data.Count > 0)
                    {
                        ret.AppendLine("Additional data:");
                        foreach (var x in e.Data)
                        {
                            if (x is DictionaryEntry entry)
                            {
                                ret.AppendLine($"{entry.Key}: {entry.Value}");
                            }
                        }
                    }
                }
            }
            return ret.ToString();
        }

        private class LoggedException
        {
            public Exception Exception { get; }
            public DateTime Time { get; }
            public string? Catcher { get; }

            public LoggedException(Exception exception, DateTime time, string? catcher)
            {
                Exception = exception;
                Time = time;
                Catcher = catcher;
            }
        }
    }

    /// <summary>
    ///     The runtime log is responsible for the logging of exceptions, to prevent the game crashing entirely.
    /// </summary>
    /// <remarks>
    ///     The term "runtime" dates back to BYOND, in which an exception is called a "runtime error".
    ///     As such, what we call exceptions is called a "runtime" in BYOND.
    /// </remarks>
    public interface IRuntimeLog
    {
        int ExceptionCount { get; }

        void LogException(Exception exception, string? catcher=null);

        string Display();
    }
}
