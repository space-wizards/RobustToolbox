using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.IoC.Exceptions
{
    /// <summary>
    /// Signifies that a type threw an exception from its constructor while IoC was trying to build it.
    /// </summary>
    class ImplementationConstructorException : Exception
    {
        public readonly Type type;

        public ImplementationConstructorException(Type type, Exception inner)
            : base(string.Format("{0} threw an exception inside its constructor."), inner)
        {
            this.type = type;
        }
    }
}
