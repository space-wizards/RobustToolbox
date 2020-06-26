using System;
using System.Runtime.Serialization;

namespace Robust.Shared.IoC.Exceptions
{
    /// <summary>
    /// Signifies that a type threw an exception from its constructor while IoC was trying to build it.
    /// </summary>
    [Serializable]
    public class ImplementationConstructorException : Exception
    {
        /// <summary>
        /// The <see cref="Type.AssemblyQualifiedName" /> of the type that threw the exception inside its constructor.
        /// </summary>
        public readonly string? typeName;

        public ImplementationConstructorException(Type type, Exception? inner)
            : base($"{type} threw an exception inside its constructor.", inner)
        {
            typeName = type.AssemblyQualifiedName;
        }

        protected ImplementationConstructorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            typeName = info.GetString("typeName");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("typeName", typeName);
        }
    }
}
