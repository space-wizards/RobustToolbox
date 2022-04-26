using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Client.Audio.Midi.Commands;

public sealed class MidiPanicCommand : IConsoleCommand
{
    [Dependency] private readonly IMidiManager _midiManager = default!;

    public string Command => "midipanic";
    public string Description => Loc.GetString("midi-panic-command-description");
    public string Help => $"{Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        foreach (var renderer in _midiManager.Renderers)
        {
            renderer.StopAllNotes();
        }
    }
}
