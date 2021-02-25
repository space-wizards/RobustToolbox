using System;

namespace Robust.Shared.Serialization.Manager
{
    public class InvalidNodeTypeException : Exception
    {
        public InvalidNodeTypeException() : base()
        {
        }

        public InvalidNodeTypeException(string? message) : base(message)
        {
        }
    }
}
