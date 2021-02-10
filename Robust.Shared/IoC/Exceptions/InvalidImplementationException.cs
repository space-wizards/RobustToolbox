using System;
using Robust.Shared.Reflection;

namespace Robust.Shared.IoC.Exceptions
{
    /// <summary>
    /// An exception for when a type doesn't correctly implement an interface, but is still IoC or reflection accessible..
    /// Such as missing an attribute.
    /// </summary>
    /// <seealso cref="IoCManager" />
    /// <seealso cref="IReflectionManager" />
    public class InvalidImplementationException : Exception
    {
        private readonly string message;
        private readonly Type type;
        private readonly Type parent;

        /// <param name="type">The implementation incorrectly implementing something.</param>
        /// <param name="parent">The interface incorrectly being implemented.</param>
        /// <param name="message">Additionally info.</param>
        public InvalidImplementationException(Type type, Type parent, string message)
        {
            this.type = type;
            this.parent = parent;
            this.message = message;
        }

        public override string Message => $"{type} incorrectly implements {parent}: {message}";
    }
}
