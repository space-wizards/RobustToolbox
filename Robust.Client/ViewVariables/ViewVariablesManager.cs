using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables.Editors;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.ViewVariables;
using NumberType = Robust.Client.ViewVariables.Editors.ViewVariablesPropertyEditorNumeric.NumberType;

namespace Robust.Client.ViewVariables
{
    internal class ViewVariablesManager : ViewVariablesManagerShared, IViewVariablesManagerInternal
    {
#pragma warning disable 649
        [Dependency] private readonly IClientNetManager _netManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        private uint _nextReqId = 1;
        private readonly Vector2i _defaultWindowSize = (640, 420);

        private readonly Dictionary<ViewVariablesInstance, SS14Window> _windows =
            new Dictionary<ViewVariablesInstance, SS14Window>();

        private readonly Dictionary<uint, ViewVariablesRemoteSession> _sessions =
            new Dictionary<uint, ViewVariablesRemoteSession>();

        private readonly Dictionary<uint, TaskCompletionSource<ViewVariablesRemoteSession>> _requestedSessions =
            new Dictionary<uint, TaskCompletionSource<ViewVariablesRemoteSession>>();

        private readonly Dictionary<uint, TaskCompletionSource<ViewVariablesBlob>> _requestedData
            = new Dictionary<uint, TaskCompletionSource<ViewVariablesBlob>>();

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgViewVariablesOpenSession>(MsgViewVariablesOpenSession.NAME,
                _netMessageOpenSession);
            _netManager.RegisterNetMessage<MsgViewVariablesRemoteData>(MsgViewVariablesRemoteData.NAME,
                _netMessageRemoteData);
            _netManager.RegisterNetMessage<MsgViewVariablesCloseSession>(MsgViewVariablesCloseSession.NAME,
                _netMessageCloseSession);
            _netManager.RegisterNetMessage<MsgViewVariablesDenySession>(MsgViewVariablesDenySession.NAME,
                _netMessageDenySession);
            _netManager.RegisterNetMessage<MsgViewVariablesModifyRemote>(MsgViewVariablesModifyRemote.NAME);
            _netManager.RegisterNetMessage<MsgViewVariablesReqSession>(MsgViewVariablesReqSession.NAME);
            _netManager.RegisterNetMessage<MsgViewVariablesReqData>(MsgViewVariablesReqData.NAME);
        }

