namespace Robust.Shared.Toolshed.Commands.Values;

[ToolshedCommand(Name = "i")]
public sealed class IntCommand : ToolshedCommand
{
    [CommandImplementation]
    public int Impl(int value) => value;
}

[ToolshedCommand(Name = "f")]
public sealed class FloatCommand : ToolshedCommand
{
    [CommandImplementation]
    public float Impl(float value) => value;
}

[ToolshedCommand(Name = "s")]
public sealed class StringCommand : ToolshedCommand
{
    [CommandImplementation]
    public string Impl(string value) => value;
}

[ToolshedCommand(Name = "b")]
public sealed class BoolCommand : ToolshedCommand
{
    [CommandImplementation]
    public bool Impl(bool value) => value;
}
