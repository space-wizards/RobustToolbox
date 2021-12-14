using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Network.Messages;
using Robust.Shared.Utility;

namespace Robust.Client.Console
{
    public partial class ScriptClient
    {
        private sealed class ScriptConsoleServer : ScriptConsole
        {
            private readonly ScriptClient _client;
            private readonly int _session;

            private int _linesEntered;
            private string? _lastEnteredText;

            public ScriptConsoleServer(ScriptClient client, int session)
            {
                _client = client;
                _session = session;
                Title = Loc.GetString("Robust C# Interactive (SERVER)");

                OutputPanel.AddText(Loc.GetString(@"Robust C# interactive console (SERVER)."));
                OutputPanel.AddText(">");
            }

            protected override void Run()
            {
                if (RunButton.Disabled || string.IsNullOrWhiteSpace(InputBar.Text))
                {
                    return;
                }

                RunButton.Disabled = true;

                var msg = _client._netManager.CreateNetMessage<MsgScriptEval>();
                msg.ScriptSession = _session;
                msg.Code = _lastEnteredText = InputBar.Text;

                _client._netManager.ClientSendMessage(msg);

                InputBar.Clear();


            }

            protected override void Complete()
            {
                var msg = _client._netManager.CreateNetMessage<MsgScriptCompletion>();
                msg.ScriptSession = _session;
                msg.Code = InputBar.Text;
                msg.Cursor = InputBar.CursorPosition;

                _client._netManager.ClientSendMessage(msg);
            }

            public override void Close()
            {
                base.Close();

                _client.ConsoleClosed(_session);
            }

            public void ReceiveResponse(MsgScriptResponse response)
            {
                RunButton.Disabled = false;

                // Remove > or . at the end of the output panel.
                OutputPanel.RemoveEntry(^1);
                _linesEntered += 1;

                if (!response.WasComplete)
                {
                    if (_linesEntered == 1)
                    {
                        OutputPanel.AddText($"> {_lastEnteredText}");
                    }
                    else
                    {
                        OutputPanel.AddText($". {_lastEnteredText}");
                    }

                    OutputPanel.AddText(".");
                    return;
                }

                // Remove echo of partial submission from the output panel.
                for (var i = 1; i < _linesEntered; i++)
                {
                    OutputPanel.RemoveEntry(^1);
                }

                _linesEntered = 0;

                // Echo entered script.
                OutputPanel.AddMessage(new FormattedMessage(new []
                    {
                        new Section() { Color=unchecked((int) 0xFFD4D4D4), Content="> " },
                    }
                ));

                OutputPanel.AddMessage(response.Echo);

                OutputPanel.AddMessage(response.Response);

                OutputPanel.AddText(">");
            }

            public void ReceiveCompletionResponse(MsgScriptCompletionResponse response)
            {
                Suggestions.SetSuggestions(response);
                Suggestions.OpenAt((Position.X + Size.X, Position.Y), (Size.X / 2, Size.Y));
            }
        }
    }
}
