using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Exceptions
{
    class RuntimeLog
    {
        private List<Exception> exceptions { get; set; }
        private Dictionary<Type, int> exp_count { get; set; }
        public void AddException(Exception E)
        {
            exceptions.Add(E);
            exp_count.Add(E.GetType(), exp_count[E.GetType()] + 1);
        }
        public string Display()
        {
            var ret = "";
            foreach (Exception E in exceptions)
            {
                ret += "Exception in " + E.Source.ToString() + ", " + E.TargetSite.ToString() + ". \n";
                ret += "Message: " + E.Message + "\n";
                ret += "Stack Trace: " + E.StackTrace + "\n";
                if (E.Data.Count > 0)
                {
                    ret += "Additional data:";
                    foreach (Object x in E.Data)
                    {
                        ret += x.ToString() + ": " + E.Data[x].ToString();
                    }
                }
            }
            return ret;
        }
    }
}
