using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables
{
    internal sealed partial class ServerViewVariablesManager : ViewVariablesManager, IServerViewVariablesInternal
    {
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConGroupController _groupController = default!;
        [Dependency] private readonly IRobustSerializer _robustSerializer = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<uint, ViewVariablesSession>
            _sessions = new();

        private readonly Dictionary<NetUserId, List<uint>> _users = new();

        private uint _nextSessionId = 1;

        public override void Initialize()
        {
            base.Initialize();
            InitializeDomains();
            _netManager.RegisterNetMessage<MsgViewVariablesReqSession>(_msgReqSession);
            _netManager.RegisterNetMessage<MsgViewVariablesReqData>(_msgReqData);
            _netManager.RegisterNetMessage<MsgViewVariablesModifyRemote>(_msgModifyRemote);
            _netManager.RegisterNetMessage<MsgViewVariablesCloseSession>(_msgCloseSession);
            _netManager.RegisterNetMessage<MsgViewVariablesDenySession>();
            _netManager.RegisterNetMessage<MsgViewVariablesOpenSession>();
            _netManager.RegisterNetMessage<MsgViewVariablesRemoteData>();

            _playerManager.PlayerStatusChanged += OnStatusChanged;
        }

        private void OnStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Disconnected)
                return;

            if (!_users.TryGetValue(e.Session.UserId, out var vvSessions))
                return;

            foreach (var id in vvSessions)
            {
                _closeSession(id, false);
            }
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
                var value = message.Value;

                if (message.ReinterpretValue && !TryReinterpretValue(value, out value))
                {
                    Sawmill.Warning($"Couldn't reinterpret value \"{message.Value}\" sent by {session.PlayerUser}!");
                    return;
                }

                session.Modify(message.PropertyIndex, value);
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

            var dataMsg = new MsgViewVariablesRemoteData();
            dataMsg.RequestId = message.RequestId;
            dataMsg.Blob = blob;
            _netManager.ServerSendMessage(dataMsg, message.MsgChannel);
        }

        private void _msgReqSession(MsgViewVariablesReqSession message)
        {
            void Deny(ViewVariablesResponseCode reason)
            {
                var denyMsg = new MsgViewVariablesDenySession();
                denyMsg.RequestId = message.RequestId;
                denyMsg.Reason = reason;
                _netManager.ServerSendMessage(denyMsg, message.MsgChannel);
            }

            var player = _playerManager.GetSessionByChannel(message.MsgChannel);
            if (!_groupController.CanCommand(player, "vv"))
            {
                Deny(ViewVariablesResponseCode.NoAccess);
                return;
            }

            object theObject;

            switch (message.Selector)
            {
                case ViewVariablesComponentSelector componentSelector:
                {
                    var compType = _reflectionManager.GetType(componentSelector.ComponentType);
                    var entity = _entityManager.GetEntity(componentSelector.Entity);

                    if (compType == null ||
                        !_entityManager.TryGetComponent(entity, compType, out var component))
                    {
                        Deny(ViewVariablesResponseCode.NoObject);
                        return;
                    }

                    theObject = component;
                    break;
                }
                case ViewVariablesEntitySelector entitySelector:
                {
                    var entity = _entityManager.GetEntity(entitySelector.Entity);

                    if (!_entityManager.EntityExists(entity))
                    {
                        Deny(ViewVariablesResponseCode.NoObject);
                        return;
                    }

                    theObject = entitySelector.Entity;
                    break;
                }
                case ViewVariablesSessionRelativeSelector sessionRelativeSelector:
                {
                    if (!_sessions.TryGetValue(sessionRelativeSelector.SessionId, out var relSession)
                        || relSession.PlayerUser != message.MsgChannel.UserId)
                    {
                        // TODO: logging?
                        Deny(ViewVariablesResponseCode.NoObject);
                        return;
                    }

                    object? value;
                    try
                    {
                        if (!relSession.TryGetRelativeObject(sessionRelativeSelector.PropertyIndex, out value))
                        {
                            Deny(ViewVariablesResponseCode.InvalidRequest);
                            return;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Deny(ViewVariablesResponseCode.NoObject);
                        return;
                    }
                    catch (Exception e)
                    {
                        Sawmill.Error("Exception while retrieving value for session. {0}", e);
                        Deny(ViewVariablesResponseCode.NoObject);
                        return;
                    }

                    if (value == null || value.GetType().IsValueType)
                    {
                        Deny(ViewVariablesResponseCode.NoObject);
                        return;
                    }

                    theObject = value;
                    break;
                }
                case ViewVariablesIoCSelector ioCSelector:
                {
                    var reflectionManager = _reflectionManager;
                    if (!reflectionManager.TryLooseGetType(ioCSelector.TypeName, out var type))
                    {
                        Deny(ViewVariablesResponseCode.InvalidRequest);
                        return;
                    }

                    theObject = _dependencyCollection.ResolveType(type);
                    break;
                }
                case ViewVariablesEntitySystemSelector esSelector:
                {
                    var reflectionManager = _reflectionManager;
                    if (!reflectionManager.TryLooseGetType(esSelector.TypeName, out var type))
                    {
                        Deny(ViewVariablesResponseCode.InvalidRequest);
                        return;
                    }

                    theObject = _entityManager.EntitySysManager.GetEntitySystem(type);
                    break;
                }
                case ViewVariablesPathSelector paSelector:
                {
                    if (ResolvePath(paSelector.Path)?.Get() is not {} obj)
                    {
                        Deny(ViewVariablesResponseCode.NoObject);
                        return;
                    }

                    theObject = obj;
                    break;
                }
                default:
                    Deny(ViewVariablesResponseCode.InvalidRequest);
                    return;
            }

            var sessionId = _nextSessionId++;
            var session = new ViewVariablesSession(message.MsgChannel.UserId, theObject, sessionId, this,
                _robustSerializer, _entityManager, Sawmill);

            _sessions.Add(sessionId, session);
            _users.GetOrNew(session.PlayerUser).Add(sessionId);

            var allowMsg = new MsgViewVariablesOpenSession();
            allowMsg.RequestId = message.RequestId;
            allowMsg.SessionId = session.SessionId;
            _netManager.ServerSendMessage(allowMsg, message.MsgChannel);

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

            var closeMsg = new MsgViewVariablesCloseSession();
            closeMsg.SessionId = session.SessionId;
            _netManager.ServerSendMessage(closeMsg, player.Channel);
        }

        private bool TryReinterpretValue(object? input, [NotNullWhen(true)] out object? output)
        {
            output = null;

            switch (input)
            {
                case ViewVariablesBlobMembers.PrototypeReferenceToken token:
                    if (!_prototypeManager.TryGetKindType(token.Variant, out var variantType))
                        return false;

                    if (!_prototypeManager.TryIndex(variantType, token.ID, out var prototype))
                        return false;

                    output = prototype;
                    return true;
                default:
                    return false;
            }
        }

        protected override bool CheckPermissions(INetChannel channel, string command)
        {
            return _playerManager.TryGetSessionByChannel(channel, out var session) && _groupController.CanCommand(session, command);
        }

        protected override bool TryGetSession(Guid guid, [NotNullWhen(true)] out ICommonSession? session)
        {
            if (guid != Guid.Empty
                && _playerManager.TryGetSessionById(new NetUserId(guid), out var player)
                && !_groupController.CanCommand(player, "vv")) // Can't VV other admins.
            {
                session = player;
                return true;
            }

            session = null;
            return false;
        }
    }
}
