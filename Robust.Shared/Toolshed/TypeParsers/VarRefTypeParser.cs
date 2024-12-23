using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class VarRefTypeParser<T> : TypeParser<VarRef<T>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out VarRef<T>? result)
    {
        result = null;

        ctx.ConsumeWhitespace();
        var start = ctx.Index;
        if (!ctx.EatMatch('$'))
        {
            if (ctx.GenerateCompletions)
                return false;
            ctx.Error = new ExpectedDollarydoo();
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index+1));
            return false;
        }

        start = ctx.Index;
        var name = ctx.GetWord(ParserContext.IsToken);

        if (string.IsNullOrEmpty(name))
        {
            if (ctx.GenerateCompletions)
                return false;
            ctx.Error = new ExpectedVariableName();
            ctx.Error.Contextualize(ctx.Input, (start, ctx.Index+1));
            return false;
        }

        result = new VarRef<T>(name);
        return true;
    }

    public override CompletionResult TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return parserContext.VariableParser.GenerateCompletions<T>();
    }
}

public sealed class WriteableVarRefParser<T> : TypeParser<WriteableVarRef<T>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out WriteableVarRef<T>? result)
    {
        var start = ctx.Index;
        result = null;
        if (!ctx.Toolshed.TryParse(ctx, out VarRef<T>? inner))
            return false;

        if (!ctx.VariableParser.IsReadonlyVar(inner.VarName))
        {
            result = new(inner);
            return true;
        }
        if (ctx.GenerateCompletions)
            return false;

        ctx.Error = new ReadonlyVariableError(inner.VarName);
        ctx.Error.Contextualize(ctx.Input, (start, ctx.Index+1));
        return false;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return parserContext.VariableParser.GenerateCompletions<T>(false);
    }
}


public sealed class ExpectedDollarydoo : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected a variable name, which must start with a '$'.");
    }
}

public sealed class ExpectedVariableName : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected a valid variable name.");
    }
}
