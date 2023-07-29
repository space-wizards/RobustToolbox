using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.TypeParsers;


// TODO: This should be able to parse more types, currently it only knows the ones in SimpleTypes.
internal sealed class TypeTypeParser : TypeParser<Type>
{
    public Dictionary<string, Type> SimpleTypes = new()
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
        {"EntityUid", typeof(EntityUid)},
        {"ResPath", typeof(ResPath)},
    };

    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var firstWord = parser.GetWord(char.IsLetterOrDigit);
        if (firstWord is null)
        {
            error = new OutOfInputError();
            result = null;
            return false;
        }

        if (firstWord == "IEnumerable")
        {
            if (!parser.EatMatch('<'))
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }

            if (parser.GetWord(char.IsLetterOrDigit) is not { } word)
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }

            var innerTy = ParseBase(word);

            if (innerTy is null)
            {
                error = new UnknownType(firstWord);
                result = null;
                return false;
            }

            if (!parser.EatMatch('>'))
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }

            result = typeof(IEnumerable<>).MakeGenericType(innerTy);
            error = null;
            return true;

        }

        var ty = ParseBase(firstWord);

        if (ty is null)
        {
            error = new UnknownType(firstWord);
            result = null;
            return false;
        }

        result = ty;
        error = null;
        return true;
    }

    private Type? ParseBase(string word)
    {
        SimpleTypes.TryGetValue(word, out var ty);
        return ty;
    }

    public override ValueTask<(CompletionResult? result, IConError? error)> TryAutocomplete(ForwardParser parser,
        string? argName)
    {
        var options = SimpleTypes.Select(x => new CompletionOption(x.Key));
        return ValueTask.FromResult<(CompletionResult? result, IConError? error)>((CompletionResult.FromHintOptions(options, "C# level type"), null));
    }
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
