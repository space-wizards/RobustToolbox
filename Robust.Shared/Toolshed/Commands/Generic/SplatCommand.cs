using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
internal sealed class SplatCommand : ToolshedCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation]
    public IEnumerable<T> Splat<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] ValueRef<T> value,
        [CommandArgument] ValueRef<int> amountValue)
    {
        var amount = amountValue.Evaluate(ctx);
        for (var i = 0; i < amount; i++)
        {
            yield return value.Evaluate(ctx)!;
            if (ctx.GetErrors().Any())
                yield break;
        }
    }
}
