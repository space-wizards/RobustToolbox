using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

/// <summary>
/// This class is used represent toolshed command arguments that are either a reference to a Toolshed variable
/// (<see cref="VarRef{T}"/>), a block of commands that need to be evaluated (<see cref="Block{T}"/>), or simply some
/// specific value that has already been parsed/evaluated (<see cref="ParsedValueRef{T}"/>).
/// </summary>
public abstract class ValueRef<T>
{
    public abstract T? Evaluate(IInvocationContext ctx);

    // Internal method used when invoking commands to evaluate & cast command parameters.
    // Mainly exists for convenience, as expression trees don't support 'is' pattern matching or null propagation.
    // Also makes makes for much nicer debugging of command parameter parsing
    internal static T? EvaluateParameter(object? obj, IInvocationContext ctx)
    {
        return obj switch
        {
            null => default,
            T cast => cast,
            ValueRef<T> @ref => @ref.Evaluate(ctx),
            _ => throw new Exception(
                $"Failed to parse command parameter. This likely is a toolshed bug and should be reported.\n" +
                $"Target type: {typeof(T).PrettyName()}.\n" +
                $"Input type: {obj.GetType()}.\n" +
                $"Input: {obj}")
        };
    }

    internal static T[] EvaluateParamsCollection(object? obj, IInvocationContext ctx)
    {
        if (obj is not List<object?> parsedValues)
            throw new Exception("Failed to parse command parameter. This likely is a toolshed bug and should be reported.");

        var i = 0;
        var arr = new T[parsedValues.Count];
        foreach (var parsed in parsedValues)
        {
            arr[i++] = EvaluateParameter(parsed, ctx)!;
        }

        return arr;
    }

}

[Obsolete("Use EntProtoId / ProtoId<T>")]
public sealed class ValueRef<T, TAuto>(ValueRef<T> inner) : ValueRef<T>
{
    public override T? Evaluate(IInvocationContext ctx)
    {
        return inner.Evaluate(ctx);
    }
}

public sealed class BlockRef<T>(Block<T> block) : ValueRef<T>
{
    public override T? Evaluate(IInvocationContext ctx) => block.Invoke(ctx);
}

/// <summary>
/// This class is used represent toolshed command arguments that references to a Toolshed variable.
/// I.e., something accessible via <see cref="IInvocationContext.ReadVar"/>.
/// </summary>
public sealed class VarRef<T>(string varName) : ValueRef<T>
{
    /// <summary>
    /// The name of the variable.
    /// </summary>
    public readonly string VarName = varName;

    public override T? Evaluate(IInvocationContext ctx)
    {
        var value = ctx.ReadVar(VarName);

        if (value is T v)
            return v;

        var error = new BadVarTypeError(value?.GetType(), typeof(T), VarName);
        ctx.ReportError(error);
        return default;
    }

    public record BadVarTypeError(Type? Got, Type Expected, string VarName) : IConError
    {
        public FormattedMessage DescribeInner()
        {
            var msg = Got == null
                ? $"Variable ${VarName} is not assigned. Expected variable of type {Expected.PrettyName()}."
                : $"Variable ${VarName} is not of the expected type. Expected {Expected.PrettyName()} but got {Got?.PrettyName()}.";
            return FormattedMessage.FromUnformatted(msg);
        }

        public string? Expression { get; set; }
        public Vector2i? IssueSpan { get; set; }
        public StackTrace? Trace { get; set; }
    }
}

// Used to only parse writeable variable names.
// Hacky class to work around the lack of generics in attributes, preventing a custom type parser .
public sealed class WriteableVarRef<T>(VarRef<T> inner) : ValueRef<T>
{
    public readonly VarRef<T> Inner = inner;
    public override T? Evaluate(IInvocationContext ctx)
    {
        return Inner.Evaluate(ctx);
    }
}

/// <summary>
/// This class represents a <see cref="ValueRef{T}"/> command argument that simply corresponds to a specific value of
/// some type that has already been parsed/evaluated.
/// </summary>
internal sealed class ParsedValueRef<T>(T? value) : ValueRef<T>
{
    public readonly T? Value = value;

    public override T? Evaluate(IInvocationContext ctx)
    {
        return Value;
    }
}
