using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces.MessageLogging;
using ClientInterfaces.Configuration;

namespace ClientServices.MessageLogging
{
    public class MessageLogger: IMessageLogger
    {
        private MessageLoggerServiceClient _loggerServiceClient;
        private bool _logging;

        public MessageLogger(IConfigurationManager _configurationManager)
        {
            _logging = _configurationManager.GetMessageLogging();
            _loggerServiceClient = new MessageLoggerServiceClient("NetNamedPipeBinding_IMessageLoggerService");
        }

        public void LogOutgoingComponentNetMessage(int uid, SS13_Shared.GO.ComponentFamily family, object[] parameters)
        {
            if (!_logging)
                return;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] is Enum)
                    parameters[i] = (int)parameters[i];
            }
            try
            {
                _loggerServiceClient.LogClientOutgoingNetMessage(uid, (int)family, parameters);
            }
            catch (System.ServiceModel.CommunicationException e)
            {
            }
        }

        public void LogIncomingComponentNetMessage(int uid, SS13_Shared.EntityMessage entityMessage, SS13_Shared.GO.ComponentFamily componentFamily, object[] parameters)
        {
            if (!_logging)
                return;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] is Enum)
                    parameters[i] = (int)parameters[i];
            }
            try
            {
                _loggerServiceClient.LogClientIncomingNetMessage(uid, (int)entityMessage, (int)componentFamily, parameters);
            }
            catch (System.ServiceModel.CommunicationException e)
            {
            }
        }

        public void LogComponentMessage(int uid, SS13_Shared.GO.ComponentFamily senderfamily, string sendertype, SS13_Shared.GO.ComponentMessageType type)
        {
            if (!_logging)
                return;

            try
            {
                _loggerServiceClient.LogClientComponentMessage(uid, (int)senderfamily, sendertype, (int)type);
            }
            catch (System.ServiceModel.CommunicationException e)
            {
            }
        }
    }
}
