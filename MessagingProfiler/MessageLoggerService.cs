using System.ServiceModel;
using System;
using SS13_Shared;
using MessagingProfiler;
using System.Collections.Generic;
using SS13_Shared.GO;

namespace MessagingProfiler
{
    [ServiceContract]
    public interface IMessageLoggerService
    {
        [OperationContract]
        void LogServerIncomingNetMessage(long clientUID, int uid, int entityMessageType, int componentFamily, object[] parameters);

        [OperationContract]
        void LogServerOutgoingNetMessage(long clientUID, int uid, int family, object[] parameters);

        [OperationContract]
        void LogClientIncomingNetMessage(int uid, int entityMessageType, int componentFamily, object[] parameters);

        [OperationContract]
        void LogClientOutgoingNetMessage(int uid, int family, object[] parameters);

        //TODO add reply logging
        [OperationContract]
        void LogServerComponentMessage(int senderid, int senderFamily, string senderType, int componentMessageType);

        //[OperationContract]
        //void LogServerComponentReplyMessage(int replierid, int replierFamily, string replierType);

        //TODO add reply logging
        [OperationContract]
        void LogClientComponentMessage(int senderid, int senderFamily, string senderType, int componentMessageType);

        //[OperationContract]
        //void LogClientComponentReplyMessage(int replierid, int replierFamily, string replierType);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
        ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class MessageLoggerService : IMessageLoggerService
    {
        public void LogServerIncomingNetMessage(long clientUID, int uid, int entityMessageType, int componentFamily, object[] parameters)
        {
            var i = new LogItem();
            i.ClientId = clientUID;
            i.EntityId = uid;
            i.EntityMessageType = (EntityMessage)entityMessageType;
            i.ComponentFamily = (ComponentFamily)componentFamily;
            i.Parameters = parameters;
            i.MessageSource = LogItem.LogMessageType.ServerRecievedNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogServerOutgoingNetMessage(long clientUID, int uid, int family, object[] parameters)
        {
            var i = new LogItem();
            i.ClientId = clientUID;
            i.EntityId = uid;
            i.Parameters = parameters;
            i.MessageSource = LogItem.LogMessageType.ServerSentNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogClientIncomingNetMessage(int uid, int entityMessageType, int componentFamily, object[] parameters)
        {
            var i = new LogItem();
            i.EntityId = uid;
            i.EntityMessageType = (EntityMessage)entityMessageType;
            i.ComponentFamily = (ComponentFamily)componentFamily;
            i.Parameters = parameters;
            i.MessageSource = LogItem.LogMessageType.ClientRecievedNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogClientOutgoingNetMessage(int uid, int family, object[] parameters)
        {
            var i = new LogItem();
            i.EntityId = uid;
            i.ComponentFamily = (ComponentFamily)family;
            i.Parameters = parameters;
            i.MessageSource = LogItem.LogMessageType.ClientSentNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogServerComponentMessage(int uid, int senderFamily, string senderType, int componentMessageType)
        {
            var i = new LogItem();
            i.EntityId = uid;
            i.ComponentFamily = (ComponentFamily)senderFamily;
            i.SenderType = senderType;
            i.MessageType = (ComponentMessageType)componentMessageType;
            i.MessageSource = LogItem.LogMessageType.ClientComponentMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogClientComponentMessage(int uid, int senderFamily, string senderType, int componentMessageType)
        {
            var i = new LogItem();
            i.EntityId = uid;
            i.ComponentFamily = (ComponentFamily)senderFamily;
            i.SenderType = senderType;
            i.MessageType = (ComponentMessageType)componentMessageType;
            i.MessageSource = LogItem.LogMessageType.ClientComponentMessage;
            
            LogHolder.Singleton.LogItems.Add(i);
        }

    }
}