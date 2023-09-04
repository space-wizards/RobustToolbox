using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Robust.Client.Audio.Midi.Commands;

[InjectDependencies]
public sealed partial class MidiPanicCommand : LocalizedCommands
{
    [Dependency] private IMidiManager _midiManager = default!;

    public override string Command => "midipanic";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        foreach (var renderer in _midiManager.Renderers)
        {
            renderer.StopAllNotes();
        }
    }
}
