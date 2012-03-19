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

    public class MessageLoggerService : IMessageLoggerService
    {
        public void LogServerIncomingNetMessage(long clientUID, int uid, int entityMessageType, int componentFamily, object[] parameters)
        {
            LogItem i = new LogItem();
            i.clientID = clientUID;
            i.entityID = uid;
            i.entityMessageType = (EntityMessage)entityMessageType;
            i.componentFamily = (ComponentFamily)componentFamily;
            i.parameters = parameters;
            i.logMessageType = LogItem.LogMessageType.ServerRecievedNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogServerOutgoingNetMessage(long clientUID, int uid, int family, object[] parameters)
        {
            LogItem i = new LogItem();
            i.clientID = clientUID;
            i.entityID = uid;
            i.parameters = parameters;
            i.logMessageType = LogItem.LogMessageType.ServerSentNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogClientIncomingNetMessage(int uid, int entityMessageType, int componentFamily, object[] parameters)
        {
            LogItem i = new LogItem();
            i.entityID = uid;
            i.entityMessageType = (EntityMessage)entityMessageType;
            i.componentFamily = (ComponentFamily)componentFamily;
            i.parameters = parameters;
            i.logMessageType = LogItem.LogMessageType.ClientRecievedNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogClientOutgoingNetMessage(int uid, int family, object[] parameters)
        {
            LogItem i = new LogItem();
            i.entityID = uid;
            i.componentFamily = (ComponentFamily)family;
            i.parameters = parameters;
            i.logMessageType = LogItem.LogMessageType.ClientSentNetMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogServerComponentMessage(int uid, int senderFamily, string senderType, int componentMessageType)
        {
            LogItem i = new LogItem();
            i.entityID = uid;
            i.componentFamily = (ComponentFamily)senderFamily;
            i.senderType = senderType;
            i.messageType = (ComponentMessageType)componentMessageType;
            i.logMessageType = LogItem.LogMessageType.ClientComponentMessage;
            LogHolder.Singleton.LogItems.Add(i);
        }

        public void LogClientComponentMessage(int uid, int senderFamily, string senderType, int componentMessageType)
        {
            LogItem i = new LogItem();
            i.entityID = uid;
            i.componentFamily = (ComponentFamily)senderFamily;
            i.senderType = senderType;
            i.messageType = (ComponentMessageType)componentMessageType;
            i.logMessageType = LogItem.LogMessageType.ClientComponentMessage;
            
            LogHolder.Singleton.LogItems.Add(i);
        }

    }
}