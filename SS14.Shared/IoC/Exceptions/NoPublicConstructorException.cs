using System;

namespace SS14.Shared.IoC.Exceptions
{
    public class NoPublicConstructorException : Exception
    {
        private readonly Type _type;

        public NoPublicConstructorException(Type type)
        {
            _type = type;
        }

        public override string Message
        {
            get { return String.Format("Type {0} does not have a public constructor.", _type); }
        }
    }
}
