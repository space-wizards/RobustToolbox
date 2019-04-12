using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Robust.Shared.Log;
using Robust.Shared.Utility;


namespace Robust.Shared.Exceptions
{
    internal sealed class RuntimeLog : IRuntimeLog
    {
        private readonly Dictionary<Type, List<LoggedException>> exceptions = new Dictionary<Type, List<LoggedException>>();

        public void LogException(Exception exception, string catcher=null)
        {
            if (!exceptions.TryGetValue(exception.GetType(), out var list))
            {
                list = new List<LoggedException>();
                exceptions[exception.GetType()] = list;
            }

            list.Add(new LoggedException(exception, DateTime.Now, catcher));

            if (catcher != null)
            {
                Logger.ErrorS("runtime", "Caught exception in {0}: {1}", catcher, exception);
            }
            else
            {
                Logger.ErrorS("runtime", "Caught exception: {0}", exception);
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
                        foreach (var x in e.Data.Keys)
                        {
                            ret.AppendLine($"{x}: {e.Data[x]}");
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
            public string Catcher { get; }

            public LoggedException(Exception exception, DateTime time, string catcher)
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
        void LogException(Exception exception, string catcher=null);

        string Display();
    }
}
