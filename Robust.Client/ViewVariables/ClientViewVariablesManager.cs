﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables.Editors;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using static Robust.Client.ViewVariables.Editors.VVPropEditorNumeric;

namespace Robust.Client.ViewVariables
{
    internal sealed partial class ClientViewVariablesManager : ViewVariablesManager, IClientViewVariablesManagerInternal
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IRobustSerializer _robustSerializer = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private uint _nextReqId = 1;
        private readonly Vector2i _defaultWindowSize = (640, 420);

        private readonly Dictionary<ViewVariablesInstance, DefaultWindow> _windows =
            new();

        private readonly Dictionary<uint, ViewVariablesRemoteSession> _sessions =
            new();

        private readonly Dictionary<uint, TaskCompletionSource<ViewVariablesRemoteSession>> _requestedSessions =
            new();

        private readonly Dictionary<uint, TaskCompletionSource<ViewVariablesBlob>> _requestedData
            = new();

        public override void Initialize()
        {
            base.Initialize();
            InitializeDomains();
            _netManager.RegisterNetMessage<MsgViewVariablesOpenSession>(_netMessageOpenSession);
            _netManager.RegisterNetMessage<MsgViewVariablesRemoteData>(_netMessageRemoteData);
            _netManager.RegisterNetMessage<MsgViewVariablesCloseSession>(_netMessageCloseSession);
            _netManager.RegisterNetMessage<MsgViewVariablesDenySession>(_netMessageDenySession);
            _netManager.RegisterNetMessage<MsgViewVariablesModifyRemote>();
            _netManager.RegisterNetMessage<MsgViewVariablesReqSession>();
            _netManager.RegisterNetMessage<MsgViewVariablesReqData>();
        }

        public VVPropEditor PropertyFor(Type? type)
        {
            // TODO: make this more flexible.
            if (type == null)
            {
                return new VVPropEditorDummy();
            }

            if (type == typeof(sbyte))
            {
                return new VVPropEditorNumeric(NumberType.SByte);
            }

            if (type == typeof(byte))
            {
                return new VVPropEditorNumeric(NumberType.Byte);
            }

            if (type == typeof(ushort))
            {
                return new VVPropEditorNumeric(NumberType.UShort);
            }

            if (type == typeof(short))
            {
                return new VVPropEditorNumeric(NumberType.Short);
            }

            if (type == typeof(uint))
            {
                return new VVPropEditorNumeric(NumberType.UInt);
            }

            if (type == typeof(int))
            {
                return new VVPropEditorNumeric(NumberType.Int);
            }

            if (type == typeof(ulong))
            {
                return new VVPropEditorNumeric(NumberType.ULong);
            }

            if (type == typeof(long))
            {
                return new VVPropEditorNumeric(NumberType.Long);
            }

            if (type == typeof(float))
            {
                return new VVPropEditorNumeric(NumberType.Float);
            }

            if (type == typeof(double))
            {
                return new VVPropEditorNumeric(NumberType.Double);
            }

            if (type == typeof(decimal))
            {
                return new VVPropEditorNumeric(NumberType.Decimal);
            }

            if (type == typeof(string))
            {
                return new VVPropEditorString();
            }

            if (typeof(IPrototype).IsAssignableFrom(type) || typeof(ViewVariablesBlobMembers.PrototypeReferenceToken).IsAssignableFrom(type))
            {
                return (VVPropEditor)Activator.CreateInstance(typeof(VVPropEditorIPrototype<>).MakeGenericType(type))!;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(type))
            {
                return (VVPropEditor)Activator.CreateInstance(typeof(VVPropEditorISelfSerializable<>).MakeGenericType(type))!;
            }

            if (type.IsEnum)
            {
                return new VVPropEditorEnum();
            }

            if (type == typeof(Vector2))
            {
                return new VVPropEditorVector2(intVec: false);
            }

            if (type == typeof(Vector2i))
            {
                return new VVPropEditorVector2(intVec: true);
            }

            if (type == typeof(bool))
            {
                return new VVPropEditorBoolean();
            }

            if (type == typeof(Angle))
            {
                return new VVPropEditorAngle();
            }

            if (type == typeof(Box2))
            {
                return new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.Box2);
            }

