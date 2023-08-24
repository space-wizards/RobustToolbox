using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class RngCommand : ToolshedCommand
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [CommandImplementation("to")]
    public int To(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] int from,
            [CommandArgument] ValueRef<int> to
        )
        => _random.Next(from, to.Evaluate(ctx));

    [CommandImplementation("from")]
    public int From(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] int to,
            [CommandArgument] ValueRef<int> from
        )
        => _random.Next(from.Evaluate(ctx), to);


    [CommandImplementation("to")]
    public float To(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] float from,
            [CommandArgument] ValueRef<float> to
        )
        => _random.NextFloat(from, to.Evaluate(ctx));

    [CommandImplementation("from")]
    public float From(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] float to,
            [CommandArgument] ValueRef<float> from
        )
        => _random.NextFloat(from.Evaluate(ctx), to);

    [CommandImplementation("prob")]
    public bool Prob(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] float prob
    )
        => _random.Prob(prob);

    [CommandImplementation("prob")]
    public bool Prob(
            [CommandInvocationContext] IInvocationContext ctx,
            [CommandArgument] ValueRef<float> prob
        )
        => _random.Prob(prob.Evaluate(ctx));
}
