using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Commands.Values;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// This custom parser is for parsing the type of a <see cref="VarRef{T}"/>. I.e., this can be used if the type of a
/// toolshed variable should be used as the generic argument for a command. Note that this will not actually "eat" a
/// successfully parsed the <see cref="VarRef{T}"/>, such that it can be used as a command argument. However, this
/// also means that this must always be the last type argument, and the first command argument.
/// </summary>
/// <remarks>
/// Note that this uses <see cref="ParserContext.VariableParser"/> to determine the variable type. If a variable's
/// type is modified during a command invocation in a way that this parser was not not aware of, this may result in
/// a <see cref="VarRef{T}.BadVarTypeError"/>.
/// </remarks>
public sealed class VarTypeParser : CustomTypeParser<Type>
{
    public override bool ShowTypeArgSignature => false;
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
    {
        result = null;
        var save = ctx.Save();

        ctx.ConsumeWhitespace();
        if (!ctx.EatMatch('$'))
            return false;

        var name = ctx.GetWord(ParserContext.IsToken);

        if (string.IsNullOrEmpty(name))
        {
            if (!ctx.GenerateCompletions)
                ctx.Error = new OutOfInputError();
            return false;
        }

        if (ctx.VariableParser.TryParseVar(name, out result))
        {
            // Reset the parser, so the VarRef can be parsed as a command argument.
            ctx.Restore(save);
            return true;
        }

        if (!ctx.GenerateCompletions)
            ctx.Error = new UnknownVariableError(name);
        return false;
    }

    public override CompletionResult TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return ctx.VariableParser.GenerateCompletions();
    }
}

public record UnknownVariableError(string VarName) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Unknown variable '${VarName}'. Cannot infer type. Consider using {nameof(ValCommand)} and explicitly specifying the type.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
