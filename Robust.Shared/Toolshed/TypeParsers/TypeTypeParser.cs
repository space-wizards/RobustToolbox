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
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;


// TODO: This should be able to parse more types, currently it only knows the ones in SimpleTypes.
internal sealed class TypeTypeParser : TypeParser<Type>
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
    }

    public override bool TryParse(ParserContext parserContext, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var firstWord = parserContext.GetWord(Rune.IsLetterOrDigit);
        if (firstWord is null)
        {
            error = new OutOfInputError();
            result = null;
            return false;
        }

        var ty = ParseBase(firstWord);

        if (ty is null)
        {
            error = new UnknownType(firstWord);
            result = null;
            return false;
        }

        if (ty.IsGenericTypeDefinition)
        {
            if (!parserContext.EatMatch('<'))
            {
                error = new ExpectedGeneric();
                result = null;
                return false;
            }

            var len = ty.GetGenericArguments().Length;
            var args = new Type[ty.GetGenericArguments().Length];

            for (var i = 0; i < len; i++)
            {
                if (!TryParse(parserContext, out var t, out error))
                {
                    result = null;
                    return false;
                }

                args[i] = (Type) t;

                if (i != (len - 1) && !parserContext.EatMatch(','))
                {
                    error = new ExpectedNextType();
                    result = null;
                    return false;
                }
            }

            if (!parserContext.EatMatch('>'))
            {
                error = new ExpectedGeneric();
                result = null;
                return false;
            }

            ty = ty.MakeGenericType(args);
        }

        if (parserContext.EatMatch('['))
        {
            if (!parserContext.EatMatch(']'))
            {
                error = new UnknownType(firstWord);
                result = null;
                return false;
            }

            ty = ty.MakeArrayType();
        }

        if (parserContext.EatMatch('?') && (ty.IsValueType || ty.IsPrimitive))
        {
            ty = typeof(Nullable<>).MakeGenericType(ty);
        }

        result = ty;
        error = null;
        return true;
    }

    private Type? ParseBase(string word)
    {
        Types.TryGetValue(word, out var ty);
        return ty;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ParserContext parserContext,
        string? argName)
    {
        // TODO: Suggest generics.
        var options = Types.Select(x => new CompletionOption(x.Key));
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHintOptions(options, "C# level type"), null));
    }
}

public record struct ExpectedNextType() : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"Expected another type in the generic arguments.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct ExpectedGeneric() : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"Expected a generic type, did you forget the angle brackets?");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct UnknownType(string T) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"The type {T} is not known and cannot be used.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}


internal record struct TypeIsSandboxViolation(Type T) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"The type {T.PrettyName()} is not permitted under sandbox rules.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
