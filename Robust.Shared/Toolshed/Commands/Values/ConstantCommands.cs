namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand]
internal sealed class IntCommand : ToolshedCommand
{
    [CommandImplementation]
    public int Impl([CommandArgument] int value) => value;
}

[ToolshedCommand]
internal sealed class FloatCommand : ToolshedCommand
{
    [CommandImplementation]
    public float Impl([CommandArgument] float value) => value;
}

[ToolshedCommand]
internal sealed class StringCommand : ToolshedCommand
{
    [CommandImplementation]
    public string Impl([CommandArgument] string value) => value;
}

[ToolshedCommand]
internal sealed class BoolCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool Impl([CommandArgument] bool value) => value;
}
