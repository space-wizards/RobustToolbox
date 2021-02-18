using System;
using System.Runtime.Serialization;

namespace Robust.Shared.Exceptions
{
    /// <summary>
    /// Thrown if a method is called with an invalid type argument.
    /// For example <see cref="IoC.IoCManager.Register{TInterface, TImplementation}(bool)"/> with an abstract <code>TImplementation</code>.
    /// </summary>
    [Serializable]
    public sealed class TypeArgumentException : Exception
    {
        /// <summary>
        /// The name of the type argument that had invalid data.
        /// </summary>
        public readonly string? TypeArgumentName;

        public TypeArgumentException()
        {
        }
        public TypeArgumentException(string message) : base(message)
        {
        }
        public TypeArgumentException(string message, Exception inner) : base(message, inner)
        {
        }
        public TypeArgumentException(string message, string name) : base(message)
        {
            TypeArgumentName = name;
        }
        public TypeArgumentException(string message, string name, Exception inner) : base(message, inner)
        {
            TypeArgumentName = name;
        }

        private TypeArgumentException(
          SerializationInfo info,
          StreamingContext context) : base(info, context)
        {
            TypeArgumentName = info.GetString("TypeArgumentName");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("TypeArgumentName", TypeArgumentName);

            base.GetObjectData(info, context);
        }
    }
}
