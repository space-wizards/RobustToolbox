using System;
using System.Runtime.Serialization;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Thrown if an entity fails to be created due to an exception inside a component.
    /// </summary>
    /// <remarks>
    ///     See the <see cref="Exception.InnerException"/> for the actual exception.
    /// </remarks>
    [Serializable]
    [Virtual]
    public class EntityCreationException : Exception
    {
        public EntityCreationException()
        {
        }

        public EntityCreationException(string message) : base(message)
        {
        }

        public EntityCreationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected EntityCreationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
