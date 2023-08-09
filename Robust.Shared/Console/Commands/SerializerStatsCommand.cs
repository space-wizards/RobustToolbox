using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Robust.Shared.Console.Commands;

internal sealed class SerializeStatsCommand : LocalizedCommands
{
    [Dependency] private readonly IRobustSerializerInternal _robustSerializer = default!;

    public override string Command => "szr_stats";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.WriteLine($"serialized: {_robustSerializer.BytesSerialized} bytes, {_robustSerializer.ObjectsSerialized} objects");
        shell.WriteLine($"largest serialized: {_robustSerializer.LargestObjectSerializedBytes} bytes, {_robustSerializer.LargestObjectSerializedType} objects");
        shell.WriteLine($"deserialized: {_robustSerializer.BytesDeserialized} bytes, {_robustSerializer.ObjectsDeserialized} objects");
        shell.WriteLine($"largest serialized: {_robustSerializer.LargestObjectDeserializedBytes} bytes, {_robustSerializer.LargestObjectDeserializedType} objects");
    }
}

