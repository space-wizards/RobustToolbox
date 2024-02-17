using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Generic;

[ToolshedCommand]
public sealed class ReduceCommand : ToolshedCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public T Reduce<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> input,
        [CommandArgument] Block<T, T> reducer
    )
        => input.Aggregate((x, next) => reducer.Invoke(x, new ReduceContext<T>(ctx, next))!);
}

internal record ReduceContext<T>(IInvocationContext Inner, T Value) : IInvocationContext
{
    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        return Inner.CheckInvokable(command, out error);
    }

    public ICommonSession? Session => Inner.Session;
    public ToolshedManager Toolshed => Inner.Toolshed;
    public NetUserId? User => Inner.User;

    public ToolshedEnvironment Environment => Inner.Environment;

    public void WriteLine(string line)
    {
        Inner.WriteLine(line);
    }

    public void ReportError(IConError err)
    {
        Inner.ReportError(err);
    }

    public IEnumerable<IConError> GetErrors()
    {
        return Inner.GetErrors();
    }

    public void ClearErrors()
    {
        Inner.ClearErrors();
    }

    public Dictionary<string, object?> Variables { get; } = new();

    public object? ReadVar(string name)
    {
        if (name == "value")
            return Value;

        return Inner.ReadVar(name);
    }
}
