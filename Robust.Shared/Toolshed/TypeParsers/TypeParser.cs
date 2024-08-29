using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ITypeParser : IPostInjectInit
{
    public Type Parses { get; }

    public bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error);

    public ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName);
}

[PublicAPI]
public abstract class TypeParser<T> : ITypeParser
    where T: notnull
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] protected readonly ILocalizationManager Loc = default!;

    protected ISawmill Log = default!;

    public virtual Type Parses => typeof(T);

    public abstract bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error);
    public abstract ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName);

    public virtual void PostInject()
    {
        Log = _log.GetSawmill(GetType().PrettyName());
    }
}
