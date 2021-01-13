using System;
using System.Runtime.Serialization;

namespace Robust.Shared.ContentPack
{
    [Serializable]
    public class TypeCheckFailedException : Exception
    {
        public TypeCheckFailedException()
        {
        }

        public TypeCheckFailedException(string message) : base(message)
        {
        }

        public TypeCheckFailedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected TypeCheckFailedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
