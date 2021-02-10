using System;
using System.Collections.Generic;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using DenyReason = Robust.Shared.Network.Messages.MsgViewVariablesDenySession.DenyReason;

namespace Robust.Server.ViewVariables
{
    internal class ViewVariablesHost : ViewVariablesManagerShared, IViewVariablesHost
    {
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IRobustSerializer _robustSerializer = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private readonly Dictionary<uint, ViewVariablesesSession>
            _sessions = new();

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
                || session.PlayerUser != message.MsgChannel.UserId)
            {
                // TODO: logging?
                return;
            }

            _closeSession(message.SessionId, true);
        }

        private void _msgModifyRemote(MsgViewVariablesModifyRemote message)
        {
            if (!_sessions.TryGetValue(message.SessionId, out var session)
                || session.PlayerUser != message.MsgChannel.UserId)
            {
                // TODO: logging?
                return;
            }

            try
            {
                session.Modify(message.PropertyIndex, message.Value);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        private void _msgReqData(MsgViewVariablesReqData message)
        {
            if (!_sessions.TryGetValue(message.SessionId, out var session)
                || session.PlayerUser != message.MsgChannel.UserId)
            {
                // TODO: logging?
                return;
            }

            var blob = session.DataRequest(message.RequestMeta);

            var dataMsg = _netManager.CreateNetMessage<MsgViewVariablesRemoteData>();
            dataMsg.RequestId = message.RequestId;
            dataMsg.Blob = blob;
            _netManager.ServerSendMessage(dataMsg, message.MsgChannel);
        }

        private void _msgReqSession(MsgViewVariablesReqSession message)
        {
            void Deny(DenyReason reason)
            {
                var denyMsg = _netManager.CreateNetMessage<MsgViewVariablesDenySession>();
                denyMsg.RequestId = message.RequestId;
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
                    var compType = _reflectionManager.GetType(componentSelector.ComponentType);
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
                        || relSession.PlayerUser != message.MsgChannel.UserId)
                    {
                        // TODO: logging?
                        Deny(DenyReason.NoObject);
                        return;
                    }

                    object? value;
                    try
                    {
                        if (!relSession.TryGetRelativeObject(sessionRelativeSelector.PropertyIndex, out value))
                        {
                            Deny(DenyReason.InvalidRequest);
                            return;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Deny(DenyReason.NoObject);
                        return;
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

                case ViewVariablesIoCSelector ioCSelector:
                    var reflectionManager = IoCManager.Resolve<IReflectionManager>();
                    if (!reflectionManager.TryLooseGetType(ioCSelector.TypeName, out var type))
                    {
                        Deny(DenyReason.InvalidRequest);
                        return;
                    }

                    theObject = IoCManager.ResolveType(type);
                    break;

                default:
                    Deny(DenyReason.InvalidRequest);
                    return;
            }

            var sessionId = _nextSessionId++;
            var session = new ViewVariablesesSession(message.MsgChannel.UserId, theObject, sessionId, this,
                _robustSerializer);

            _sessions.Add(sessionId, session);

            var allowMsg = _netManager.CreateNetMessage<MsgViewVariablesOpenSession>();
            allowMsg.RequestId = message.RequestId;
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
            if (!sendMsg || !_playerManager.TryGetSessionById(session.PlayerUser, out var player) ||
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
