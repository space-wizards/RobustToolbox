using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

public sealed class ValueRef<T>
{
    private Block<T>? _innerBlock;
    private string? _varName;
    private bool _hasValue = false;
    private T? _value;
    private string? _expression;
    private Vector2i? _refSpan;

    public ValueRef(string varName)
    {
        _varName = varName;
    }

    public ValueRef(Block<T> innerBlock)
    {
        _innerBlock = innerBlock;
    }

    public ValueRef(T value)
    {
        _value = value;
        _hasValue = true;
    }

    public bool LikelyConst => _varName is not null || _hasValue;


    public T? Evaluate(IInvocationContext ctx)
    {
        if (_value is not null && _hasValue)
        {
            return _value;
        }
        else if (_varName is not null)
        {
            return (T?)ctx.ReadVar(_varName);
        }
        else if (_innerBlock is not null)
        {
            return _innerBlock.Invoke(null, ctx);
        }
        else
        {
            throw new UnreachableException();
        }
    }

    public void Set(IInvocationContext ctx, T? value)
    {
        if (_varName is null)
            throw new NotImplementedException();

        ctx.WriteVar(_varName!, value);
    }
}

public record struct BadVarTypeError(Type Got, Type Expected, string VarName) : IConError
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
