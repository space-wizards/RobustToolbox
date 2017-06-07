using System;

namespace SS14.Shared.IoC.Exceptions
{
    /// <summary>
    /// An exception for when a type doesn't correctly implement an interface, but is still IoC accessible.
    /// Such as missing an attribute.
    /// </summary>
    public class InvalidImplementationException : Exception
    {
        private readonly string message;
        private readonly Type type;
        private readonly Type parent;

        /// <param name="type">The IoC target incorrectly implementing something.</param>
        /// <param name="parent">The IoC interface incorrectly being implemented.</param>
        /// <param name="message">Additionally info.</param>
        public InvalidImplementationException(Type type, Type parent, string message)
        {
            this.type = type;
            this.parent = parent;
            this.message = message;
        }

        public override string Message => string.Format("{0} incorrectly implements {1}: {2}", type, parent, message);
    }
}
