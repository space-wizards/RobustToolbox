using System;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed.Commands.Values;

/// <summary>
/// Variant of the <see cref="ValCommand"/> that only works for variable references, and automatically infers the type
/// from the variable's value.
/// </summary>
[ToolshedCommand]
public sealed class VarCommand : ToolshedCommand
{
    private static Type[] _parsers = [typeof(VarTypeParser)];
    public override Type[] TypeParameterParsers => _parsers;

    [CommandImplementation]
    public T Var<T>(IInvocationContext ctx, VarRef<T> var)
        => var.Evaluate(ctx)!;
}
