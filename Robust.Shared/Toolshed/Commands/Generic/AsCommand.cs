﻿using System;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class AsCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => [ typeof(Type) ];

    /// <summary>
    ///     Uses a typecast to convert a type. It does not handle implicit casts, nor explicit ones.
    ///     If you're thinking to extend this, you probably want to look into making a <see cref="TypeParser{T}"/>
    /// </summary>
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut? As<TOut, TIn>([PipedArgument] TIn value)
        => (TOut?)(object?)value;
}
