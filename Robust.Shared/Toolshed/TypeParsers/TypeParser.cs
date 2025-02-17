using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// Base interface used by both custom and default type parsers.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ITypeParser
{
    public Type Parses { get; }
    bool TryParse(ParserContext ctx, [NotNullWhen(true)] out object? result);
    CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg);

    /// <summary>
    /// If true, then before attempting to use this parser directly, toolshed will instead first try to parse this as a
    /// as a variable (<see cref="VarRef{T}"/>) or command block (<see cref="BlockRef{T}"/>).
    /// This has no effect when the parser is being used to parse type-arguments, those don't currently support blocks
    /// or variables
    /// </summary>
    public bool EnableValueRef { get; }

    /// <summary>
    /// Whether or not the type argument should appear in the method's signature. This mainly exists for type-argument
    /// parsers that infer a type argument based on a regular arguments, like <see cref="VarTypeParser"/>.
    /// </summary>
    public virtual bool ShowTypeArgSignature => true;
}

public abstract class BaseParser<T> : ITypeParser, IPostInjectInit where T : notnull
{
    public virtual bool EnableValueRef => true;
    public virtual bool ShowTypeArgSignature => true;

    // TODO TOOLSHED Localization
    // Ensure that all of the type parser auto-completions actually use localized strings
    [Dependency] protected readonly ILocalizationManager Loc = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] protected readonly ToolshedManager Toolshed = default!;

    protected ISawmill Log = default!;

    public virtual void PostInject()
    {
        Log = _log.GetSawmill(GetType().PrettyName());
    }

    public abstract bool TryParse(ParserContext ctx, [NotNullWhen(true)] out T? result);

    public abstract CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg);

    protected string GetArgHint(CommandArgument? arg)
    {
        return ToolshedCommand.GetArgHint(arg, typeof(T));
    }

    public Type Parses => typeof(T);

    bool ITypeParser.TryParse(ParserContext ctx, [NotNullWhen(true)] out object? result)
    {
        if (!TryParse(ctx, out T? res))
        {
            result = null;
            return false;
        }

        result = res;
        return true;
    }
}

/// <summary>
/// Inheritors of this class can be used as custom parsers for toolshed commands. Inheritors need to have a
/// parameterless constructor, so that <see cref="ToolshedManager"/> can create a parser instance.
/// </summary>
public abstract class CustomTypeParser<T> : BaseParser<T> where T : notnull;

/// <summary>
/// Inheritors of this class are used as the default parsers when trying to resolve arguments of type <see cref="T"/>.
/// Inheritors need to have a parameterless constructor, so that <see cref="ToolshedManager"/> can create a parser
/// instance.
/// </summary>
public abstract class TypeParser<T> : BaseParser<T> where T : notnull;

/// <summary>
/// Base class for custom parsers that exist simply to provide customized auto-completion options.
/// </summary>
public abstract class CustomCompletionParser<T> : CustomTypeParser<T> where T : notnull
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out T? result)
    {
        // Use default parser type T
        return Toolshed.TryParse(ctx, out result);
    }
}
