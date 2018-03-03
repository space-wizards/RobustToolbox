using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.AI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    class AiLogicProcessorAttribute : Attribute
    {
        public string SerializeName { get; }

        public AiLogicProcessorAttribute(string serializeName)
        {
            SerializeName = serializeName;
        }
    }
}
