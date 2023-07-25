using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.RTShell.Errors;

namespace Robust.Shared.RTShell.TypeParsers;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ITypeParser : IPostInjectInit
{
    public Type Parses { get; }

    public bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error);

    public bool TryAutocomplete(ForwardParser parser, string? argName, [NotNullWhen(true)] out CompletionResult? options, out IConError? error);
}

[PublicAPI]
public abstract class TypeParser<T> : ITypeParser
    where T: notnull
{
    [Dependency] private readonly ILogManager _log = default!;

    protected ISawmill _sawmill = default!;

    public Type Parses => typeof(T);

    public abstract bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error);
    public abstract bool TryAutocomplete(ForwardParser parser, string? argName, [NotNullWhen(true)] out CompletionResult? options, out IConError? error);

    public void PostInject()
    {
        Logger.Debug("awawasadfs");
        _sawmill = _log.GetSawmill(GetType().PrettyName());
    }
}
