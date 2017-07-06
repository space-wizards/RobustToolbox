using SS14.Server.Interfaces.MessageLogging;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using System;
using System.ServiceModel;
using System.Timers;

namespace SS14.Server.MessageLogging
{
    public class MessageLogger : IMessageLogger, IPostInjectInit
    {
        [Dependency]
        private readonly IConfigurationManager _configurationManager;
        private Timer _pingTimer;
        private MessageLoggerServiceClient _loggerServiceClient;
        private bool _logging = false;

        public void PostInject()
        {
            _configurationManager.RegisterCVar("log.enabled", false);
        }

        public void Initialize()
        {
            _logging = _configurationManager.GetCVar<bool>("log.enabled");

            if (_logging)
            {
                _loggerServiceClient = new MessageLoggerServiceClient("NetNamedPipeBinding_IMessageLoggerService");
                Ping();
                _pingTimer = new Timer(5000);
                _pingTimer.Elapsed += CheckServer;
                _pingTimer.Enabled = true;
            }
        }

        #region IMessageLogger Members

        /// <summary>
        /// Check to see if the server is still running
        /// </summary>
        public void Ping()
        {
            bool failed = false;
            try
            {
                bool up = _loggerServiceClient.ServiceStatus();
            }
            catch (CommunicationException)
            {
                failed = true;
            }
            finally
            {
                if (failed)
                    _logging = false;
            }
        }

        public void LogOutgoingComponentNetMessage(long clientUID, int uid, ComponentFamily family, object[] parameters)
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
            catch (CommunicationException)
            {
            }
        }

        public void LogIncomingComponentNetMessage(long clientUID, int uid, EntityMessage entityMessage,
                                                   ComponentFamily componentFamily, object[] parameters)
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
                _loggerServiceClient.LogServerIncomingNetMessage(clientUID, uid, (int)entityMessage,
                                                                 (int)componentFamily, parameters);
            }
            catch (CommunicationException)
            {
            }
        }

        public void LogComponentMessage(int uid, ComponentFamily senderfamily, string sendertype,
                                        ComponentMessageType type)
        {
            if (!_logging)
                return;

            try
            {
                _loggerServiceClient.LogServerComponentMessage(uid, (int)senderfamily, sendertype, (int)type);
            }
            catch (CommunicationException)
            {
            }
        }

        #endregion IMessageLogger Members

        public static void CheckServer(object source, ElapsedEventArgs e)
        {
            IoCManager.Resolve<IMessageLogger>().Ping();
        }
    }
}
