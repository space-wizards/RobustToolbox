using System;
using System.Diagnostics;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

public sealed class ValueRef<T> : ValueRef<T, T>
{
    public ValueRef(ValueRef<T, T> inner)
    {
        InnerBlock = inner.InnerBlock;
        VarName = inner.VarName;
        HasValue = inner.HasValue;
        Value = inner.Value;
        Expression = inner.Expression;
        RefSpan = inner.RefSpan;
    }
    public ValueRef(string varName) : base(varName)
    {
    }

    public ValueRef(Block<T> innerBlock) : base(innerBlock)
    {
    }

    public ValueRef(T value) : base(value)
    {
    }
}

[Virtual]
public class ValueRef<T, TAuto>
{
    internal Block<T>? InnerBlock;
    internal string? VarName;
    internal bool HasValue = false;
    internal T? Value;
    internal string? Expression;
    internal Vector2i? RefSpan;

    protected ValueRef()
    {
    }

    public ValueRef(string varName)
    {
        VarName = varName;
    }

    public ValueRef(Block<T> innerBlock)
    {
        InnerBlock = innerBlock;
    }

    public ValueRef(T value)
    {
        Value = value;
        HasValue = true;
    }

    public bool LikelyConst => VarName is not null || HasValue;

    public T? Evaluate(IInvocationContext ctx)
    {
        if (Value is not null && HasValue)
        {
            return Value;
        }
        else if (VarName is not null)
        {
            var value = ctx.ReadVar(VarName);

            if (value is not T v)
            {
                ctx.ReportError(new BadVarTypeError(value?.GetType() ?? typeof(void), typeof(T), VarName));
                return default;
            }

            return v;
        }
        else if (InnerBlock is not null)
        {
            return InnerBlock.Invoke(null, ctx);
        }
        else
        {
            throw new UnreachableException();
        }
    }

    public void Set(IInvocationContext ctx, T? value)
    {
        if (VarName is null)
            throw new NotImplementedException();

        ctx.WriteVar(VarName!, value);
    }
}

public record BadVarTypeError(Type Got, Type Expected, string VarName) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"Got unexpected type {Got.PrettyName()} in {VarName}, expected {Expected.PrettyName()}");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
