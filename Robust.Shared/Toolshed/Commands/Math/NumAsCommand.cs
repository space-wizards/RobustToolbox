using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class CheckedToCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut Operation<TOut, T>([PipedArgument] T x)
        where TOut: INumberBase<TOut>
        where T : INumberBase<T>
    {
        return TOut.CreateChecked(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TOut> Operation<TOut, T>([PipedArgument] IEnumerable<T> x)
        where TOut: INumberBase<TOut>
        where T : INumberBase<T>
        => x.Select(Operation<TOut, T>);
}

[ToolshedCommand]
public sealed class SaturateToCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut Operation<TOut, T>([PipedArgument] T x)
        where TOut: INumberBase<TOut>
        where T : INumberBase<T>
    {
        return TOut.CreateSaturating(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TOut> Operation<TOut, T>([PipedArgument] IEnumerable<T> x)
        where TOut: INumberBase<TOut>
        where T : INumberBase<T>
        => x.Select(Operation<TOut, T>);
}

[ToolshedCommand]
public sealed class TruncToCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut Operation<TOut, T>([PipedArgument] T x)
        where TOut: INumberBase<TOut>
        where T : INumberBase<T>
    {
        return TOut.CreateTruncating(x);
    }

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TOut> Operation<TOut, T>([PipedArgument] IEnumerable<T> x)
        where TOut: INumberBase<TOut>
        where T : INumberBase<T>
        => x.Select(Operation<TOut, T>);
}
