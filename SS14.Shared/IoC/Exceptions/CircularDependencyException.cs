using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.IoC.Exceptions
{
    public class CircularDependencyException : Exception
    {
        private readonly Type type;
        // TODO: More detailed saying of how the object graph is screwed up.
        public override string Message => string.Format("The type {0} is in a circular dependency reference.", type);

        public CircularDependencyException(Type type)
        {
            this.type = type;
        }
    }
}
