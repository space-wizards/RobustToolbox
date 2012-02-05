using System;

namespace ClientServices.Exceptions
{
    public class ExistingClientServiceException : Exception
    {
        private readonly Type _serviceType;

        public ExistingClientServiceException(Type serviceType)
        {
            _serviceType = serviceType;
        }

        public override string Message
        {
            get
            {
                return String.Format("Service of Type {0} has already been registerd with the Service Manager.", _serviceType);
            }
        }
    }
}
