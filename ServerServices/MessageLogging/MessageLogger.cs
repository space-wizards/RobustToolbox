using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13.IoC;
using SS13_Shared;
using ServerInterfaces;
using ServerInterfaces.MessageLogging;
using System.Timers;
using ServerInterfaces.Configuration;

namespace ServerServices.MessageLogging
{
    public class MessageLogger : IMessageLogger, IService
    {
        private MessageLoggerServiceClient _loggerServiceClient;
        private bool _logging;
        private static Timer _pingTimer;

        public MessageLogger(IConfigurationManager _configurationManager)
        {
            _logging = _configurationManager.MessageLogging;
            _loggerServiceClient = new MessageLoggerServiceClient("NetNamedPipeBinding_IMessageLoggerService");
            if(_logging)
            {
                Ping();
                _pingTimer = new Timer(5000);
                _pingTimer.Elapsed += CheckServer;
                _pingTimer.Enabled = true;
            }
        }

        public static void CheckServer(object source, ElapsedEventArgs e)
        {
            IoCManager.Resolve<IMessageLogger>().Ping();
        }

        /// <summary>
        /// Check to see if the server is still running 
        /// </summary>
        public void Ping()
        {
            bool failed = false;
            try
            {
                var up = _loggerServiceClient.ServiceStatus();
            }
            catch(System.ServiceModel.CommunicationException e)
            {
                failed = true;
            }
            finally
            {
                if (failed)
                    _logging = false;
            }
        }

        public void LogOutgoingComponentNetMessage(long clientUID, int uid, SS13_Shared.GO.ComponentFamily family, object[] parameters)
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
                _loggerServiceClient.LogServerOutgoingNetMessage(clientUID, uid, (int)family, parameters);
            }
            catch (System.ServiceModel.CommunicationException e)
            {
            }
        }

        public void LogIncomingComponentNetMessage(long clientUID, int uid, SS13_Shared.EntityMessage entityMessage, SS13_Shared.GO.ComponentFamily componentFamily, object[] parameters)
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
                _loggerServiceClient.LogServerIncomingNetMessage(clientUID, uid, (int)entityMessage, (int)componentFamily, parameters);
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
                _loggerServiceClient.LogServerComponentMessage(uid, (int)senderfamily, sendertype, (int)type);
            }
            catch (System.ServiceModel.CommunicationException e)
            {
            }
        }

        public SS13_Shared.ServerServiceType ServiceType
        {
            get { return ServerServiceType.MessageLogger; }
        }
    }
}
