using System.IO;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpSerializerTypeMapCommand : LocalizedCommands
{
    [Dependency] private readonly IRobustSerializerInternal _robustSerializer = default!;

    public override string Command => "dump_netserializer_type_map";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var stream = new MemoryStream();
        ((RobustSerializer)_robustSerializer).GetHashManifest(stream, true);
        stream.Position = 0;

        using var streamReader = new StreamReader(stream);
        shell.WriteLine($"Hash: {_robustSerializer.GetSerializableTypesHashString()}");
        shell.WriteLine("Manifest:");
        while (streamReader.ReadLine() is { } line)
        {
            shell.WriteLine(line);
        }
    }
}
