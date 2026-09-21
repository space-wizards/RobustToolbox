using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
public sealed class CmdCommand : ToolshedCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List(IInvocationContext ctx)
        => ctx.Environment.AllCommands();

    [CommandImplementation("moo")]
    public string Moo()
        => "Have you mooed today?";

    [CommandImplementation("descloc")]
    public string GetLocStr([PipedArgument] CommandSpec cmd) => cmd.DescLocStr();

    [CommandImplementation("info")]
    public CommandSpec Info(CommandSpec cmd) => cmd;

    [CommandImplementation("accepts")]
    public IEnumerable<CommandSpec> Accepts([PipedArgument] IEnumerable<CommandSpec> specs, Type pipedArgument)
        => Accepts_Impl(specs, pipedArgument, false);

    // TODO: This should be an optional parameter, --generic, instead.
    [CommandImplementation("accepts_generic")]
    public IEnumerable<CommandSpec> AcceptsGeneric([PipedArgument] IEnumerable<CommandSpec> specs, Type pipedArgument)
        => Accepts_Impl(specs, pipedArgument, true);

    private IEnumerable<CommandSpec> Accepts_Impl([PipedArgument] IEnumerable<CommandSpec> specs, Type pipedArgument, bool allowGeneric)
    {
        // This did not get made into methods on ToolshedCommand because of the fitness thing.
        // For user-friendliness reasons this always orders the most exactly matching commands first.
        return specs.Select(cmd =>
        {
            var fitness = -1; // unfit, ditch.

            var impls = cmd.Cmd.CommandImplementors[cmd.SubCommand ?? string.Empty]
                .MethodsByPipedType(pipedArgument, allowGeneric);

            var anyNonGeneric = false;

            foreach (var impl in impls)
            {
                // if any commands at all, raise it to 0, but don't modify higher fitness.
                fitness = int.Max(fitness, 0);

                // We treat object like a generic param because that's "probably what the user wants"
                if (!impl.PipeGeneric && (impl.PipeArg!.ParameterType != typeof(object)))
                {
                    if (impl.PipeArg!.ParameterType == pipedArgument)
                        fitness = int.Max(fitness, 2); // Exact match
                    else
                        fitness = int.Max(fitness, 1);

                    anyNonGeneric = true;
                }
            }

            if (!anyNonGeneric && !allowGeneric)
                return (cmd, fitness: -1);

            return (cmd, fitness); // command + fitness
        })
        .Where(x => x.fitness >= 0)
        .OrderByDescending(x => x.fitness)
        .Select(x => x.cmd);
    }

    [CommandImplementation("returns")]
    public IEnumerable<CommandSpec> Returns([PipedArgument] IEnumerable<CommandSpec> specs, Type pipedArgument)
        => Returns_Impl(specs, pipedArgument, false);

    // TODO: This should be an optional parameter, --generic, instead.
    [CommandImplementation("returns_generic")]
    public IEnumerable<CommandSpec> ReturnsGeneric([PipedArgument] IEnumerable<CommandSpec> specs, Type pipedArgument)
        => Returns_Impl(specs, pipedArgument, true);

    private IEnumerable<CommandSpec> Returns_Impl(IEnumerable<CommandSpec> specs, Type returnType, bool allowGeneric)
    {
        // This did not get made into methods on ToolshedCommand because of the fitness thing.
        // For user-friendliness reasons this always orders the most exactly matching commands first.
        return specs.Select(cmd =>
            {
                var fitness = -1; // unfit, ditch.

                var impls = cmd.Cmd.CommandImplementors[cmd.SubCommand ?? string.Empty]
                    .MethodsByReturnType(returnType, allowGeneric);

                var anyNonGeneric = false;

                foreach (var impl in impls)
                {
                    // if any commands at all, raise it to 0, but don't modify higher fitness.
                    fitness = int.Max(fitness, 0);

                    if (!impl.Info.ReturnType.IsGenericParameter && (impl.Info.ReturnType != typeof(object)))
                    {
                        if (impl.Info.ReturnType == returnType)
                            fitness = int.Max(fitness, 2); // Exact match
                        else
                            fitness = int.Max(fitness, 1);

                        anyNonGeneric = true;
                    }
                }

                if (!anyNonGeneric && !allowGeneric)
                    return (cmd, fitness: -1);

                return (cmd, fitness); // command + fitness
            })
            .Where(x => x.fitness >= 0)
            .OrderByDescending(x => x.fitness)
            .Select(x => x.cmd);
    }

#if CLIENT_SCRIPTING
    [CommandImplementation("getshim")]
    public MethodInfo GetShim(Block block)
    {

        // this is gross sue me
        var invocable = block.Run.Commands.Last().Item1.Invocable;
        return invocable.GetMethodInfo();
    }
#endif
}
