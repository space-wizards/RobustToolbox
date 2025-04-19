using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;


// TODO: This should be able to parse more types, currently it only knows the ones in SimpleTypes.
public sealed class TypeTypeParser : TypeParser<Type>
{
    [Dependency] private readonly IModLoader _modLoader = default!;

    public Dictionary<string, Type> Types = new()
    {
        {"object", typeof(object)},
        {"int", typeof(int)},
        {"uint", typeof(uint)},
        {"char", typeof(char)},
        {"byte", typeof(byte)},
        {"sbyte", typeof(sbyte)},
        {"short", typeof(short)},
        {"ushort", typeof(ushort)},
        {"long", typeof(ulong)},
        {"ulong", typeof(ulong)},
        {"string", typeof(string)},
        {"bool", typeof(bool)},
        {"nint", typeof(nint)},
        {"nuint", typeof(nuint)},
        {"float", typeof(float)},
        {"double", typeof(double)},
        {"decimal", typeof(decimal)},
        {nameof(Vector2), typeof(Vector2)},
        {nameof(TimeSpan), typeof(TimeSpan)},
        {nameof(DateTime), typeof(DateTime)},
        {"IEnumerable", typeof(IEnumerable<>)},
        {"List", typeof(List<>)},
        {"HashSet", typeof(HashSet<>)},
        {nameof(Task), typeof(Task<>)},
        {nameof(ValueTask), typeof(ValueTask<>)},
        // C# doesn't let you do `typeof(Dictionary<>)`. Why is a mystery to me.
        {"Dictionary", typeof(Dictionary<,>)},
    };

    private readonly HashSet<string> _ambiguousTypes = new();
    private CompletionResult? _optionsCache;

    public override void PostInject()
    {
        // SANDBOXING: We assume all `public` types on loaded assemblies are safe to reference. INCLUDING ROBUST.SHARED.
        foreach (var mod in _modLoader.LoadedModules.Append(Assembly.GetExecutingAssembly()).Append(Assembly.GetAssembly(typeof(Box2))!))
        {
            foreach (var exported in mod.ExportedTypes)
            {
                var name = exported.Name;
                if (_ambiguousTypes.Contains(name))
                    continue;

                if (!Types.TryAdd(name, exported))
                {
                    Types.Remove(name);
                    _ambiguousTypes.Add(name);
                }
            }
        }

        _optionsCache = CompletionResult.FromHintOptions(Types.Select(x => new CompletionOption(x.Key)), "C# level type");
    }

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Type? result)
    {
        var firstWord = ctx.GetWord(Rune.IsLetterOrDigit);
        if (firstWord is null)
        {
            ctx.Error = new OutOfInputError();
            result = null;
            return false;
        }

        var ty = ParseBase(firstWord);

        if (ty is null)
        {
            ctx.Error = new UnknownType(firstWord);
            result = null;
            return false;
        }

        if (ty.IsGenericTypeDefinition)
        {
            if (!ctx.EatMatch('<'))
            {
                ctx.Error = new ExpectedGeneric();
                result = null;
                return false;
            }

            var len = ty.GetGenericArguments().Length;
            var args = new Type[ty.GetGenericArguments().Length];

            for (var i = 0; i < len; i++)
            {
                if (!TryParse(ctx, out var t))
                {
                    result = null;
                    return false;
                }

                args[i] = t;

                if (i != (len - 1) && !ctx.EatMatch(','))
                {
                    ctx.Error = new ExpectedNextType();
                    result = null;
                    return false;
                }
            }

            if (!ctx.EatMatch('>'))
            {
                ctx.Error = new ExpectedGeneric();
                result = null;
                return false;
            }

            ty = ty.MakeGenericType(args);
        }

        if (ctx.EatMatch('['))
        {
            if (!ctx.EatMatch(']'))
            {
                ctx.Error = new UnknownType(firstWord);
                result = null;
                return false;
            }

            ty = ty.MakeArrayType();
        }

        if (ctx.EatMatch('?') && (ty.IsValueType || ty.IsPrimitive))
        {
            ty = typeof(Nullable<>).MakeGenericType(ty);
        }

        result = ty;
        return true;
    }

    private Type? ParseBase(string word)
    {
        Types.TryGetValue(word, out var ty);
        return ty;
    }

    public override CompletionResult? TryAutocomplete(
        ParserContext parserContext,
        CommandArgument? arg)
    {
        // TODO TOOLSHED Generic Type Suggestions.
        if (_optionsCache != null)
            _optionsCache.Hint = GetArgHint(arg);
        return _optionsCache;
    }
}

public record struct ExpectedNextType() : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected another type in the generic arguments.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct ExpectedGeneric() : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected a generic type, did you forget the angle brackets?");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct UnknownType(string T) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"The type {T} is not known and cannot be used.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
