using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SS14.Shared.Exceptions
{
    public struct ExceptionAndTime
    {
        public Exception exp { get; set; }
        public DateTime time { get; set; }
    }
    public class RuntimeLog : IRuntimeLog
    {
        private Dictionary<Type, List<ExceptionAndTime>> exceptions = new Dictionary<Type, List<ExceptionAndTime>>();

        public RuntimeLog()
        {
        }

        public void AddException(Exception E, DateTime D)
        {
            if (!exceptions.TryGetValue(E.GetType(), out var list))
            {
                list = new List<ExceptionAndTime>();
                exceptions[E.GetType()] = list;
            }

            list.Add(new ExceptionAndTime
            {
                exp = E,
                time = D,
            });

        }
        public string Display()
        {
            StringBuilder ret = new StringBuilder();
            foreach (Type T in exceptions.Keys)
            {
                ret.AppendLine($"{exceptions[T].Count()} exception {((exceptions[T].Count() > 1) ? "s" : "")} {T}");
                foreach (ExceptionAndTime EandD in exceptions[T])
                {
                    Exception E = EandD.exp;
                    DateTime D = EandD.time;
                    ret.AppendLine($"Exception in {E.TargetSite}, at {D.ToString()}:");
                    ret.AppendLine($"Message: {E.Message}");
                    ret.AppendLine($"Stack trace: {E.StackTrace}");
                    if (E.Data.Count > 0)
                    {
                        ret.AppendLine("Additional data:");
                        foreach (Object x in E.Data.Keys)
                        {
                            ret.AppendLine($"{x}: {E.Data[x]}");
                        }
                    }
                }
            }
            return ret.ToString();
        }
    }
    public interface IRuntimeLog
    {
        void AddException(Exception E, DateTime D);
        string Display();
    }
}
