using System;

namespace Robust.Shared.Serialization
{
    public class InvalidMappingException : Exception
    {

        public InvalidMappingException(string msg) : base(msg)
        {

        }
    }
}
