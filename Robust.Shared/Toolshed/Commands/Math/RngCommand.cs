using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Robust.Shared.Toolshed.Commands.Math;

[ToolshedCommand]
public sealed class RngCommand : ToolshedCommand
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [CommandImplementation("to")]
    public int To(
            [PipedArgument] int from,
            int to
        )
        => _random.Next(from, to);

    [CommandImplementation("from")]
    public int From(
            [PipedArgument] int to,
            int from
        )
        => _random.Next(from, to);


    [CommandImplementation("to")]
    public float To(
            [PipedArgument] float from,
            float to
        )
        => _random.NextFloat(from, to);

    [CommandImplementation("from")]
    public float From(
            [PipedArgument] float to,
            float from
        )
        => _random.NextFloat(from, to);

    [CommandImplementation("prob")]
    public bool Prob(
        [PipedArgument] float prob
    )
        => _random.Prob(prob);
}
