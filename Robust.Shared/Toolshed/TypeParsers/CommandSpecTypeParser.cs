using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

public sealed class CommandSpecTypeParser : TypeParser<CommandSpec>
{
    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var cmd = parserContext.GetWord(ParserContext.IsCommandToken);
        var start = parserContext.Index;
        string? subCommand = null;
        if (cmd is null)
        {
            if (parserContext.PeekRune() is null)
            {
                error = new OutOfInputError();
                error.Contextualize(parserContext.Input, (parserContext.Index, parserContext.Index));
                result = null;
                return false;
            }
            else
            {

                error = new NotValidCommandError(typeof(object));
                error.Contextualize(parserContext.Input, (start, parserContext.Index+1));
                result = null;
                return false;
            }
        }

        if (!parserContext.Environment.TryGetCommand(cmd, out var cmdImpl))
        {
            error = new UnknownCommandError(cmd);
            error.Contextualize(parserContext.Input, (start, parserContext.Index));
            result = null;
            return false;
        }

        if (cmdImpl.HasSubCommands)
        {
            error = null;

            if (parserContext.GetChar() is not ':')
            {
                error = new OutOfInputError();
                error.Contextualize(parserContext.Input, (parserContext.Index, parserContext.Index));
                result = null;
                return false;
            }

            var subCmdStart = parserContext.Index;

            if (parserContext.GetWord(ParserContext.IsToken) is not { } subcmd)
            {
                error = new OutOfInputError();
                error.Contextualize(parserContext.Input, (parserContext.Index, parserContext.Index));
                result = null;
                return false;
            }

            if (!cmdImpl.Subcommands.Contains(subcmd))
            {
                error = new UnknownSubcommandError(cmd, subcmd, cmdImpl);
                error.Contextualize(parserContext.Input, (subCmdStart, parserContext.Index));
                result = null;
                return false;
            }

            subCommand = subcmd;
        }

        result = new CommandSpec(cmdImpl, subCommand);
        error = null;
        return true;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext, string? argName)
    {
        var cmds = parserContext.Environment.AllCommands();
        return ValueTask.FromResult<(CompletionResult?, IConError?)>((CompletionResult.FromHintOptions(cmds.Select(x => x.AsCompletion()), "<command name>"), null));
    }
}
