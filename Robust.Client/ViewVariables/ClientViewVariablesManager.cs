using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables
{
    internal sealed partial class ClientViewVariablesManager : ViewVariablesManager, IClientViewVariablesManagerInternal
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IRobustSerializer _robustSerializer = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IViewVariableControlFactory _controlFactory = default!;

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
            return _controlFactory.CreateFor(type);
        }

        public void OpenVV(object obj)
        {
            // TODO: more flexibility in allowing custom instances here.
            ViewVariablesInstance instance;
            if (obj is NetEntity netEntity && _entityManager.GetEntity(netEntity).IsValid())
            {
                instance = new ViewVariablesInstanceEntity(this, _entityManager, _robustSerializer, Sawmill);
            }
            else
            {
                instance = new ViewVariablesInstanceObject(this, _robustSerializer);
            }

            var window = new DefaultWindow { Title = Loc.GetString("view-variables") };
            instance.Initialize(window, obj);
            window.OnClose += () => _closeInstance(instance, false);
            _windows.Add(instance, window);
            window.SetSize = _defaultWindowSize;
            window.Open();
        }

        public void OpenVV(string path)
        {
            if (ReadPath(path) is { } obj)
                OpenVV(obj);
        }

        public async void OpenVV(ViewVariablesObjectSelector selector)
        {
            var window = new DefaultWindow
            {
                Title = Loc.GetString("view-variables"),
                SetSize = _defaultWindowSize
            };
            var loadingLabel = new Label { Text = "Retrieving remote object data from server..." };
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
            if (type != null && typeof(NetEntity).IsAssignableFrom(type))
            {
                instance = new ViewVariablesInstanceEntity(this, _entityManager, _robustSerializer, Sawmill);
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
            return (T)await RequestData(session, meta);
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
                Sawmill.Error("Server sent us new session {0}/{1} which we didn't request.", msg.RequestId,
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
                Sawmill.Warning("Got a close session message for an unknown session: {0}", message.SessionId);
                return;
            }

            session.Closed = true;
            _sessions.Remove(message.SessionId);
        }

        private void _netMessageRemoteData(MsgViewVariablesRemoteData message)
        {
            if (!_requestedData.TryGetValue(message.RequestId, out var tcs))
            {
                Sawmill.Warning("Server sent us data we didn't request: {0}.", message.RequestId);
                return;
            }

            _requestedData.Remove(message.RequestId);
            tcs.SetResult(message.Blob);
        }

        private void _netMessageDenySession(MsgViewVariablesDenySession message)
        {
            if (!_requestedSessions.TryGetValue(message.RequestId, out var tcs))
            {
                Sawmill.Warning("Server sent us a deny session {0} which we didn't request.", message.RequestId);
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
