using System;
using SS14.Shared.Network;
using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables
{
    public abstract class ViewVariablesSession
    {
        public NetSessionId PlayerSession { get; }
        public object Object { get; }
        public uint SessionId { get; }
        public Type ObjectType { get; }

        protected ViewVariablesSession(NetSessionId playerSession, object o, uint sessionId)
        {
            PlayerSession = playerSession;
            Object = o;
            SessionId = sessionId;
            ObjectType = o.GetType();
        }

        public abstract ViewVariablesBlob DataRequest();
    }
}
