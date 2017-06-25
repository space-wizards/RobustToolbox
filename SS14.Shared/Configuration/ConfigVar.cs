using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Configuration
{
    internal class ConfigVar
    {
        public string Name { get; set; }
        public object DefaultValue { get; set; }
        public CVarFlags Flags { get; set; }
        public object Value { get; set; }
        public bool Registered { get; set; }
        
        public ConfigVar(string name, object defaultValue, CVarFlags flags)
        {
            Name = name;
            DefaultValue = defaultValue;
            Flags = flags;
        }
    }
}
