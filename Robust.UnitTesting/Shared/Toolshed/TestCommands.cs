using System;
using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.UnitTesting.Shared.Toolshed;

// This file just contains a collection of various test commands for use in other tests.

[ToolshedCommand]
public sealed class TestVoidCommand : ToolshedCommand
{
    [CommandImplementation] public void Impl() {}
}

[ToolshedCommand]
public sealed class TestIntCommand : ToolshedCommand
{
    [CommandImplementation] public int Impl() => 1;
}


[ToolshedCommand]
public sealed class TestTypeArgCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser)];
    [CommandImplementation] public string Impl<T>() => typeof(T).Name;
}

[ToolshedCommand]
public sealed class TestMultiTypeArgCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [typeof(TypeTypeParser), typeof(TypeTypeParser)];
    [CommandImplementation] public string Impl<T1, T2>(int i)
        => $"{typeof(T1).Name}, {typeof(T2).Name}, {i}";
}

[ToolshedCommand]
public sealed class TestIntStrArgCommand : ToolshedCommand
{
    [CommandImplementation] public int Impl(int i, string str) => i;
}

[ToolshedCommand]
public sealed class TestPipedIntCommand : ToolshedCommand
{
    [CommandImplementation] public int Impl([PipedArgument] int i) => i;
}

[ToolshedCommand]
public sealed class TestCustomVarRefParserCommand : ToolshedCommand
{
    [CommandImplementation]
    public int Impl([CommandArgument(typeof(Parser))] int i) => i;

    [Virtual]
    public class Parser : CustomTypeParser<int>
    {
        public override bool TryParse(ParserContext ctx, out int result)
        {
            result = default;
            if (!Toolshed.TryParse(ctx, out int _))
                return false;

            // Disregard the parsed value and always return 1
            result = 1;
            return true;
        }

        public override CompletionResult TryAutocomplete(ParserContext ctx, string? argName)
        {
            return new CompletionResult([new("A")], "B");
        }
    }
}

[ToolshedCommand]
public sealed class TestCustomParserCommand : ToolshedCommand
{
    [CommandImplementation]
    public int Impl([CommandArgument(typeof(Parser))] int i) => i;

    public sealed class Parser : TestCustomVarRefParserCommand.Parser
    {
        // Disable ValueRef support.
        // I.e., this parser will not not try to parse variables or blocks
        public override bool EnableValueRef => false;
    }
}
