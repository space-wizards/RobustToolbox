using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.SpeechSynthesis;

//
// Speech synthesis related console commands
//

internal sealed class TextToSpeechCommand : LocalizedCommands
{
    [Dependency] private readonly ITtsManager _ttsManager = default!;

    public override string Command => "tts";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ttsManager.Speak(args[0]);
    }
}

internal sealed class TextToSpeechVoicesCommand : LocalizedCommands
{
    [Dependency] private readonly ITtsManager _ttsManager = default!;

    public override string Command => "tts_voices";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        foreach (var voice in _ttsManager.Voices)
        {
            shell.WriteLine($"{voice.Id}: {voice.DisplayName}, {voice.Language}, {voice.Gender}, {voice.Description}");
        }
    }
}
