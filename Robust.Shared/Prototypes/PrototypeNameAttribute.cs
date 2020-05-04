using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Shared.Prototypes
{
    /// <summary>
    /// A metadata attribute which marks an argument as being a prototype name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class PrototypeNameAttribute : Attribute
    {
        public PrototypeNameAttribute(string _prototypeType) { }
    }
}
