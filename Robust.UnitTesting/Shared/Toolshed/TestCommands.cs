using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
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

        public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
        {
            return new CompletionResult([new("A")], "B");
        }
    }
}

[ToolshedCommand]
public sealed class TestOptionalArgsCommand : ToolshedCommand
{
    [CommandImplementation]
    public int[] Impl(int x, int y = 0, int z = 1)
        => [x, y, z];
}

[ToolshedCommand]
public sealed class TestParamsCollectionCommand : ToolshedCommand
{
    [CommandImplementation]
    public int[] Impl(int x, int y = 0, params int[] others)
        => [x, y, ..others];
}

[ToolshedCommand]
public sealed class TestParamsOnlyCommand : ToolshedCommand
{
    [CommandImplementation]
    public int[] Impl(params int[] others)
        => others;
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

[ToolshedCommand]
public sealed class TestEnumerableInferCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public Type Impl<T>([PipedArgument] IEnumerable<T> x, T y) => typeof(T);
}

[ToolshedCommand]
public sealed class TestListInferCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public Type Impl<T>([PipedArgument] List<T> x, T y) => typeof(T);
}

[ToolshedCommand]
public sealed class TestArrayInferCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public Type Impl<T>([PipedArgument] T[] x, T y) => typeof(T);
}

[ToolshedCommand]
public sealed class TestNestedEnumerableInferCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public Type Impl<T>([PipedArgument] IEnumerable<ProtoId<T>> x)
        where T : class, IPrototype
    {
        return typeof(T);
    }
}

[ToolshedCommand]
public sealed class TestNestedListInferCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public Type Impl<T>([PipedArgument] List<ProtoId<T>> x)
        where T : class, IPrototype
    {
        return typeof(T);
    }
}

[ToolshedCommand]
public sealed class TestNestedArrayInferCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public Type Impl<T>([PipedArgument] ProtoId<T>[] x)
        where T : class, IPrototype
    {
        return typeof(T);
    }
}

[ToolshedCommand]
public sealed class TestArrayCommand : ToolshedCommand
{
    [CommandImplementation]
    public int[] Impl() => Array.Empty<int>();
}

[ToolshedCommand]
public sealed class TestListCommand : ToolshedCommand
{
    [CommandImplementation]
    public List<int> Impl() => new();
}

[ToolshedCommand]
public sealed class TestEnumerableCommand : ToolshedCommand
{
    private static int[] _arr = {1, 3, 3};

    [CommandImplementation]
    public IEnumerable<int> Impl() => _arr.Select(x => 2 * x);
}

[ToolshedCommand]
public sealed class TestNestedArrayCommand : ToolshedCommand
{
    [CommandImplementation]
    public ProtoId<EntityCategoryPrototype>[] Impl() => [];
}

[ToolshedCommand]
public sealed class TestNestedListCommand : ToolshedCommand
{
    [CommandImplementation]
    public List<ProtoId<EntityCategoryPrototype>> Impl() => new();
}

[ToolshedCommand]
public sealed class TestNestedEnumerableCommand : ToolshedCommand
{
    private static ProtoId<EntityCategoryPrototype>[] _arr = [];

    [CommandImplementation]
    public IEnumerable<ProtoId<EntityCategoryPrototype>> Impl() => _arr.OrderByDescending(x => x.Id);
}
