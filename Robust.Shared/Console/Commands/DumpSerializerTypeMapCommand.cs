using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Robust.Shared.Console.Commands;

internal sealed class DumpSerializerTypeMapCommand : LocalizedCommands
{
    [Dependency] private readonly IRobustSerializerInternal _robustSerializer = default!;

    public override string Command => "dump_netserializer_type_map";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        foreach (var (type, index) in _robustSerializer.GetTypeMap().OrderBy(x => x.Value))
        {
            shell.WriteLine($"{index}: {type}");
        }

        shell.WriteLine($"Hash: {_robustSerializer.GetSerializableTypesHashString()}");
    }
}
