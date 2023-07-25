using System;

namespace Robust.Shared.RTShell.Commands.Generic;

[RtShellCommand]
internal sealed class AsCommand : RtShellCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut? As<TOut, TIn>([PipedArgument] TIn value)
        => (TOut?)(object?)value;
}
