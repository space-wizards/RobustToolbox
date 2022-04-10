using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Robust.Client.Graphics.Clyde
{
    [Serializable]
    [PublicAPI]
    [Virtual]
    internal class ShaderCompilationException : Exception
    {
        public ShaderCompilationException()
        {
        }

        public ShaderCompilationException(string message) : base(message)
        {
        }

        public ShaderCompilationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ShaderCompilationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