            if (type == typeof(Box2i))
            {
                return new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.Box2i);
            }

            if (type == typeof(UIBox2))
            {
                return new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.UIBox2);
            }

            if (type == typeof(UIBox2i))
            {
                return new VVPropEditorUIBox2(VVPropEditorUIBox2.BoxType.UIBox2i);
            }

            if (type == typeof(EntityCoordinates))
            {
                return new VVPropEditorEntityCoordinates();
            }

            if (type == typeof(EntityUid))
            {
                return new VVPropEditorEntityUid();
            }

            if (type == typeof(Color))
            {
                return new VVPropEditorColor();
            }

            if (type == typeof(TimeSpan))
            {
                return new VVPropEditorTimeSpan();
            }

            if (type == typeof(ViewVariablesBlobMembers.ServerKeyValuePairToken) ||
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return new VVPropEditorKeyValuePair();
            }

            if (type != typeof(ViewVariablesBlobMembers.ServerValueTypeToken) && !type.IsValueType)
            {
                return new VVPropEditorReference();
            }

            return new VVPropEditorDummy();
        }

        public void OpenVV(object obj)
        {
            // TODO: more flexibility in allowing custom instances here.
            ViewVariablesInstance instance;
            if (obj is EntityUid entity && _entityManager.EntityExists(entity))
            {
                instance = new ViewVariablesInstanceEntity(this, _entityManager, _robustSerializer);
            }
            else
            {
                instance = new ViewVariablesInstanceObject(this, _robustSerializer);
            }

            var window = new DefaultWindow {Title = "View Variables"};
            instance.Initialize(window, obj);
            window.OnClose += () => _closeInstance(instance, false);
            _windows.Add(instance, window);
            window.SetSize = _defaultWindowSize;
            window.Open();
        }

        public void OpenVV(string path)
        {
            if (ReadPath(path) is {} obj)
                OpenVV(obj);
        }

        public async void OpenVV(ViewVariablesObjectSelector selector)
        {
            var window = new DefaultWindow
            {
                Title = "View Variables",
                SetSize = _defaultWindowSize
            };
            var loadingLabel = new Label {Text = "Retrieving remote object data from server..."};
            window.Contents.AddChild(loadingLabel);

            // We need to request the data, THEN create an instance.
            // Because we don't know what instance to make until we asked the server about the object data.

            ViewVariablesRemoteSession session;
            try
            {
                session = await RequestSession(selector);
            }
            catch (SessionDenyException e)
            {
                loadingLabel.Text = $"Server denied session request: {e.Reason}";
                return;
            }

            var blob = await RequestData<ViewVariablesBlobMetadata>(session, new ViewVariablesRequestMetadata());
            var type = Type.GetType(blob.ObjectType);
            // TODO: more flexibility in allowing custom instances here.
            ViewVariablesInstance instance;
            if (type != null && typeof(EntityUid).IsAssignableFrom(type))
            {
                instance = new ViewVariablesInstanceEntity(this, _entityManager, _robustSerializer);
            }
            else
            {
                instance = new ViewVariablesInstanceObject(this, _robustSerializer);
            }

            loadingLabel.Dispose();
            instance.Initialize(window, blob, session);
            window.OnClose += () => _closeInstance(instance, false);
            _windows.Add(instance, window);
            window.Size = _defaultWindowSize;
            window.Open();
        }

        public Task<ViewVariablesRemoteSession> RequestSession(ViewVariablesObjectSelector selector)
        {
            var msg = new MsgViewVariablesReqSession();
            msg.Selector = selector;
            msg.RequestId = _nextReqId++;
            _netManager.ClientSendMessage(msg);
            var tcs = new TaskCompletionSource<ViewVariablesRemoteSession>();
            _requestedSessions.Add(msg.RequestId, tcs);
            return tcs.Task;
        }

        public Task<ViewVariablesBlob> RequestData(ViewVariablesRemoteSession session, ViewVariablesRequest meta)
        {
            if (session.Closed)
            {
                throw new ArgumentException("Session is closed", nameof(session));
            }

            var msg = new MsgViewVariablesReqData();
            var reqId = msg.RequestId = _nextReqId++;
            msg.RequestMeta = meta;
            msg.SessionId = session.SessionId;
            _netManager.ClientSendMessage(msg);
            var tcs = new TaskCompletionSource<ViewVariablesBlob>();
            _requestedData.Add(reqId, tcs);
            return tcs.Task;
        }

        public async Task<T> RequestData<T>(ViewVariablesRemoteSession session, ViewVariablesRequest meta) where T : ViewVariablesBlob
        {
            return (T) await RequestData(session, meta);
        }

        public void CloseSession(ViewVariablesRemoteSession session)
        {
            if (!_sessions.ContainsKey(session.SessionId))
            {
                throw new ArgumentException();
            }

            var closeMsg = new MsgViewVariablesCloseSession();
            closeMsg.SessionId = session.SessionId;
            _netManager.ClientSendMessage(closeMsg);
        }

        public bool TryGetSession(uint sessionId, [NotNullWhen(true)] out ViewVariablesRemoteSession? session)
        {
            return _sessions.TryGetValue(sessionId, out session);
        }

        public void ModifyRemote(ViewVariablesRemoteSession session, object[] propertyIndex, object? value, bool reinterpretValue = false)
        {
            if (!_sessions.ContainsKey(session.SessionId))
            {
                throw new ArgumentException();
            }

            var msg = new MsgViewVariablesModifyRemote();
            msg.SessionId = session.SessionId;
            msg.ReinterpretValue = reinterpretValue;
            msg.PropertyIndex = propertyIndex;
            msg.Value = value;
            _netManager.ClientSendMessage(msg);
        }

        private void _closeInstance(ViewVariablesInstance instance, bool closeWindow)
        {
            if (!_windows.TryGetValue(instance, out var window))
            {
                throw new ArgumentException();
            }

            if (closeWindow)
            {
                window.Dispose();
            }

            _windows.Remove(instance);
            instance.Close();
        }

        private void _netMessageOpenSession(MsgViewVariablesOpenSession msg)
        {
            if (!_requestedSessions.TryGetValue(msg.RequestId, out var tcs))
            {
                Logger.ErrorS("vv", "Server sent us new session {0}/{1} which we didn't request.", msg.RequestId,
                    msg.SessionId);
                return;
            }

            _requestedSessions.Remove(msg.RequestId);
            var session = new ViewVariablesRemoteSession(msg.SessionId);
            _sessions.Add(msg.SessionId, session);
            tcs.SetResult(session);
        }

        private void _netMessageCloseSession(MsgViewVariablesCloseSession message)
        {
            if (!_sessions.TryGetValue(message.SessionId, out var session))
            {
                Logger.WarningS("vv", "Got a close session message for an unknown session: {0}", message.SessionId);
                return;
            }

            session.Closed = true;
            _sessions.Remove(message.SessionId);
        }

        private void _netMessageRemoteData(MsgViewVariablesRemoteData message)
        {
            if (!_requestedData.TryGetValue(message.RequestId, out var tcs))
            {
                Logger.WarningS("vv", "Server sent us data we didn't request: {0}.", message.RequestId);
                return;
            }

            _requestedData.Remove(message.RequestId);
            tcs.SetResult(message.Blob);
        }

        private void _netMessageDenySession(MsgViewVariablesDenySession message)
        {
            if (!_requestedSessions.TryGetValue(message.RequestId, out var tcs))
            {
                Logger.WarningS("vv", "Server sent us a deny session {0} which we didn't request.", message.RequestId);
                return;
            }

            _requestedSessions.Remove(message.RequestId);
            tcs.SetException(new SessionDenyException(message.Reason));
        }

        protected override bool CheckPermissions(INetChannel channel, string command)
        {
            // Acquiesce, client!! Do what the server tells you.
            return true;
        }

        protected override bool TryGetSession(Guid guid, [NotNullWhen(true)] out ICommonSession? session)
        {
            session = null;
            return false;
        }
    }

    [Virtual]
    public class SessionDenyException : Exception
    {
        public SessionDenyException(ViewVariablesResponseCode reason)
        {
            Reason = reason;
        }

        public ViewVariablesResponseCode Reason { get; }
    }
}