        public ViewVariablesPropertyEditor PropertyFor(Type type)
        {
            // TODO: make this more flexible.
            if (type == null)
            {
                return new ViewVariablesPropertyEditorDummy();
            }

            if (type == typeof(sbyte))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.SByte);
            }

            if (type == typeof(byte))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.Byte);
            }

            if (type == typeof(ushort))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.UShort);
            }

            if (type == typeof(short))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.Short);
            }

            if (type == typeof(uint))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.UInt);
            }

            if (type == typeof(int))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.Int);
            }

            if (type == typeof(ulong))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.ULong);
            }

            if (type == typeof(long))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.Long);
            }

            if (type == typeof(float))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.Float);
            }

            if (type == typeof(double))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.Double);
            }

            if (type == typeof(decimal))
            {
                return new ViewVariablesPropertyEditorNumeric(NumberType.Decimal);
            }

            if (type == typeof(string))
            {
                return new ViewVariablesPropertyEditorString();
            }

            if (type.IsEnum)
            {
                return new ViewVariablesPropertyEditorEnum();
            }

            if (type == typeof(Vector2))
            {
                return new ViewVariablesPropertyEditorVector2(intVec: false);
            }

            if (type == typeof(Vector2i))
            {
                return new ViewVariablesPropertyEditorVector2(intVec: true);
            }

            if (type == typeof(bool))
            {
                return new ViewVariablesPropertyEditorBoolean();
            }

            if (type == typeof(Angle))
            {
                return new ViewVariablesPropertyEditorAngle();
            }

            if (type == typeof(Box2))
            {
                return new ViewVariablesPropertyEditorUIBox2(ViewVariablesPropertyEditorUIBox2.BoxType.Box2);
            }

            if (type == typeof(Box2i))
            {
                return new ViewVariablesPropertyEditorUIBox2(ViewVariablesPropertyEditorUIBox2.BoxType.Box2i);
            }

            if (type == typeof(UIBox2))
            {
                return new ViewVariablesPropertyEditorUIBox2(ViewVariablesPropertyEditorUIBox2.BoxType.UIBox2);
            }

            if (type == typeof(UIBox2i))
            {
                return new ViewVariablesPropertyEditorUIBox2(ViewVariablesPropertyEditorUIBox2.BoxType.UIBox2i);
            }

            if (type == typeof(GridCoordinates))
            {
                return new ViewVariablesPropertyEditorGridLocalCoordinates();
            }

            if (type == typeof(EntityUid))
            {
                return new ViewVariablesPropertyEditorEntityUid();
            }

            if (type == typeof(Color))
            {
                return new ViewVariablesPropertyEditorColor();
            }

            if (type != typeof(ViewVariablesBlobMembers.ServerValueTypeToken) && !type.IsValueType)
            {
                return new ViewVariablesPropertyEditorReference();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return new ViewVariablesPropertyEditorKeyValuePair();
            }

            return new ViewVariablesPropertyEditorDummy();
        }

        public void OpenVV(object obj)
        {
            // TODO: more flexibility in allowing custom instances here.
            ViewVariablesInstance instance;
            if (obj is IEntity entity && !entity.Deleted)
            {
                instance = new ViewVariablesInstanceEntity(this, _resourceCache, _entityManager);
            }
            else
            {
                instance = new ViewVariablesInstanceObject(this, _resourceCache);
            }

            var window = new SS14Window {Title = "View Variables"};
            instance.Initialize(window, obj);
            window.OnClose += () => _closeInstance(instance, false);
            _windows.Add(instance, window);
            window.Open();
            LayoutContainer.SetSize(window, _defaultWindowSize);
        }

        public async void OpenVV(ViewVariablesObjectSelector selector)
        {
            var window = new SS14Window {Title = "View Variables"};
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
            if (type != null && typeof(IEntity).IsAssignableFrom(type))
            {
                instance = new ViewVariablesInstanceEntity(this, _resourceCache, _entityManager);
            }
            else
            {
                instance = new ViewVariablesInstanceObject(this, _resourceCache);
            }

            loadingLabel.Dispose();
            instance.Initialize(window, blob, session);
            window.OnClose += () => _closeInstance(instance, false);
            _windows.Add(instance, window);
            window.Open();
            LayoutContainer.SetSize(window, _defaultWindowSize);
        }

        public Task<ViewVariablesRemoteSession> RequestSession(ViewVariablesObjectSelector selector)
        {
            var msg = _netManager.CreateNetMessage<MsgViewVariablesReqSession>();
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

            var msg = _netManager.CreateNetMessage<MsgViewVariablesReqData>();
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

            var closeMsg = _netManager.CreateNetMessage<MsgViewVariablesCloseSession>();
            closeMsg.SessionId = session.SessionId;
            _netManager.ClientSendMessage(closeMsg);
        }

        public void ModifyRemote(ViewVariablesRemoteSession session, object[] propertyIndex, object value)
        {
            if (!_sessions.ContainsKey(session.SessionId))
            {
                throw new ArgumentException();
            }

            var msg = _netManager.CreateNetMessage<MsgViewVariablesModifyRemote>();
            msg.SessionId = session.SessionId;
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
    }

    public class SessionDenyException : Exception
    {
        public SessionDenyException(MsgViewVariablesDenySession.DenyReason reason)
        {
            Reason = reason;
        }

        public MsgViewVariablesDenySession.DenyReason Reason { get; }
    }
}
