using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Reflection;
using Robust.Shared.Scripting;
using Robust.Shared.Utility;
using static Robust.Shared.Network.Messages.MsgScriptCompletionResponse;

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
            _netManager.RegisterNetMessage<MsgScriptStop>(ReceiveScriptEnd);
            _netManager.RegisterNetMessage<MsgScriptEval>(ReceiveScriptEval);
            _netManager.RegisterNetMessage<MsgScriptStart>(ReceiveScriptStart);
            _netManager.RegisterNetMessage<MsgScriptCompletion>(ReceiveScriptCompletion);
            _netManager.RegisterNetMessage<MsgScriptCompletionResponse>();
            _netManager.RegisterNetMessage<MsgScriptResponse>();
            _netManager.RegisterNetMessage<MsgScriptStartAck>();

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

            if (!_conGroupController.CanScript(session))
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

            if (!_conGroupController.CanScript(session))
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

            if (code == "y" && instance.AutoImportRepeatBuffer.HasValue)
            {
                var (imports, repeatCode) = instance.AutoImportRepeatBuffer.Value;
                var sb = new StringBuilder();
                foreach (var import in imports)
                {
                    sb.AppendFormat("using {0};\n", import);
                }

                sb.Append(repeatCode);

                code = sb.ToString();
                replyMessage.WasComplete = true;
            }
            else
            {
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
            }

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

                PromptAutoImports(e.Diagnostics, code, msg, instance);

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
                msg.AddText(ScriptInstanceShared.SafeFormat(instance.State.ReturnValue));
            }

            replyMessage.Response = msg;
            _netManager.ServerSendMessage(replyMessage, message.MsgChannel);
        }

        private async void ReceiveScriptCompletion(MsgScriptCompletion message)
        {
            if (!_playerManager.TryGetSessionByChannel(message.MsgChannel, out var session))
                return;

            if (!_conGroupController.CanScript(session))
            {
                Logger.WarningS("script", "Client {0} tried to access Scripting without permissions.", session);
                return;
            }

            if (!_instances.TryGetValue(session, out var instances) ||
                !instances.TryGetValue(message.ScriptSession, out var instance))
                    return;

            var replyMessage = _netManager.CreateNetMessage<MsgScriptCompletionResponse>();
            replyMessage.ScriptSession = message.ScriptSession;

            // Everything below here cribbed from
            // https://www.strathweb.com/2018/12/using-roslyn-c-completion-service-programmatically/
            var workspace = new AdhocWorkspace(MefHostServices.Create(MefHostServices.DefaultAssemblies));

            var scriptProject = workspace.AddProject(ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "Script", "Script",
                    LanguageNames.CSharp,
                    isSubmission: true
                )
                .WithMetadataReferences(
                        _reflectionManager.Assemblies.Select(a => MetadataReference.CreateFromFile(a.Location))
                )
                .WithCompilationOptions(new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    usings: ScriptInstanceShared.DefaultImports
            )));

            var document = workspace.AddDocument(DocumentInfo.Create(
                DocumentId.CreateNewId(scriptProject.Id),
                "Script",
                sourceCodeKind: SourceCodeKind.Script,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(message.Code), VersionStamp.Create()))
            ));

            var results = await CompletionService
                .GetService(document)
                .GetCompletionsAsync(document, message.Cursor);

            if (results is not null)
            {
                var ires = ImmutableArray.CreateBuilder<LiteResult>();
                foreach  (var r in results.Items)
                    ires.Add(new LiteResult(
                                displayText: r.DisplayText,
                                displayTextPrefix: r.DisplayTextPrefix,
                                displayTextSuffix: r.DisplayTextSuffix,
                                inlineDescription: r.InlineDescription,
                                tags: r.Tags,
                                properties: r.Properties
                    ));

                replyMessage.Results = ires.ToImmutable();
            }
            else
                replyMessage.Results = ImmutableArray<LiteResult>.Empty;

            _netManager.ServerSendMessage(replyMessage, message.MsgChannel);
        }

        private void PromptAutoImports(
            IEnumerable<Diagnostic> diags,
            string code,
            FormattedMessage output,
            ScriptInstance instance)
        {
            if (!ScriptInstanceShared.CalcAutoImports(_reflectionManager, diags, out var found))
                return;

            output.AddText($"Auto-import {string.Join(", ", found)} (enter 'y')?");

            instance.AutoImportRepeatBuffer = (found.ToArray(), code);
        }


        private sealed class ScriptInstance
        {
            public Workspace HighlightWorkspace { get; } = new AdhocWorkspace();
            public StringBuilder InputBuffer { get; } = new();
            public StringBuilder OutputBuffer { get; } = new();
            public bool RunningScript { get; set; }

            public ScriptGlobals Globals { get; }
            public ScriptState? State { get; set; }

            public (string[] imports, string code)? AutoImportRepeatBuffer;

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
                write(ScriptInstanceShared.SafeFormat(obj));
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [PublicAPI]
    public abstract class ScriptGlobals : ScriptGlobalsShared
    {
    }
}
