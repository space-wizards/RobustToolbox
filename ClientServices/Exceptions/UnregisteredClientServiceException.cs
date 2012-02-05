using System;

namespace ClientServices.Exceptions
{
    public class UnregisteredClientServiceException : Exception
    {
        private readonly Type _serviceType;

        public UnregisteredClientServiceException(Type serviceType)
        {
            _serviceType = serviceType;
        }

        public override string Message
        {
            get
            {
                return String.Format("Service of Type {0} has not been registerd with the Service Manager.", _serviceType);
            }
        }
    }
}
