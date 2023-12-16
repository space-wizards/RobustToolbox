using System;
using System.Runtime.Serialization;

namespace Robust.Shared.IoC.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="IoCManager.Resolve{T}()"/> if one attempts to resolve an interface that isn't registered.
    /// </summary>
    [Serializable]
    [Virtual]
    public class UnregisteredTypeException : Exception
    {
        /// <summary>
        /// The actual type that was attempted to be resolved, but wasn't registered. This is the <see cref="Type.AssemblyQualifiedName"/>.
        /// </summary>
        public readonly string? TypeName;

        public UnregisteredTypeException(Type type) : base($"Attempted to resolve unregistered type: {type}")
        {
            TypeName = type.AssemblyQualifiedName;
        }
    }
}
