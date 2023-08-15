using System.IO;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Upload;

namespace Robust.Client.Upload.Commands;

public sealed class LoadPrototypeCommand : IConsoleCommand
{
    public string Command => "loadprototype";
    public string Description => "Load a prototype file into the server.";
    public string Help => Command;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        LoadPrototype();
    }

    public static async void LoadPrototype()
    {
        var dialogManager = IoCManager.Resolve<IFileDialogManager>();
        var loadManager = IoCManager.Resolve<IGamePrototypeLoadManager>();

        var stream = await dialogManager.OpenFile();
        if (stream is null)
            return;

        // ew oop
        var reader = new StreamReader(stream);
        var proto = await reader.ReadToEndAsync();
        loadManager.SendGamePrototype(proto);
    }
}
