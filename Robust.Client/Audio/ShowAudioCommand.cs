using Robust.Shared.Console;

namespace Robust.Client.Audio;

public sealed class ShowAudioCommand : IConsoleCommand
{
    public string Command => "showaudio";
    public string Description => "Shows audio details for nearby streams";
    public string Help => Command;
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        throw new System.NotImplementedException();
    }
}
