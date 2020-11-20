using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Scripting;
using Robust.Shared.Utility;

#nullable enable

namespace Robust.Server.Scripting
{
    internal sealed class ScriptHost : IScriptHost
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IConGroupController _conGroupController = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        readonly Dictionary<IPlayerSession, Dictionary<int, ScriptInstance>> _instances =
            new();

        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgScriptStop>(MsgScriptStop.NAME, ReceiveScriptEnd);
            _netManager.RegisterNetMessage<MsgScriptEval>(MsgScriptEval.NAME, ReceiveScriptEval);
            _netManager.RegisterNetMessage<MsgScriptStart>(MsgScriptStart.NAME, ReceiveScriptStart);
            _netManager.RegisterNetMessage<MsgScriptResponse>(MsgScriptResponse.NAME);
            _netManager.RegisterNetMessage<MsgScriptStartAck>(MsgScriptStartAck.NAME);

            _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
        }

        private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            // GC it up.
            _instances.Remove(e.Session);
        }

        private void ReceiveScriptEnd(MsgScriptStop message)
        {
            if (!_playerManager.TryGetSessionByChannel(message.MsgChannel, out var session))
            {
                return;
            }

            if (!_instances.TryGetValue(session, out var instances))
            {
                return;
            }

            instances.Remove(message.ScriptSession);
        }

        private void ReceiveScriptStart(MsgScriptStart message)
        {
            var reply = _netManager.CreateNetMessage<MsgScriptStartAck>();
            reply.ScriptSession = message.ScriptSession;
            reply.WasAccepted = false;
            if (!_playerManager.TryGetSessionByChannel(message.MsgChannel, out var session))
            {
                _netManager.ServerSendMessage(reply, message.MsgChannel);
                return;
            }

            if (!_conGroupController.CanViewVar(session))
            {
                Logger.WarningS("script", "Client {0} tried to access Scripting without permissions.", session);
                _netManager.ServerSendMessage(reply, message.MsgChannel);
                return;
            }

            var instances = _instances.GetOrNew(session);

            if (instances.ContainsKey(message.ScriptSession))
            {
                // Already got one with this ID, client's problem.
                _netManager.ServerSendMessage(reply, message.MsgChannel);
                return;
            }

            ScriptInstanceShared.InitDummy();

            var instance = new ScriptInstance();
            instances.Add(message.ScriptSession, instance);

            reply.WasAccepted = true;
            _netManager.ServerSendMessage(reply, message.MsgChannel);
        }

        private async void ReceiveScriptEval(MsgScriptEval message)
        {
            if (!_playerManager.TryGetSessionByChannel(message.MsgChannel, out var session))
            {
                return;
            }

            if (!_conGroupController.CanViewVar(session))
            {
                Logger.WarningS("script", "Client {0} tried to access Scripting without permissions.", session);
                return;
            }

            if (!_instances.TryGetValue(session, out var instances) ||
                !instances.TryGetValue(message.ScriptSession, out var instance))
            {
                return;
            }

            var replyMessage = _netManager.CreateNetMessage<MsgScriptResponse>();
            replyMessage.ScriptSession = message.ScriptSession;

            var code = message.Code;

            instance.InputBuffer.AppendLine(code);

            var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(instance.InputBuffer.ToString()),
                ScriptInstanceShared.ParseOptions);

            if (!SyntaxFactory.IsCompleteSubmission(tree))
            {
                replyMessage.WasComplete = false;
                _netManager.ServerSendMessage(replyMessage, message.MsgChannel);
                return;
            }

            replyMessage.WasComplete = true;

            code = instance.InputBuffer.ToString().Trim();

            instance.InputBuffer.Clear();

            Script newScript;

            if (instance.State != null)
            {
                newScript = instance.State.Script.ContinueWith(code);
            }
            else
            {
                var options = ScriptInstanceShared.GetScriptOptions(_reflectionManager);
                newScript = CSharpScript.Create(code, options, typeof(ScriptGlobals));
            }

            // Compile ahead of time so that we can do syntax highlighting correctly for the echo.
            newScript.Compile();

            // Echo entered script.
            var echoMessage = new FormattedMessage();
            ScriptInstanceShared.AddWithSyntaxHighlighting(newScript, echoMessage, code, instance.HighlightWorkspace);

            replyMessage.Echo = echoMessage;

            var msg = new FormattedMessage();

            try
            {
                instance.RunningScript = true;
                if (instance.State != null)
                {
                    instance.State = await newScript.RunFromAsync(instance.State, _ => true);
                }
                else
                {
                    instance.State = await newScript.RunAsync(instance.Globals, _ => true);
                }
            }
            catch (CompilationErrorException e)
            {
                msg.PushColor(Color.Crimson);

                foreach (var diagnostic in e.Diagnostics)
                {
                    msg.AddText(diagnostic.ToString());
                    msg.AddText("\n");
                }

                replyMessage.Response = msg;
                _netManager.ServerSendMessage(replyMessage, message.MsgChannel);
                return;
            }
            finally
            {
                instance.RunningScript = false;
            }

            if (instance.OutputBuffer.Length != 0)
            {
                msg.AddText(instance.OutputBuffer.ToString());
                instance.OutputBuffer.Clear();
            }

            if (instance.State.Exception != null)
            {
                msg.PushColor(Color.Crimson);
                msg.AddText(CSharpObjectFormatter.Instance.FormatException(instance.State.Exception));
            }
            else if (ScriptInstanceShared.HasReturnValue(newScript))
            {
                msg.AddText(CSharpObjectFormatter.Instance.FormatObject(instance.State.ReturnValue));
            }

            replyMessage.Response = msg;
            _netManager.ServerSendMessage(replyMessage, message.MsgChannel);
        }

        private sealed class ScriptInstance
        {
            public Workspace HighlightWorkspace { get; } = new AdhocWorkspace();
            public StringBuilder InputBuffer { get; } = new();
            public StringBuilder OutputBuffer { get; } = new();
            public bool RunningScript { get; set; }

            public ScriptGlobals Globals { get; }
            public ScriptState? State { get; set; }

            public ScriptInstance()
            {
                Globals = new ScriptGlobalsImpl(this);
            }
        }

        private sealed class ScriptGlobalsImpl : ScriptGlobals
        {
            private readonly ScriptInstance _scriptInstance;

            public ScriptGlobalsImpl(ScriptInstance scriptInstance)
            {
                _scriptInstance = scriptInstance;
                IoCManager.InjectDependencies(this);
            }

            public override void write(object toString)
            {
                if (_scriptInstance.RunningScript)
                {
                    _scriptInstance.OutputBuffer.AppendLine(toString?.ToString());
                }
            }

            public override void show(object obj)
            {
                write(CSharpObjectFormatter.Instance.FormatObject(obj));
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [PublicAPI]
    public abstract class ScriptGlobals : ScriptGlobalsShared
    {
    }
}
