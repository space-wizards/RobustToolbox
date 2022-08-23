using System;

namespace Robust.Shared.Serialization.Manager
{
    [Virtual]
    public class InvalidNodeTypeException : Exception
    {
        public InvalidNodeTypeException()
        {
        }

        public InvalidNodeTypeException(string? message) : base(message)
        {
        }
    }
}
