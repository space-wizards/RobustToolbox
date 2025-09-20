using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;

/// <summary>
/// This is a simple custom type parser that uses reflection to search for constructible types that are the children base type.
/// </summary>
internal sealed class ReflectionTypeParser<TBase> : CustomTypeParser<Type> where TBase : class
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    private Dictionary<string, Type>? _cache;
    private CompletionOption[]? _options;

    [MemberNotNull(nameof(_cache))]
    [MemberNotNull(nameof(_options))]
    private void InitCache()
    {
        if (_cache != null && _options != null)
            return;

        _cache = _reflection.GetAllChildren(typeof(TBase))
            .Where(x => x.HasParameterlessConstructor())
            .ToDictionary(x => x.Name, x => x);

        _options = _cache.Keys.Select(x => new CompletionOption()).ToArray();
    }

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
    {
        InitCache();
        var name = ctx.GetWord();
        if (name is null)
        {
            ctx.Error = new OutOfInputError();
            result = null;
            return false;
        }

        if (_cache.TryGetValue(name, out result))
            return true;

        ctx.Error = new UnknownType(name);
        result = null;
        return false;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        InitCache();
        return CompletionResult.FromHintOptions(_options, GetArgHint(arg));
    }
}
