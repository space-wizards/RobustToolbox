using System;

namespace Robust.Shared.Serialization
{
    [Virtual]
    public class InvalidMappingException : Exception
    {

        public InvalidMappingException(string msg) : base(msg)
        {

        }
    }
}
