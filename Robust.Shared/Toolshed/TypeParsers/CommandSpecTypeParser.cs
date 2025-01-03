using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class CommandSpecTypeParser : TypeParser<CommandSpec>
{
    public override bool TryParse(ParserContext ctx, out CommandSpec result)
    {
        var cmd = ctx.GetWord(ParserContext.IsCommandToken);
        var start = ctx.Index;
        string? subCommand = null;
        if (cmd is null)
        {
            if (ctx.PeekRune() is null)
            {
                ctx.Error = new OutOfInputError();
                ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index));
                result = default;
                return false;
            }

            ctx.Error = new NotValidCommandError();
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index+1));
            result = default;
            return false;
        }

        if (!ctx.Environment.TryGetCommand(cmd, out var cmdImpl))
        {
            ctx.Error = new UnknownCommandError(cmd);
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index));
            result = default;
            return false;
        }

        if (cmdImpl.HasSubCommands)
        {
            if (!ctx.EatMatch(':'))
            {
                ctx.Error = ctx.OutOfInput ? new OutOfInputError() : new ExpectedSubCommand();
                ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index + 1));
                result = default;
                return false;
            }

            var subCmdStart = ctx.Index;

            if (ctx.GetWord(ParserContext.IsToken) is not { } subcmd)
            {
                ctx.Error = new ExpectedSubCommand();
                ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index));
                result = default;
                return false;
            }

            if (!cmdImpl.Subcommands.Contains(subcmd))
            {
                ctx.Error = new UnknownSubcommandError(subcmd, cmdImpl);
                ctx.Error.Contextualize(ctx.Input, (subCmdStart, ctx.Index));
                result = default;
                return false;
            }

            subCommand = subcmd;
        }

        result = new CommandSpec(cmdImpl, subCommand);
        return true;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        var cmds = parserContext.Environment.AllCommands();
        return CompletionResult.FromHintOptions(cmds.Select(x => x.AsCompletion()), "<command name>");
    }
}


public sealed class ExpectedSubCommand : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Expected subcommand");
    }
}
