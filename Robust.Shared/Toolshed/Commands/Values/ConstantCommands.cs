namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand(Name = "i")]
public sealed class IntCommand : ToolshedCommand
{
    [CommandImplementation]
    public int Impl([CommandArgument] int value) => value;
}

[ToolshedCommand(Name = "f")]
public sealed class FloatCommand : ToolshedCommand
{
    [CommandImplementation]
    public float Impl([CommandArgument] float value) => value;
}

[ToolshedCommand(Name = "s")]
public sealed class StringCommand : ToolshedCommand
{
    [CommandImplementation]
    public string Impl([CommandArgument] string value) => value;
}

[ToolshedCommand(Name = "b")]
public sealed class BoolCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool Impl([CommandArgument] bool value) => value;
}
