using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.IoC.Exceptions
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
        public readonly string typeName;

        public ImplementationConstructorException(Type type, Exception inner)
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

    [System.Serializable]
    public class MyException : System.Exception
    {
        public MyException() { }
        public MyException( string message ) : base( message ) { }
        public MyException( string message, System.Exception inner ) : base( message, inner ) { }
        protected MyException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context ) : base( info, context ) { }
    }
}
