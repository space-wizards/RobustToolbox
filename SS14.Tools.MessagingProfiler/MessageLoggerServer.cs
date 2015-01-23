using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace SS14.Tools.MessagingProfiler
{
    public class MessageLoggerServer
    {
        private ServiceHost host;

        public MessageLoggerServer()
        { }

        public void Initialize()
        {
            Uri baseAddress = new Uri("net.pipe://MessageLoggerService");

            // Create the ServiceHost.
            host = new ServiceHost(typeof(MessageLoggerService), baseAddress);
            
            // Enable metadata publishing.
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            host.Description.Behaviors.Add(smb);

            var db = host.Description.Behaviors.Find<ServiceDebugBehavior>();
            db.IncludeExceptionDetailInFaults = true;

            host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexNamedPipeBinding(), "");
            host.AddServiceEndpoint(typeof(IMessageLoggerService), new NetNamedPipeBinding(), "log");
        }

        public void Start()
        {
            host.Open();
        }

        public void Stop()
        {
            host.Close();
        }
    }
}
