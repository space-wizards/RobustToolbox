using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces.MessageLogging;

namespace ClientServices.MessageLogging
{
    public class MessageLogger: IMessageLogger
    {
        private MessageLoggerServiceClient _loggerServiceClient;

        public MessageLogger()
        {
            _loggerServiceClient = new MessageLoggerServiceClient("NetNamedPipeBinding_IMessageLoggerService");
        }

        public void LogOutgoingComponentNetMessage(int uid, SS13_Shared.GO.ComponentFamily family, object[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] is Enum)
                    parameters[i] = (int)parameters[i];
            }
            _loggerServiceClient.LogClientOutgoingNetMessage(uid, (int)family, parameters);
        }

        public void LogIncomingComponentNetMessage(int uid, SS13_Shared.EntityMessage entityMessage, SS13_Shared.GO.ComponentFamily componentFamily, object[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] is Enum)
                    parameters[i] = (int)parameters[i];
            }
            _loggerServiceClient.LogClientIncomingNetMessage(uid, (int)entityMessage, (int)componentFamily, parameters);
        }

        public void LogComponentMessage(int uid, SS13_Shared.GO.ComponentFamily senderfamily, string sendertype, SS13_Shared.GO.ComponentMessageType type)
        {
            _loggerServiceClient.LogClientComponentMessage(uid, (int)senderfamily, sendertype, (int)type);
        }
    }
}
