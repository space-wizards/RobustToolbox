using System;
using System.Collections.Generic;
using System.Reflection;
using SS14.Server.Console;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Shared.ViewVariables;
using DenyReason = SS14.Shared.Network.Messages.MsgViewVariablesDenySession.DenyReason;

namespace SS14.Server.ViewVariables
{
    internal class ViewVariablesHost : IViewVariablesHost
    {
        [Dependency] private readonly INetManager _netManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IConGroupController _groupController;

        private readonly Dictionary<uint, ViewVariablesSession>
            _sessions = new Dictionary<uint, ViewVariablesSession>();

        private uint _nextSessionId = 1;

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgViewVariablesReqSession>(MsgViewVariablesReqSession.NAME,
                _msgReqSession);
            _netManager.RegisterNetMessage<MsgViewVariablesReqData>(MsgViewVariablesReqData.NAME, _msgReqData);
            _netManager.RegisterNetMessage<MsgViewVariablesModifyRemote>(MsgViewVariablesModifyRemote.NAME,
                _msgModifyRemote);
            _netManager.RegisterNetMessage<MsgViewVariablesCloseSession>(MsgViewVariablesCloseSession.NAME,
                _msgCloseSession);
            _netManager.RegisterNetMessage<MsgViewVariablesDenySession>(MsgViewVariablesDenySession.NAME);
            _netManager.RegisterNetMessage<MsgViewVariablesOpenSession>(MsgViewVariablesOpenSession.NAME);
            _netManager.RegisterNetMessage<MsgViewVariablesRemoteData>(MsgViewVariablesRemoteData.NAME);
        }

        private void _msgCloseSession(MsgViewVariablesCloseSession message)
        {
            if (!_sessions.TryGetValue(message.SessionId, out var session)
                || session.PlayerSession != message.MsgChannel.SessionId)
            {
                // TODO: logging?
                return;
            }

            _closeSession(message.SessionId, true);
        }

        private void _msgModifyRemote(MsgViewVariablesModifyRemote message)
        {
            if (!_sessions.TryGetValue(message.SessionId, out var session)
                || session.PlayerSession != message.MsgChannel.SessionId)
            {
                // TODO: logging?
                return;
            }

            var property = session.ObjectType.GetProperty(message.PropertyName);
            if (property == null)
            {
                // TODO: logging?
                return;
            }

            var attr = property.GetCustomAttribute<ViewVariablesAttribute>();
            if (attr == null || attr.Access != VVAccess.ReadWrite)
            {
                // TODO: logging?
                return;
            }

            try
            {
                property.SetValue(session.Object, message.Value);
            }
            catch (Exception e)
            {
                Logger.ErrorS("vv", "Exception while modifying property {0} on session {1} object {2}: {3}",
                    property.Name, session.SessionId, session.Object, e);
            }
        }

        private void _msgReqData(MsgViewVariablesReqData message)
        {
            if (!_sessions.TryGetValue(message.SessionId, out var session)
                || session.PlayerSession != message.MsgChannel.SessionId)
            {
                // TODO: logging?
                return;
            }

            var blob = session.DataRequest();
            var dataMsg = _netManager.CreateNetMessage<MsgViewVariablesRemoteData>();
            dataMsg.SessionId = message.SessionId;
            dataMsg.Blob = blob;
            _netManager.ServerSendMessage(dataMsg, message.MsgChannel);
        }

        private void _msgReqSession(MsgViewVariablesReqSession message)
        {
            void Deny(DenyReason reason)
            {
                var denyMsg = _netManager.CreateNetMessage<MsgViewVariablesDenySession>();
                denyMsg.ReqId = message.ReqId;
                denyMsg.Reason = reason;
                _netManager.ServerSendMessage(denyMsg, message.MsgChannel);
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            if (!_groupController.CanViewVar(player))
            {
                Deny(DenyReason.NoAccess);
                return;
            }

            object theObject;

            switch (message.Selector)
            {
                case ViewVariablesComponentSelector componentSelector:
                    var compType = Type.GetType(componentSelector.ComponentType);
                    if (compType == null ||
                        !_componentManager.TryGetComponent(componentSelector.Entity, compType, out var component))
                    {
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    theObject = component;
                    break;
                case ViewVariablesEntitySelector entitySelector:
                {
                    if (!_entityManager.TryGetEntity(entitySelector.Entity, out var entity))
                    {
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    theObject = entity;
                    break;
                }
                case ViewVariablesSessionRelativeSelector sessionRelativeSelector:
                    if (!_sessions.TryGetValue(sessionRelativeSelector.SessionId, out var relSession)
                        || relSession.PlayerSession != message.MsgChannel.SessionId)
                    {
                        // TODO: logging?
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    var relObject = relSession.Object;
                    var relProperty = relSession.ObjectType.GetProperty(sessionRelativeSelector.PropertyName);
                    if (relProperty == null)
                    {
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    var attr = relProperty.GetCustomAttribute<ViewVariablesAttribute>();
                    if (attr == null)
                    {
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    object value;
                    try
                    {
                        value = relProperty.GetValue(relObject);
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("vv", "Exception while retrieving value for session. {0}", e);
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    if (value == null || value.GetType().IsValueType)
                    {
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    theObject = value;
                    break;
                default:
                    Deny(DenyReason.InvalidRequest);
                    return;
            }

            var sessionId = _nextSessionId++;
            ViewVariablesSession session;
            {
                // TODO: Flexibility here, and allow the client more control.
                if (theObject is IEntity entity)
                {
                    session = new ViewVariablesSessionEntity(message.MsgChannel.SessionId, entity, sessionId);
                }
                else
                {
                    session = new ViewVariablesSessionObject(message.MsgChannel.SessionId, theObject, sessionId);
                }
            }

            _sessions.Add(sessionId, session);

            var allowMsg = _netManager.CreateNetMessage<MsgViewVariablesOpenSession>();
            allowMsg.ReqId = message.ReqId;
            allowMsg.SessionId = session.SessionId;
            _netManager.ServerSendMessage(allowMsg, message.MsgChannel);


            player.PlayerStatusChanged += (_, args) =>
            {
                if (args.NewStatus == SessionStatus.Disconnected)
                {
                    _closeSession(session.SessionId, false);
                }
            };
        }

        private void _closeSession(uint sessionId, bool sendMsg)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return;
            }

            _sessions.Remove(sessionId);
            if (!sendMsg || !_playerManager.TryGetSessionById(session.PlayerSession, out var player) ||
                player.Status == SessionStatus.Disconnected)
            {
                return;
            }

            var closeMsg = _netManager.CreateNetMessage<MsgViewVariablesCloseSession>();
            closeMsg.SessionId = session.SessionId;
            _netManager.ServerSendMessage(closeMsg, player.ConnectedClient);
        }
    }
}
